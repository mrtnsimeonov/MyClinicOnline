using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Data;
using MyClinicOnline.Models;

namespace MyClinicOnline.Controllers
{
    public class BookingController : Controller
    {
        private readonly MyClinicOnlineContext _context;

        public BookingController(MyClinicOnlineContext context)
        {
            _context = context;
        }

        // NEW PAGE: dropdowns
        public IActionResult Index()
        {
            var vm = new BookingSearchVm
            {
                Specialties = _context.Specialties.OrderBy(s => s.Name).ToList(),
                Cities = _context.Cities.OrderBy(c => c.Name).ToList()
            };

            return View(vm);
        }

        // Search doctors by selected specialty + city
        [HttpGet]
        public IActionResult Search(int specialtyId, int cityId)
        {
            var doctors = _context.Doctors
                .Include(d => d.City)
                .Include(d => d.Specialties).ThenInclude(ds => ds.Specialty)
                .Where(d => d.CityId == cityId)
                .Where(d => d.Specialties.Any(ds => ds.SpecialtyId == specialtyId))
                .OrderBy(d => d.FullName)
                .ToList();

            ViewBag.Specialty = _context.Specialties.Find(specialtyId)?.Name;
            ViewBag.City = _context.Cities.Find(cityId)?.Name;

            return View(doctors);
        }
    }

    // ViewModel for dropdown page
    public class BookingSearchVm
    {
        public int SpecialtyId { get; set; }
        public int CityId { get; set; }

        public List<Specialty> Specialties { get; set; } = new();
        public List<City> Cities { get; set; } = new();
    }
}
