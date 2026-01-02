using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;

namespace MyClinicOnline.Controllers
{
    public class CalendarController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public CalendarController(MyClinicOnlineContext context)
        {
            _context = context;
        }

        // /Calendar/Index?doctorId=1&year=2026&month=1
        public IActionResult Index(int doctorId, int? year, int? month)
        {
            var doctor = _context.Doctors
                .Include(d => d.City)
                .FirstOrDefault(d => d.Id == doctorId);

            if (doctor == null) return NotFound();

            var today = DateTime.Today;
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
        public IActionResult Book(int slotId)
        {
            var slot = _context.TimeSlots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) return NotFound();

            if (slot.IsBooked)
            {
                TempData["Error"] = "This hour is already booked.";
                return RedirectToAction("Index", new { doctorId = slot.DoctorId });
            }

            slot.IsBooked = true;
            _context.SaveChanges();

            TempData["Success"] = "Booked successfully!";
            return RedirectToAction("Index", new { doctorId = slot.DoctorId, year = slot.StartTime.Year, month = slot.StartTime.Month });
        }

        // Creates hourly slots 10:00–16:00 (last start 16:00, ends 17:00)
        private void EnsureTimeSlotsForDoctor(int doctorId, DateTime fromDate, DateTime toDate)
        {
            // Generate from today to end date, only if missing
            for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
            {
                // Optional: skip weekends (uncomment if you want)
                // if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) continue;

                for (int hour = 10; hour < 17; hour++) // 10..16
                {
                    var start = day.AddHours(hour);
                    var end = start.AddHours(1);

                    bool exists = _context.TimeSlots.Any(ts =>
                        ts.DoctorId == doctorId &&
                        ts.StartTime == start);

                    if (!exists)
                    {
                        _context.TimeSlots.Add(new TimeSlot
                        {
                            DoctorId = doctorId,
                            StartTime = start,
                            EndTime = end,
                            IsBooked = false
                        });
                    }
                }
            }

            _context.SaveChanges();
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
