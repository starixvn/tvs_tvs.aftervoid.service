using Microsoft.AspNetCore.Mvc;

namespace Fanex.TVS.AfterVoid.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controller : ControllerBase
    {
        [HttpGet]
        public string Index()
        {
            return "OK";
        }
    }
}