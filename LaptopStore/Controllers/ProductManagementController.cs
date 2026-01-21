using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

public class ProductManagementController : Controller
{
    public IActionResult Index()
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var products = context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.ProductImages).ToList();
            return View("~/Views/Manager/ProductManagement.cshtml", products);
        }
    }

    [HttpGet]
    public IActionResult AddNewProduct()
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            ViewBag.Brands = context.Brands.ToList();
            ViewBag.Categories = context.Categories.ToList();

            return View("~/Views/Manager/AddNewProduct.cshtml");
        }
    }

    [HttpPost]
    public IActionResult AddNewProduct(Product newProduct)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            if (!ModelState.IsValid)
            {

                ViewBag.Brands = context.Brands.ToList();
                ViewBag.Categories = context.Categories.ToList();

                return View("~/Views/Manager/AddNewProduct.cshtml", newProduct);
            }

            context.Products.Add(newProduct);
            context.SaveChanges();

            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public IActionResult ProductDetails(int id)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Manager/ProductDetails.cshtml", product);
        }
    }
}
