using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ICCMS_Web.Controllers
{
    public class ContractorController : Controller
    {
        [AllowAnonymous] // keep anon for this test
        public IActionResult Dashboard()
            => View("~/Views/Contractor/Dashboard.cshtml"); // absolute view path
    }
}
