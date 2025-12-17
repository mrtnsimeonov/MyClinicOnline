using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;

namespace MyClinicOnline.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public DoctorsController(MyClinicOnlineContext context)
        {
            _context = context;
        }

        // GET: /Doctors/Search?specialty=Cardiology
        public IActionResult Search(string specialty)
        {
            if (string.IsNullOrWhiteSpace(specialty))
            {
                return View(new List<Models.Doctor>());
            }

            var doctors = _context.Doctors
                .Include(d => d.Specialties)
                    .ThenInclude(ds => ds.Specialty)
                .Where(d =>
                    d.Specialties.Any(ds =>
                        ds.Specialty.Name.Contains(specialty)))
                .ToList();

            ViewBag.Specialty = specialty;
            return View(doctors);
        }
    }
}
