using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PersonelIzinSistemi.Controllers
{
    [AllowAnonymous]
    public class AnaSayfaController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}