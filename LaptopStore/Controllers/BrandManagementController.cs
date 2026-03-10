using LaptopStore.DTOs;
using LaptopStore.DTOs.BrandDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class BrandManagementController : Controller
{
    private readonly LaptopStoreDbContext _context;

    public BrandManagementController(LaptopStoreDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var brands = _context.Brands
            .Include(b => b.Products)
            .Select(b => new BrandResponse
            {
                Id = b.Id,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                Origin = b.Origin,
                TotalProducts = b.Products.Count
            }).ToList();

        return View("~/Views/Manager/BrandManagement.cshtml", brands);
    }
    public IActionResult AddNewBrand()
    {
        return View("~/Views/Manager/AddNewBrand.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddNewBrand(AddNewBrandRequest request)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Manager/AddNewBrand.cshtml", request);

        var exists = _context.Brands
            .Any(b => b.Name.Trim().ToLower()
                   == request.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError("Name", "Tên thương hiệu đã tồn tại.");
            return View("~/Views/Manager/AddNewBrand.cshtml", request);
        }

        string logoPath = null;

        if (request.LogoFile != null)
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/brands");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.LogoFile.FileName);
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                request.LogoFile.CopyTo(stream);
            }

            logoPath = "/images/brands/" + fileName;
        }

        var brand = new Brand
        {
            Name = request.Name.Trim(),
            Origin = request.Origin,
            LogoUrl = logoPath
        };

        _context.Brands.Add(brand);
        _context.SaveChanges();

        TempData["Success"] = "Thêm thương hiệu thành công.";
        return RedirectToAction("Index");
    }
    public IActionResult BrandDetails(int id)
    {
        var brand = _context.Brands
            .Include(b => b.Products)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefault(b => b.Id == id);

        if (brand == null)
            return NotFound();

        return View("~/Views/Manager/BrandDetails.cshtml", brand);
    }
    public IActionResult UpdateBrand(int id)
    {
        var brand = _context.Brands.FirstOrDefault(b => b.Id == id);

        if (brand == null)
            return NotFound();

        var model = new UpdateBrandRequest
        {
            Id = brand.Id,
            Name = brand.Name,
            Origin = brand.Origin
        };

        return View("~/Views/Manager/UpdateBrand.cshtml", model);
    }
    [HttpPost]
    public IActionResult UpdateBrand(UpdateBrandRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Manager/UpdateBrand.cshtml", request);
        }

        var brand = _context.Brands.Find(request.Id);
        if (brand == null)
            return NotFound();

        brand.Name = request.Name;
        brand.Origin = request.Origin;

        var exists = _context.Brands
    .Any(b => b.Id != request.Id &&
              b.Name.Trim().ToLower()
              == request.Name.Trim().ToLower());

        if (exists)
        {
            ModelState.AddModelError("Name", "Tên thương hiệu đã tồn tại.");
            return View("~/Views/Manager/UpdateBrand.cshtml", request);
        }
        _context.SaveChanges();

        return RedirectToAction("BrandDetails", new { id = brand.Id });
    }

    public IActionResult DeleteBrand(int id)
    {
        var brand = _context.Brands
            .Include(b => b.Products)
            .FirstOrDefault(b => b.Id == id);

        if (brand == null)
            return NotFound();

        return View("~/Views/Manager/DeleteBrand.cshtml", brand);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteBrandConfirmed(int id)
    {
        var brand = _context.Brands
            .Include(b => b.Products)
            .FirstOrDefault(b => b.Id == id);

        if (brand == null)
            return NotFound();

        // ❗ Không cho xóa nếu còn sản phẩm
        if (brand.Products != null && brand.Products.Any())
        {
            TempData["Error"] = "Không thể xóa thương hiệu vì vẫn còn sản phẩm thuộc thương hiệu này.";
            return RedirectToAction("BrandDetails", new { id = id });
        }

        _context.Brands.Remove(brand);
        _context.SaveChanges();

        TempData["Success"] = "Xóa thương hiệu thành công.";
        return RedirectToAction("Index");
    }

}
