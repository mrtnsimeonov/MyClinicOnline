using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MyClinicOnline.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.RateLimiting;

namespace MyClinicOnline.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService;

        public AccountController(MyClinicOnlineContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        Phone = model.Phone,
                        Region = model.Region,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    await _emailService.SendEmailAsync(user.Email, "Успешно регистриране в MyClinicOnline", "Здравейте, успешно се регистрирахте в MyClinicOnline!");

                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    return Content($"Грешка: {ex.Message} --- {ex.InnerException?.Message}");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> RegisterDoctor()
        {
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(RegisterDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = new Doctor
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        WorksWithNhif = model.WorksWithNhif,
                        CityId = model.CityId
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Any())
                    {
                        foreach (var specId in model.SelectedSpecialtyIds)
                        {
                            var doctorSpecialty = new DoctorSpecialty
                            {
                                DoctorId = doctor.Id,
                                SpecialtyId = specId
                            };
                            _context.DoctorSpecialties.Add(doctorSpecialty);
                        }
                        await _context.SaveChangesAsync();
                    }

                    string subject = "Successfully Registered - MyClinicOnline";
                    string body = $@"Dear Dr. {doctor.FullName},
Thank you for registering on MyClinicOnline.
Your account is currently under review. You will receive a confirmation email once approved by our admin team.";

                    await _emailService.SendEmailAsync(doctor.Email, subject, body);

                    await _emailService.SendEmailAsync(
                        "admin@myclinic.com",
                        "New doctor registration pending approval",
                        $"Dr. {doctor.FullName} ({doctor.Email}) has registered and is awaiting your approval.");

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                    System.Diagnostics.Debug.WriteLine("Registration Error: " + ex.Message);
                }
            }

            ViewBag.Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Check users (patients + admin)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && VerifyPassword(password, user.Password))
            {
                // Migrate plain-text password to hash on first login after upgrade
                if (!user.Password.StartsWith("$2"))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }

                if (user.IsAdmin)
                {
                    await SignInUser(user.Email, "AD", "Admin", user.Id);
                    return RedirectToAction("Dashboard", "Admin");
                }

                string initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
                await SignInUser(user.Email, initials, "Patient", user.Id);
                return RedirectToAction("Index", "Home");
            }

            // 2. Check doctors
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
            if (doctor != null && !string.IsNullOrEmpty(doctor.Password) && VerifyPassword(password, doctor.Password))
            {
                // Migrate plain-text password to hash on first login after upgrade
                if (!doctor.Password.StartsWith("$2"))
                {
                    doctor.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }

                if (!doctor.IsApproved)
                {
                    ViewBag.Error = "Your account is pending admin approval. You will be notified by email.";
                    return View();
                }

                var names = doctor.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string initials = names.Length > 1 ? $"{names[0][0]}{names[1][0]}" : $"{names[0][0]}";
                await SignInUser(doctor.Email, initials.ToUpper(), "Doctor", doctor.Id);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Грешен имейл или парола";
            return View();
        }

        private static bool VerifyPassword(string input, string stored)
        {
            if (stored.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(input, stored);
            return input == stored;
        }

        private async Task SignInUser(string email, string initials, string role, int id)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("Initials", initials),
                new Claim("UserId", id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Index", "Home");
        }
    }
}