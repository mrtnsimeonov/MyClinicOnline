using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using MyClinicOnline.Services;
using System.Security.Cryptography;

namespace MyClinicOnline.Controllers
{
    public class CalendarController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService; // Add this

        public CalendarController(MyClinicOnlineContext context, IEmailService emailService) // Add emailService here
        {
            _context = context;
            _emailService = emailService;
        }

        // /Calendar/Index?doctorId=1&year=2026&month=1
        public IActionResult Index(int doctorId, int? year, int? month)
        {
            var doctor = _context.Doctors
                .Include(d => d.City)
                .FirstOrDefault(d => d.Id == doctorId);

            if (doctor == null) return NotFound();

            var today = LocalClock.Today;
            var start = new DateTime(today.Year, today.Month, 1);

            var selectedMonth = (year.HasValue && month.HasValue)
                ? new DateTime(year.Value, month.Value, 1)
                : start;

            // Limit months: from current month to +5 months (6 months total)
            var minMonth = start;
            var maxMonth = start.AddMonths(5);

            if (selectedMonth < minMonth) selectedMonth = minMonth;
            if (selectedMonth > maxMonth) selectedMonth = maxMonth;

            // Ensure time slots exist for next 6 months for this doctor
            EnsureTimeSlotsForDoctor(doctorId, today, maxMonth.AddMonths(1).AddDays(-1));

            // Load slots for this month
            var monthStart = selectedMonth;
            var monthEnd = selectedMonth.AddMonths(1);

            var slots = _context.TimeSlots
                .Where(ts => ts.DoctorId == doctorId && ts.StartTime >= monthStart && ts.StartTime < monthEnd)
                .OrderBy(ts => ts.StartTime)
                .ToList();

            var vm = new CalendarVm
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.FullName,
                CityName = doctor.City?.Name,
                Month = monthStart,
                MinMonth = minMonth,
                MaxMonth = maxMonth,
                Slots = slots
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Book(int slotId, string consultationType)
        {
            var slot = await _context.TimeSlots
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null) return NotFound();

            if (slot.IsBooked)
            {
                TempData["Error"] = "This hour is already booked.";
                return RedirectToAction("Index", new { doctorId = slot.DoctorId });
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int patientId = int.Parse(userIdClaim);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == patientId);
            if (user == null) return RedirectToAction("Login", "Account");

            var type = Enum.TryParse<ConsultationType>(consultationType, out var parsed)
                ? parsed
                : ConsultationType.InPerson;

            string? meetingCode = type == ConsultationType.Online
                ? GenerateMeetingCode()
                : null;

            slot.IsBooked = true;

            var appointment = new Appointment
            {
                DoctorId = slot.DoctorId,
                UserId = user.Id,
                TimeSlotId = slot.Id,
                Status = AppointmentStatus.Confirmed,
                ConsultationType = type,
                MeetingCode = meetingCode
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            try
            {
                string subject = "Потвърждение за записан час";
                string body;

                if (type == ConsultationType.Online && meetingCode != null)
                {
                    var joinLink = $"{Request.Scheme}://{Request.Host}/Video/Join?code={meetingCode}";
                    body = $"Здравейте {user.FirstName},\n\n" +
                           $"Записахте онлайн консултация при Д-р {slot.Doctor?.FullName} " +
                           $"за {slot.StartTime:dd.MM.yyyy} в {slot.StartTime:HH:mm} ч.\n\n" +
                           $"Вашият код за среща: {meetingCode}\n\n" +
                           $"Влезте в срещата чрез линка:\n{joinLink}\n\n" +
                           $"Може също да влезете от сайта с бутон 'Join Meeting' и да въведете кода.\n\n" +
                           $"Моля влезте до 10 минути преди часа.";
                }
                else
                {
                    body = $"Здравейте {user.FirstName},\n\n" +
                           $"Успешно записахте час при Д-р {slot.Doctor?.FullName} " +
                           $"за {slot.StartTime:dd.MM.yyyy} в {slot.StartTime:HH:mm} ч.";
                }

                await _emailService.SendEmailAsync(user.Email, subject, body);

                if (type == ConsultationType.Online && meetingCode != null && slot.Doctor?.Email != null)
                {
                    var joinLink = $"{Request.Scheme}://{Request.Host}/Video/Join?code={meetingCode}";
                    await _emailService.SendEmailAsync(
                        slot.Doctor.Email,
                        "Нова онлайн консултация",
                        $"Д-р {slot.Doctor.FullName},\n\n" +
                        $"Имате нова онлайн консултация с {user.FirstName} {user.LastName} " +
                        $"за {slot.StartTime:dd.MM.yyyy} в {slot.StartTime:HH:mm} ч.\n\n" +
                        $"Код за среща: {meetingCode}\n\n" +
                        $"Влезте в срещата чрез линка:\n{joinLink}\n\n" +
                        $"Можете също да влезете от Вашия график в сайта.");
                }
                else if (type == ConsultationType.InPerson && slot.Doctor?.Email != null)
                {
                    await _emailService.SendEmailAsync(
                        slot.Doctor.Email,
                        "Нов записан час",
                        $"Д-р {slot.Doctor.FullName},\n\n" +
                        $"Имате нов записан час с {user.FirstName} {user.LastName} " +
                        $"за {slot.StartTime:dd.MM.yyyy} в {slot.StartTime:HH:mm} ч.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Booking Email Error: " + ex.Message);
            }

            TempData["Success"] = type == ConsultationType.Online
                ? $"Записан! Вашият код за онлайн среща е: {meetingCode}"
                : "Booked successfully!";

            // Redirect to symptom form — patient can fill it or skip
            return RedirectToAction("Symptoms", "AppointmentNote", new { appointmentId = appointment.Id });
        }

        private static string GenerateMeetingCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return RandomNumberGenerator.GetString(chars, 8);
        }

        // Creates hourly slots 10:00–16:00 (last start 16:00, ends 17:00)
        private void EnsureTimeSlotsForDoctor(int doctorId, DateTime fromDate, DateTime toDate)
        {
            // ONE query to get all existing slot times for this doctor in the range
            var existingSlots = _context.TimeSlots
                .Where(ts => ts.DoctorId == doctorId
                          && ts.StartTime >= fromDate.Date
                          && ts.StartTime <= toDate.Date.AddDays(1))
                .Select(ts => ts.StartTime)
                .ToHashSet(); // fast O(1) lookup

            var newSlots = new List<TimeSlot>();

            for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
            {
                for (int hour = 10; hour < 17; hour++)
                {
                    var start = day.AddHours(hour);

                    if (!existingSlots.Contains(start)) // no DB call — just a HashSet check
                    {
                        newSlots.Add(new TimeSlot
                        {
                            DoctorId = doctorId,
                            StartTime = start,
                            EndTime = start.AddHours(1),
                            IsBooked = false
                        });
                    }
                }
            }

            if (newSlots.Any())
            {
                _context.TimeSlots.AddRange(newSlots); // ONE insert for all new slots
                _context.SaveChanges();
            }
        }
    }

    public class CalendarVm
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = "";
        public string? CityName { get; set; }

        public DateTime Month { get; set; }
        public DateTime MinMonth { get; set; }
        public DateTime MaxMonth { get; set; }

        public List<TimeSlot> Slots { get; set; } = new();
    }
}
