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

        // Асинхронен метод за зареждане на падащите менюта
        public async Task<IActionResult> Index()
        {
            var vm = new BookingSearchVm
            {
                // Използваме ToListAsync(), за да не блокираме нишката
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

            // Използваме FirstOrDefaultAsync вместо Find, за да е напълно асинхронно
            var spec = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId);
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == cityId);

            ViewBag.Specialty = spec?.Name;
            ViewBag.City = city?.Name;

            return View(doctors);
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