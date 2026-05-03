using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;

namespace MyClinicOnline.Controllers
{
    [Authorize]
    public class VideoController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public VideoController(MyClinicOnlineContext context)
        {
            _context = context;
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

            var now = DateTime.Now;
            var start = appointment.TimeSlot.StartTime;

            if (now < start.AddMinutes(-10))
            {
                var minutesLeft = (int)(start - now).TotalMinutes;
                ViewBag.Error = $"Срещата не е започнала още. Влезте {minutesLeft} минути преди {start:HH:mm}.";
                return View("EnterCode");
            }

            if (now > start.AddMinutes(60))
            {
                ViewBag.Error = "Часът е изтекъл. Тази консултация вече не е достъпна.";
                return View("EnterCode");
            }

            var roomName = $"mco-{appointment.Id}-{appointment.MeetingCode}".ToLower();

            ViewBag.RoomName = roomName;
            ViewBag.DisplayName = isDoctor
                ? $"Д-р {appointment.Doctor.FullName}"
                : $"{appointment.User.FirstName} {appointment.User.LastName}";
            ViewBag.AppointmentTime = start.ToString("dd.MM.yyyy HH:mm");
            ViewBag.DoctorName = appointment.Doctor.FullName;
            ViewBag.PatientName = $"{appointment.User.FirstName} {appointment.User.LastName}";

            return View("VideoCall");
        }
    }
}