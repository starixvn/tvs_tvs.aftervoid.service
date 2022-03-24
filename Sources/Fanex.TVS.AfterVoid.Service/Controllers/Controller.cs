using Microsoft.AspNetCore.Mvc;

namespace Fanex.TVS.AfterVoid.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controller : ControllerBase
    {
        private const string Status = "OK";

        [HttpGet]
        public string Index()
        {
            return Status;
        }
    }
}