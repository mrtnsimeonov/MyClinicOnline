using Microsoft.AspNetCore.Mvc;

namespace MyClinicOnline.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
