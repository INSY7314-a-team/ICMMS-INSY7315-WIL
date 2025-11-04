using System.Diagnostics;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Redirect authenticated users to their role-specific dashboard
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "SystemOverview");
                }
                else if (User.IsInRole("Project Manager"))
                {
                    return RedirectToAction("Dashboard", "ProjectManager");
                }
                else if (User.IsInRole("Contractor"))
                {
                    return RedirectToAction("Dashboard", "Contractor");
                }
                else if (User.IsInRole("Client"))
                {
                    return RedirectToAction("Index", "Clients");
                }
                else if (User.IsInRole("Tester"))
                {
                    return RedirectToAction("Index", "SystemOverview");
                }
            }

            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}
