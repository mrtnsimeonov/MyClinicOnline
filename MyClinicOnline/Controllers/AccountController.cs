using Microsoft.AspNetCore.Mvc;
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

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Извикване на асинхронния метод за имейл
                    await SendEmailAsync(user.Email);

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

        // Метод за изпращане на имейл (използваме твоя генериран App Password)
        private async Task SendEmailAsync(string toEmail)
        {
            try
            {
                var fromAddress = new MailAddress("mycliniconline@outlook.com", "MyClinicOnline");
                var toAddress = new MailAddress(toEmail);

                // Твоят актуален 16-символен код
                const string fromPassword = "ifkzlrwcyrvouehv";

                var smtp = new SmtpClient
                {
                    Host = "smtp.office365.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Успешно регистриране в MyClinicOnline",
                    Body = "Здравейте, успешно се регистрирахте в MyClinicOnline!"
                })
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Грешен имейл или парола";
            return View();
        }
    }
}