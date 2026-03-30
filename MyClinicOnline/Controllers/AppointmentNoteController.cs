using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;

namespace MyClinicOnline.Controllers
{
    [Authorize]
    public class AppointmentNoteController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public AppointmentNoteController(MyClinicOnlineContext context)
        {
            _context = context;
        }

        // ── PATIENT: fill symptom form after booking ──────────────────────
        [HttpGet]
        public async Task<IActionResult> Symptoms(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();

            // Check if note already exists
            var existing = await _context.AppointmentNotes
                .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId);

            ViewBag.Appointment = appointment;
            return View(existing ?? new AppointmentNote { AppointmentId = appointmentId });
        }

        [HttpPost]
        public async Task<IActionResult> Symptoms(AppointmentNote model)
        {
            var existing = await _context.AppointmentNotes
                .FirstOrDefaultAsync(n => n.AppointmentId == model.AppointmentId);

            if (existing == null)
            {
                _context.AppointmentNotes.Add(model);
            }
            else
            {
                existing.MainComplaint = model.MainComplaint;
                existing.Symptoms = model.Symptoms;
                existing.SymptomDuration = model.SymptomDuration;
                existing.AdditionalPatientNotes = model.AdditionalPatientNotes;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyAppointments", "Booking");
        }

        // Skip symptom form — patient chose not to fill it
        [HttpGet]
        public IActionResult Skip(int appointmentId)
        {
            return RedirectToAction("MyAppointments", "Booking");
        }

        // ── PATIENT: view their full record for a past appointment ─────────
        [HttpGet]
        public async Task<IActionResult> PatientRecord(int appointmentId)
        {
            var note = await _context.AppointmentNotes
                .Include(n => n.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(n => n.Appointment)
                    .ThenInclude(a => a.TimeSlot)
                .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId);

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();

            ViewBag.Appointment = appointment;
            return View(note);
        }

        // ── DOCTOR: view patient symptoms + add medical notes ──────────────
        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorNotes(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();

            var note = await _context.AppointmentNotes
                .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId)
                ?? new AppointmentNote { AppointmentId = appointmentId };

            ViewBag.Appointment = appointment;
            return View(note);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorNotes(AppointmentNote model)
        {
            var existing = await _context.AppointmentNotes
                .FirstOrDefaultAsync(n => n.AppointmentId == model.AppointmentId);

            if (existing == null)
            {
                model.DoctorNotesAddedAt = DateTime.Now;
                _context.AppointmentNotes.Add(model);
            }
            else
            {
                existing.Diagnosis = model.Diagnosis;
                existing.Prescription = model.Prescription;
                existing.DoctorNotes = model.DoctorNotes;
                existing.NextSteps = model.NextSteps;
                existing.DoctorNotesAddedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyTimeSlots", "Doctor");
        }
    }
}