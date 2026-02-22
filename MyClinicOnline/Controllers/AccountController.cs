using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using System.Net;
using System.Net.Mail;

namespace MyClinicOnline.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public AccountController(MyClinicOnlineContext context)
        {
            _context = context;
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
                        Password = model.Password,
                        Phone = model.Phone,
                        Region = model.Region,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender
                    };

                    // Inside [HttpPost] Register (for Patients)
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // UPDATE THIS LINE:
                    await SendEmailAsync(user.Email, "Успешно регистриране в MyClinicOnline", "Здравейте, успешно се регистрирахте в MyClinicOnline!");

                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    // Показва грешката директно в браузъра за лесно дебъгване
                    return Content($"Грешка: {ex.Message} --- {ex.InnerException?.Message}");
                }
            }
            return View(model);
        }
        // GET: /Account/RegisterDoctor
        [HttpGet]
        public async Task<IActionResult> RegisterDoctor()
        {
            // 1. Fetch Cities and Specialties to populate the dropdown and checkboxes
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync();

            return View();
        }

        // POST: /Account/RegisterDoctor
        [HttpPost]
        [ValidateAntiForgeryToken] // Recommended for security
        public async Task<IActionResult> RegisterDoctor(RegisterDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Map ViewModel to Doctor Model
                    var doctor = new Doctor
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        WorksWithNhif = model.WorksWithNhif,
                        CityId = model.CityId
                    };

                    // 2. Add and Save Doctor first (this generates the Doctor.Id)
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    // 3. Handle Many-to-Many Specialties
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

                        // Save the entries in the join table
                        await _context.SaveChangesAsync();
                    }

                    // 4. Send Confirmation Email using the updated flexible method
                    string subject = "Successfully Registered - MyClinicOnline";
                    string body = $@"Dear Dr. {doctor.FullName}, 
                             Thank you for joining our platform. 
                             Patients can now find and book appointments with you in your city.";

                    await SendEmailAsync(doctor.Email, subject, body);

                    // 5. Redirect to Home on success
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    // Log the error (optional) and show a friendly message
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                    System.Diagnostics.Debug.WriteLine("Registration Error: " + ex.Message);
                }
            }

            // If we reach here, something failed (Validation or Exception)
            // We must reload the ViewBag data so the form doesn't crash
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync();

            return View(model);
        }

        // Метод за изпращане на имейл (използваме твоя генериран App Password)
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromAddress = new MailAddress("maartin.simeonov@gmail.com", "MyClinicOnline");
                var toAddress = new MailAddress(toEmail);
                const string fromPassword = "rkgrophwmddyftza";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
                // We don't want to crash the whole registration if the email fails
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Грешен имейл или парола";
            return View();
        }
    }
}