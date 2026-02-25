using System.Diagnostics;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("admin"))
            {
                return RedirectToAction("Index", "ProductManagement");
            } else
            {
                return RedirectToAction("Index", "Product");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

    }
}
