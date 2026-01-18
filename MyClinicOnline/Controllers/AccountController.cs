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
            // Проверка за 18 години
            var today = DateTime.Today;
            var age = today.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth.Date > today.AddYears(-age)) age--;

            if (age < 18)
            {
                ModelState.AddModelError("DateOfBirth", "Трябва да имате навършени 18 години.");
            }

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password, // Препоръчително е тук да се ползва Hash
                    Phone = model.Phone,
                    Region = model.Region,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                SendEmail(user.Email);

                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                // Тук бихме настроили Cookies/Authentication
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Грешен имейл или парола";
            return View();
        }

        private void SendEmail(string toEmail)
        {
            try
            {
                // Имейлът трябва да е същият като на снимката ти
                var fromAddress = new MailAddress("MyClinicOnline@outlook.com", "MyClinicOnline");
                var toAddress = new MailAddress(toEmail);

                // ТУК ПОСТАВИ 16-СИМВОЛНИЯ КОД (без интервали)
                const string fromPassword = "ifkzlrwcyrvouehv";

                const string subject = "Успешно регистриране в MyClinicOnline";
                const string body = "Успешно регистриране в MyClinicOnline";

                var smtp = new SmtpClient
                {
                    Host = "smtp-mail.outlook.com",
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
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Ако има грешка, ще я видиш в прозореца "Output" на Visual Studio
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }

    }
}