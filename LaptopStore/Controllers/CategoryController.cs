using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers
{
    public class CategoryController : Controller
    {
        public readonly LaptopStoreDbContext _context = new LaptopStoreDbContext();
        public IActionResult Index()
        {

            return View("~/Views/Manager/CategoryManagement.cshtml");
        }
    }
}
