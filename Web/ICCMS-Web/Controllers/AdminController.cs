using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    public class AdminController : Controller
    {
        // Keep anonymous for local testing; swap to [Authorize(Roles = "Admin")]
        [AllowAnonymous]
        public IActionResult Dashboard()
            => View("~/Views/Admin/Dashboard.cshtml");
    }
}
