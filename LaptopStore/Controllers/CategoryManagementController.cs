using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;
using LaptopStore.DTOs.CategoryDTOs;

public class CategoryManagementController : Controller
{
    private readonly LaptopStoreDbContext _context;

    public CategoryManagementController(LaptopStoreDbContext context)
    {
        _context = context;
    }

    // ================= INDEX =================
    public IActionResult Index()
    {
        var categories = _context.Categories
            .Include(c => c.Products)
            .ToList();

        return View("~/Views/Manager/CategoryManagement.cshtml", categories);
    }

    // ================= DETAIL =================
    public IActionResult CategoryDetails(int id)
    {
        var category = _context.Categories
            .Include(c => c.Products)
            .ThenInclude(p => p.ProductImages)
            .FirstOrDefault(c => c.Id == id);

        if (category == null)
            return NotFound();

        return View("~/Views/Manager/CategoryDetails.cshtml", category);
    }

    // ================= CREATE =================
    public IActionResult AddNewCategory()
    {
        return View("~/Views/Manager/AddNewCategory.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddNewCategory(AddCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Manager/AddNewCategory.cshtml", request);

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        _context.Categories.Add(category);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    // ================= UPDATE =================
    public IActionResult UpdateCategory(int id)
    {
        var category = _context.Categories.Find(id);
        if (category == null)
            return NotFound();

        var model = new UpdateCategoryRequest
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View("~/Views/Manager/UpdateCategory.cshtml", model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCategory(UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Manager/UpdateCategory.cshtml", request);

        var category = _context.Categories.Find(request.Id);
        if (category == null)
            return NotFound();

        category.Name = request.Name.Trim();
        category.Description = request.Description;

        _context.SaveChanges();

        return RedirectToAction("CategoryDetails", new { id = category.Id });
    }

    // ================= DELETE =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCategory(int id)
    {
        var category = _context.Categories
            .Include(c => c.Products)
            .FirstOrDefault(c => c.Id == id);

        if (category == null)
            return NotFound();

        if (category.Products.Any())
        {
            TempData["Error"] = "Không thể xóa vì còn sản phẩm thuộc phân loại này.";
            return RedirectToAction("CategoryDetail", new { id });
        }

        _context.Categories.Remove(category);
        _context.SaveChanges();

        TempData["Success"] = "Xóa phân loại thành công.";
        return RedirectToAction("Index");
    }
}
