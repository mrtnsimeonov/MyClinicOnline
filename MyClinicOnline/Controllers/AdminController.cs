using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;
using MyClinicOnline.Services;


namespace MyClinicOnline.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MyClinicOnlineContext _context;
        private readonly IEmailService _emailService;

        public AdminController(MyClinicOnlineContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── DASHBOARD ────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            ViewBag.TotalPatients = await _context.Users.CountAsync(u => !u.IsAdmin);
            ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
            ViewBag.ApprovedDoctors = await _context.Doctors.CountAsync(d => d.IsApproved);
            ViewBag.PendingDoctors = await _context.Doctors.CountAsync(d => !d.IsApproved);
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.AppointmentsThisMonth = await _context.Appointments
                .Include(a => a.TimeSlot)
                .CountAsync(a => a.TimeSlot.StartTime >= monthStart);
            ViewBag.OnlineAppointments = await _context.Appointments
                .CountAsync(a => a.ConsultationType == ConsultationType.Online);
            ViewBag.InPersonAppointments = await _context.Appointments
                .CountAsync(a => a.ConsultationType == ConsultationType.InPerson);

            // Recent 5 registrations
            ViewBag.RecentDoctors = await _context.Doctors
                .OrderByDescending(d => d.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ── DOCTORS ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors
                .Include(d => d.City)
                .Include(d => d.Specialties).ThenInclude(ds => ds.Specialty)
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(doctors);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return NotFound();

            doctor.IsApproved = true;
            await _context.SaveChangesAsync();

            // Notify doctor by email
            try
            {
                await _emailService.SendEmailAsync(
                    doctor.Email,
                    "Account Approved – MyClinicOnline",
                    $"Dear Dr. {doctor.FullName},\n\n" +
                    $"Your account has been approved! You can now log in and patients can book appointments with you.\n\n" +
                    $"Welcome to MyClinicOnline!"
                );
            }
            catch { }

            TempData["Success"] = $"Dr. {doctor.FullName} has been approved.";
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public async Task<IActionResult> RejectDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return NotFound();

            try
            {
                await _emailService.SendEmailAsync(
                    doctor.Email,
                    "Account Registration Update – MyClinicOnline",
                    $"Dear Dr. {doctor.FullName},\n\n" +
                    $"Unfortunately, your registration could not be approved at this time. " +
                    $"Please contact our support team for more information."
                );
            }
            catch { }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Doctor registration rejected and removed.";
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.TimeSlots)
                .Include(d => d.Appointments)
                .Include(d => d.Specialties)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null) return NotFound();

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Doctor deleted successfully.";
            return RedirectToAction("Doctors");
        }

        // ── PATIENTS ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Users
                .Where(u => !u.IsAdmin)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Phone,
                    u.Region,
                    AppointmentCount = u.Appointments.Count
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(patients.Select(u => new AdminPatientVm
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Region = u.Region,
                AppointmentCount = u.AppointmentCount
            }).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            var user = await _context.Users.FindAsync(patientId);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Patient removed successfully.";
            return RedirectToAction("Patients");
        }

        // ── ALL APPOINTMENTS ─────────────────────────────────────────────────
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.User)
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        // ── SPECIALTIES ──────────────────────────────────────────────────────
        public async Task<IActionResult> Specialties()
        {
            var specialties = await _context.Specialties.OrderBy(s => s.Name).ToListAsync();
            return View(specialties);
        }

        [HttpPost]
        public async Task<IActionResult> AddSpecialty(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) &&
                !await _context.Specialties.AnyAsync(s => s.Name == name.Trim()))
            {
                _context.Specialties.Add(new Specialty { Name = name.Trim() });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Specialty '{name}' added.";
            }
            return RedirectToAction("Specialties");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialty(int specialtyId)
        {
            var specialty = await _context.Specialties.FindAsync(specialtyId);
            if (specialty != null)
            {
                _context.Specialties.Remove(specialty);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Specialty deleted.";
            }
            return RedirectToAction("Specialties");
        }

        // ── CITIES ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Cities()
        {
            var cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            return View(cities);
        }

        [HttpPost]
        public async Task<IActionResult> AddCity(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) &&
                !await _context.Cities.AnyAsync(c => c.Name == name.Trim()))
            {
                _context.Cities.Add(new City { Name = name.Trim() });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"City '{name}' added.";
            }
            return RedirectToAction("Cities");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCity(int cityId)
        {
            var city = await _context.Cities.FindAsync(cityId);
            if (city != null)
            {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
                TempData["Success"] = "City deleted.";
            }
            return RedirectToAction("Cities");
        }
    }

    // ViewModel for patients list
    public class AdminPatientVm
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Region { get; set; }
        public int AppointmentCount { get; set; }
    }
}