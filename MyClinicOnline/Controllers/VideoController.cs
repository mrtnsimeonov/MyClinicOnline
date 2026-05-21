using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace MyClinicOnline.Controllers
{
    [Authorize]
    public class VideoController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IConfiguration _configuration;

        public VideoController(MyClinicOnlineContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Join(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return View("EnterCode");

            var appointment = await _context.Appointments
                .Include(a => a.TimeSlot)
                .Include(a => a.User)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.MeetingCode == code);

            if (appointment == null)
            {
                ViewBag.Error = "Невалиден код. Моля, проверете имейла си.";
                return View("EnterCode");
            }

            // Guard: related entities must be loaded
            if (appointment.TimeSlot == null || appointment.Doctor == null || appointment.User == null)
            {
                ViewBag.Error = "Срещата не може да бъде заредена. Моля, свържете се с поддръжката.";
                return View("EnterCode");
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdClaim);

            bool isDoctor = User.IsInRole("Doctor");
            bool isOwner = isDoctor
                ? appointment.DoctorId == currentUserId
                : appointment.UserId == currentUserId;

            if (!isOwner)
            {
                ViewBag.Error = "Нямате достъп до тази среща.";
                return View("EnterCode");
            }

            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "FLE Standard Time" : "Europe/Sofia");

            var startLocal = DateTime.SpecifyKind(appointment.TimeSlot.StartTime, DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, zone);
            var nowUtc = DateTime.UtcNow;

            if (nowUtc < startUtc.AddMinutes(-10))
            {
                var minutesLeft = (int)(startUtc - nowUtc).TotalMinutes;
                ViewBag.Error = $"Срещата не е започнала още. Влезте {minutesLeft} минути преди {startLocal:HH:mm}.";
                return View("EnterCode");
            }

            if (nowUtc > startUtc.AddMinutes(60))
            {
                ViewBag.Error = "Часът е изтекъл. Тази консултация вече не е достъпна.";
                return View("EnterCode");
            }

            // Read JaaS config — same for both patient and doctor
            var appId = _configuration["JitsiSettings:AppId"] ?? string.Empty;
            var apiKeyId = _configuration["JitsiSettings:ApiKeyId"] ?? string.Empty;
            var privateKeyPem = _configuration["JitsiSettings:PrivateKey"] ?? string.Empty;

            var roomName = $"mco-{appointment.Id}-{appointment.MeetingCode}".ToLower();

            // Build identity info — doctor path mirrors patient path exactly
            string displayName, userEmail, userId;
            if (isDoctor)
            {
                displayName = $"Д-р {appointment.Doctor.FullName}";
                userEmail = appointment.Doctor.Email ?? string.Empty;
                userId = $"doctor-{appointment.DoctorId}";
            }
            else
            {
                displayName = $"{appointment.User.FirstName} {appointment.User.LastName}";
                userEmail = appointment.User.Email ?? string.Empty;
                userId = $"patient-{appointment.UserId}";
            }

            var jwtToken = GenerateJaaSToken(
                appId, apiKeyId, privateKeyPem,
                roomName, userId, displayName, userEmail, isDoctor);

            ViewBag.RoomName = $"{appId}/{roomName}";
            ViewBag.JwtToken = jwtToken;
            ViewBag.DisplayName = displayName;
            ViewBag.AppointmentTime = startLocal.ToString("dd.MM.yyyy HH:mm");
            ViewBag.DoctorName = appointment.Doctor.FullName ?? string.Empty;
            ViewBag.PatientName = $"{appointment.User.FirstName} {appointment.User.LastName}";

            return View("VideoCall");
        }

        private static string GenerateJaaSToken(
            string appId, string apiKeyId, string privateKeyPem,
            string roomName, string userId, string displayName, string userEmail,
            bool isModerator)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var securityKey = new RsaSecurityKey(rsa)
            {
                KeyId = apiKeyId,
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

            var now = DateTimeOffset.UtcNow;

            var userContext = new Dictionary<string, object>
            {
                ["id"] = userId,
                ["name"] = displayName,
                ["email"] = userEmail,
                ["avatar"] = "",
                ["moderator"] = isModerator ? "true" : "false"
            };

            var featuresContext = new Dictionary<string, object>
            {
                ["livestreaming"] = "false",
                ["recording"] = "false",
                ["transcription"] = "false",
                ["outbound-call"] = "false"
            };

            var payload = new JwtPayload
            {
                ["iss"] = "chat",
                ["aud"] = "jitsi",
                ["sub"] = appId,
                ["room"] = "*",
                ["iat"] = now.ToUnixTimeSeconds(),
                ["nbf"] = now.AddSeconds(-10).ToUnixTimeSeconds(),
                ["exp"] = now.AddHours(2).ToUnixTimeSeconds(),
                ["context"] = new Dictionary<string, object>
                {
                    ["user"] = userContext,
                    ["features"] = featuresContext
                }
            };

            var header = new JwtHeader(credentials);
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
        }
    }
}
