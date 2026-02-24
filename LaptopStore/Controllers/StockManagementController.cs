using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;

namespace LaptopStore.Controllers
{
    public class StockManagementController : Controller
    {
        private readonly ILogger<StockManagementController> _logger;
        private readonly LaptopStoreDbContext _context = new LaptopStoreDbContext();
        
        public StockManagementController(ILogger<StockManagementController> logger, LaptopStoreDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index(int page = 1)
        {
            int pageSize = 9;
            var allStockEntries = _context.ImportReceipts
                .ToList();

            int totalStockEntries = allStockEntries.Count;
            var stockEntries = allStockEntries.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStockEntries / pageSize);
            return View("~/Views/Manager/StockManagement.cshtml", stockEntries);
        }

        
    }
}
