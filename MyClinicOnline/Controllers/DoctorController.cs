using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Services; // Ensure you have the Email Service from previous steps

namespace MyClinicOnline.Controllers
{
    public class DoctorController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService;

        public DoctorController(MyClinicOnlineContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        public async Task<IActionResult> Search(string specialty)
        {
            if (string.IsNullOrWhiteSpace(specialty))
            {
                ViewBag.Specialty = specialty;
                return View(new List<Models.Doctor>());
            }

            var doctors = await _context.Doctors
                .Include(d => d.Specialties)
                .ThenInclude(ds => ds.Specialty)
                .Where(d => d.Specialties.Any(ds => ds.Specialty.Name.Contains(specialty)))
                .Where(d => d.IsApproved)  // ← ADD THIS LINE
                .ToListAsync();

            ViewBag.Specialty = specialty;
            return View(doctors);
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MyTimeSlots()
        {
            // Get the ID of the logged-in doctor
            var doctorIdClaim = User.FindFirst("UserId")?.Value;
            if (doctorIdClaim == null) return Unauthorized();
            int doctorId = int.Parse(doctorIdClaim);

            // Fetch all slots for this doctor
            var slots = await _context.TimeSlots
                .Where(ts => ts.DoctorId == doctorId)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

            // Map booked slots to their respective patients for display
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.User)
                .Where(a => a.DoctorId == doctorId)
                .ToDictionaryAsync(a => a.TimeSlotId, a => a);

            return View(slots); // This looks for Views/Doctor/MyTimeSlots.cshtml
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int slotId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.TimeSlot)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.TimeSlotId == slotId);

            if (appointment != null)
            {
                var patientEmail = appointment.User.Email;
                var doctorName = appointment.Doctor.FullName;
                var time = appointment.TimeSlot.StartTime;

                // 1. Free the slot
                appointment.TimeSlot.IsBooked = false;

                // 2. Remove appointment record
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                // 3. Notify Patient
                await _emailService.SendEmailAsync(patientEmail,
                    "Отменен час в MyClinicOnline",
                    $"Здравейте, Вашият записан час при Д-р {doctorName} за {time:dd.MM.yyyy HH:mm} беше отменен от лекаря.");
            }

            return RedirectToAction("MyTimeSlots");
        }

    }
}