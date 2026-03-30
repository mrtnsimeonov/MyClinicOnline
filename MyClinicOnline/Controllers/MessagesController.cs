using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;

namespace MyClinicOnline.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public MessagesController(MyClinicOnlineContext context)
        {
            _context = context;
        }

        // View conversation for an appointment
        [HttpGet]
        public async Task<IActionResult> Conversation(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.User)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();

            var isDoctor = User.IsInRole("Doctor");

            // Mark incoming messages as read
            var unread = await _context.Messages
                .Where(m => m.AppointmentId == appointmentId && m.SentByDoctor != isDoctor && !m.IsRead)
                .ToListAsync();

            unread.ForEach(m => m.IsRead = true);
            await _context.SaveChangesAsync();

            var messages = await _context.Messages
                .Where(m => m.AppointmentId == appointmentId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.Appointment = appointment;
            ViewBag.IsDoctor = isDoctor;
            return View(messages);
        }

        // Send a message
        [HttpPost]
        public async Task<IActionResult> Send(int appointmentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Conversation", new { appointmentId });

            var isDoctor = User.IsInRole("Doctor");

            _context.Messages.Add(new Message
            {
                AppointmentId = appointmentId,
                Content = content.Trim(),
                SentByDoctor = isDoctor,
                SentAt = DateTime.Now,
                IsRead = false
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Conversation", new { appointmentId });
        }

        // API endpoint for unread count (used by navbar badge)
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var isDoctor = User.IsInRole("Doctor");
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return Json(0);
            int userId = int.Parse(userIdClaim);

            IQueryable<Message> query;

            if (isDoctor)
            {
                query = _context.Messages
                    .Where(m => m.Appointment.DoctorId == userId
                             && !m.SentByDoctor
                             && !m.IsRead);
            }
            else
            {
                query = _context.Messages
                    .Where(m => m.Appointment.UserId == userId
                             && m.SentByDoctor
                             && !m.IsRead);
            }

            return Json(await query.CountAsync());
        }
    }
}