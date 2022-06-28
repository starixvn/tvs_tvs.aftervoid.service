using Microsoft.AspNetCore.Mvc;

namespace Fanex.TVS.AfterVoid.Service.Controllers
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