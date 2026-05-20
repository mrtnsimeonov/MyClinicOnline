using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using MyClinicOnline.ViewModels;
using MyClinicOnline.Services;

namespace MyClinicOnline.Controllers
{
    public class BookingController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService;

        public BookingController(MyClinicOnlineContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new BookingSearchVm
            {
                Specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync(),
                Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Search(int specialtyId, int cityId)
        {
            var doctors = await _context.Doctors
                .Include(d => d.City)
                .Include(d => d.Specialties).ThenInclude(ds => ds.Specialty)
                .Where(d => d.CityId == cityId)
                .Where(d => d.Specialties.Any(ds => ds.SpecialtyId == specialtyId))
                .Where(d => d.IsApproved)  
                .OrderBy(d => d.FullName)
                .ToListAsync();

            var spec = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId);
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == cityId);

            ViewBag.Specialty = spec?.Name;
            ViewBag.City = city?.Name;

            return View(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> DoctorSlots(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.TimeSlots)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizeBooking(int slotId)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return Json(new { success = false, message = "Моля, влезте в профила си." });

                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (userIdClaim == null)
                    return Json(new { success = false, message = "Моля, влезте в профила си." });
                int patientId = int.Parse(userIdClaim);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == patientId);
                var slot = await _context.TimeSlots.Include(s => s.Doctor).FirstOrDefaultAsync(s => s.Id == slotId);

                if (slot == null || slot.IsBooked)
                    return Json(new { success = false, message = "Часът вече е зает." });

                // Save Appointment
                var appointment = new Appointment
                {
                    DoctorId = slot.DoctorId,
                    UserId = user.Id,
                    TimeSlotId = slot.Id,
                    Status = AppointmentStatus.Confirmed
                };

                slot.IsBooked = true;
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // SEND EMAIL
                await _emailService.SendEmailAsync(user.Email, "Записан час", $"Успешно записахте час при Д-р {slot.Doctor.FullName}.");

                // Return SUCCESS to the JavaScript
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // 1. Show Appointments
        public async Task<IActionResult> MyAppointments()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int patientId = int.Parse(userIdClaim);

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.TimeSlot)
                .Where(a => a.UserId == patientId)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .Select(a => new MyAppointmentViewModel
                {
                    AppointmentId = a.Id,
                    SlotId = a.TimeSlotId,
                    DoctorName = a.Doctor.FullName,
                    DateTime = a.TimeSlot.StartTime,
                    IsPast = a.TimeSlot.StartTime < DateTime.Now,
                    ConsultationType = a.ConsultationType,
                    MeetingCode = a.MeetingCode
                })
                .ToListAsync();

            return View(appointments);
        }

        // 2. Cancel Appointment
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int appointmentId, int slotId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            var slot = await _context.TimeSlots.FindAsync(slotId);

            if (appointment != null && slot != null)
            {
                _context.Appointments.Remove(appointment);
                slot.IsBooked = false; // Make the hour available again!
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyAppointments");
        }
    }

    // THIS IS THE CLASS THAT WAS MISSING CAUSING THE ERROR!
    public class BookingSearchVm
    {
        public int SpecialtyId { get; set; }
        public int CityId { get; set; }
        public List<Specialty> Specialties { get; set; } = new();
        public List<City> Cities { get; set; } = new();
    }
}