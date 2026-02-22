using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using MyClinicOnline.Services;

namespace MyClinicOnline.Controllers
{
    public class BookingController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService; // 🆕 Inject service

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
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Patient"))
            {
                TempData["LoginMessage"] = "Трябва да сте влезли в профила си като пациент, за да запишете час!";
                return RedirectToAction("Login", "Account");
            }

            var slot = await _context.TimeSlots.Include(s => s.Doctor).FirstOrDefaultAsync(s => s.Id == slotId);
            if (slot == null || slot.IsBooked) return BadRequest("Този час вече е зает.");

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return BadRequest();
            int patientId = int.Parse(userIdClaim);

            var appointment = new Appointment
            {
                DoctorId = slot.DoctorId,
                UserId = patientId,
                TimeSlotId = slot.Id,
                Status = AppointmentStatus.Confirmed,
                ConsultationType = (ConsultationType)0
            };

            slot.IsBooked = true;
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // 🆕 Updated email text format
            string patientEmail = User.Identity.Name;
            string body = $"Successfully booked an hour with Dr. {slot.Doctor.FullName} from {slot.StartTime:HH:mm} on {slot.StartTime:dd.MM.yyyy}.";
            await _emailService.SendEmailAsync(patientEmail, "Booking Confirmation", body);

            return View("BookingSuccess");
        }
    }

    public class BookingSearchVm
    {
        public int SpecialtyId { get; set; }
        public int CityId { get; set; }
        public List<Specialty> Specialties { get; set; } = new();
        public List<City> Cities { get; set; } = new();
    }
}