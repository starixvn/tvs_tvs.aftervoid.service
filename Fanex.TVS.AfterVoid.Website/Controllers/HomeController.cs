using Microsoft.AspNetCore.Mvc;

namespace Fanex.TVS.AfterVoid.Website.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Ok-200");
        }
    }
}