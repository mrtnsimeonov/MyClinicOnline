using Microsoft.AspNetCore.Mvc;

namespace MyClinicOnline.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
