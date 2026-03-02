using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.DTOs.StockDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
namespace LaptopStore.Controllers;

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
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        int totalStockEntries = allStockEntries.Count;
        var stockEntries = allStockEntries.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalStockEntries / pageSize);
        return View("~/Views/Manager/StockManagement.cshtml", stockEntries);
    }

    [HttpGet]
    public IActionResult AddNewStockInOrder()
    {
        ViewBag.Staffs = _context.Users.Where(u => u.Role == "staff").ToList();
        ViewBag.Products = _context.Products.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            Price = p.Price,
            OldPrice = p.OldPrice,
            StockQuantity = p.StockQuantity,
            Description = p.Description,
            ShortDescription = p.ShortDescription,
            Cpu = p.Cpu,
            Ram = p.Ram,
            HardDrive = p.HardDrive,
            Gpu = p.Gpu,
            ScreenSize = p.ScreenSize,
            Weight = p.Weight,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            BrandName = p.Brand != null ? p.Brand.Name : null,
            CategoryName = p.Category != null ? p.Category.Name : null,
            ProductImages = p.ProductImages
        }).ToList();
        return View("~/Views/Manager/AddNewStockInOrder.cshtml");
    }

    [HttpPost]
    public IActionResult AddNewStockInOrder([FromBody] StockInOrderRequest request)
    {
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest("Dữ liệu đơn hàng không hợp lệ.");
        }

        var newImportReceipt = new ImportReceipt
        {
            SupplierName = request.SupplierName,
            StaffId = request.StaffId,
            CreatedAt = DateTime.Now
        };

        _context.ImportReceipts.Add(newImportReceipt);
        _context.SaveChanges();

        int importReceiptId = newImportReceipt.Id;

        foreach (var item in request.Items)
        {
            var importDetail = new ImportDetail
            {
                ReceiptId = importReceiptId,
                ProductId = item.ProductId,
                RequestedQuantity = item.Quantity,
                ActualQuantity = 0
            };
            _context.ImportDetails.Add(importDetail);
        }

        _context.SaveChanges();

        return Ok(new { message = "Lưu đơn hàng thành công"});
    }
    [HttpGet]
    public IActionResult StockDetails(int id)
    {
        var order = _context.ImportReceipts.Include(r => r.ImportDetails).ThenInclude(d => d.Product).FirstOrDefault(r => r.Id == id);
        if (order == null) {
            return NotFound("Không tìm thấy đơn hàng.");
        }
        var orderDto = new StockInOrderResponse
        {
            SupplierName = order.SupplierName ?? "",
            TotalCost = order.TotalCost,
            CreatedAt = order.CreatedAt ?? DateTime.Now,
            DeliveredAt = order.DeliveredAt,
            Items = order.ImportDetails.Select(d => new StockInItemResponse
            {
                ProductId = d.ProductId ?? 0,
                ProductName = d.Product != null ? d.Product.Name : "Không thấy",
                RequestedQuantity = d.RequestedQuantity,
                ActualQuantity = d.ActualQuantity,
                ImportPrice = d.ImportPrice
            }).ToList()
        };

        switch (order.Status)
        {
            case "pending":
                orderDto.Status = "Đang chờ xử lý";
                break;
            case "delivered":
                orderDto.Status = "Đã giao hàng";
                break;
            case "canceled":
                orderDto.Status = "Đã hủy";
                break;
        }

        return View("~/Views/Manager/StockDetails.cshtml", orderDto);
    }
    [Authorize]
    public IActionResult ByStaff(int page = 1)
    {
        int pageSize = 10;

        var staffIdClaim = User.FindFirst("UserId");

        if (staffIdClaim == null)
        {
            return Unauthorized();
        }

        int staffId = int.Parse(staffIdClaim.Value);

        var query = _context.ImportReceipts
            .Where(r => r.StaffId == staffId)
            .OrderByDescending(r => r.CreatedAt);

        int totalItems = query.Count();

        var receipts = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return View("~/Views/Staff/StockManagementStaff.cshtml", receipts);
    }

    [HttpGet]
    public IActionResult StockComfirmStaff(int id)
    {
        Console.WriteLine($"!!!!!!!Stock ID: {id}");
        var order = _context.ImportReceipts
            .Include(r => r.ImportDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefault(r => r.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var orderDto = new StockInOrderResponse
        {
            ReceiptId = order.Id,
            SupplierName = order.SupplierName ?? "",
            TotalCost = order.TotalCost,
            CreatedAt = order.CreatedAt ?? DateTime.Now,
            DeliveredAt = order.DeliveredAt,
            Items = order.ImportDetails.Select(d => new StockInItemResponse
            {
                DetailId = d.Id,
                ProductId = d.ProductId ?? 0,
                ProductName = d.Product != null ? d.Product.Name : "Không thấy",
                RequestedQuantity = d.RequestedQuantity,
                ActualQuantity = d.ActualQuantity,
                ImportPrice = d.ImportPrice,
            }).ToList()
        };

        return View("~/Views/Staff/StockComfirmStaff.cshtml", orderDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAll(UpdateStockRequest request)
    {
        if (request == null || request.Items == null || !request.Items.Any())
            return BadRequest("Không có dữ liệu.");

        var receipt = await _context.ImportReceipts
            .Include(r => r.ImportDetails)
            .FirstOrDefaultAsync(r => r.Id == request.StockId);

        if (receipt == null)
            return NotFound("Không tìm thấy đơn nhập.");

        if (receipt.Status == "Success")
            return BadRequest("Đơn đã hoàn tất.");

        decimal totalCost = 0;

        foreach (var item in request.Items)
        {
            var detail = receipt.ImportDetails
                .FirstOrDefault(d => d.Id == item.DetailId);

            if (detail == null)
                continue;

            if (item.ActualQuantity < 0 || item.ImportPrice < 0)
                return BadRequest("Dữ liệu không hợp lệ.");

            // Cập nhật số lượng và giá
            detail.ActualQuantity = item.ActualQuantity;
            detail.ImportPrice = item.ImportPrice;

            // Tính thành tiền từng dòng
            decimal lineTotal = item.ActualQuantity * item.ImportPrice;

            totalCost += lineTotal;

            // Cập nhật tồn kho
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == detail.ProductId);

            if (product != null)
            {
                product.StockQuantity += item.ActualQuantity;
            }
        }

        // Cập nhật tổng tiền đơn
        receipt.TotalCost = totalCost;

        receipt.Status = "Success";
        receipt.DeliveredAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction("ByStaff");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var receipt = await _context.ImportReceipts
            .Include(r => r.ImportDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null)
            return NotFound();

        // 🔒 CHECK ĐIỀU KIỆN GIỐNG DELETE
        if (receipt.Status != "Success")
        {
            TempData["Error"] = "Chỉ được xóa đơn đã được xác nhận!";
            return RedirectToAction("ByStaff");
        }

        if (receipt.DeliveredAt == null ||
            receipt.DeliveredAt.Value.Date != DateTime.Today)
        {
            TempData["Error"] = "Chỉ được xóa trong ngày xác nhận đơn!";
            return RedirectToAction("ByStaff");
        }

        // 🔒 Kiểm tra tồn kho
        foreach (var detail in receipt.ImportDetails)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == detail.ProductId);

            if (product != null &&
                product.StockQuantity < detail.ActualQuantity)
            {
                TempData["Error"] =
                    $"Không thể xóa. Sản phẩm {product.Name} không đủ tồn kho để hoàn tác.";

                return RedirectToAction("ByStaff");
            }
        }

        return View("~/Views/Staff/StockConfirmDeleteStaff.cshtml", receipt); // Trả về view ConfirmDelete
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var receipt = await _context.ImportReceipts
            .Include(r => r.ImportDetails)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null)
            return NotFound();
        // Chỉ cho xóa khi đã confirm
        if (receipt.Status != "Success")
        {
            TempData["Error"] = "Chỉ được xóa đơn đã được xác nhận!";
            return RedirectToAction("ByStaff");
        }

        // 🔒 Chỉ xóa trong ngày confirm
        if (receipt.DeliveredAt == null ||
        receipt.DeliveredAt.Value.Date != DateTime.Today)
        {
            TempData["Error"] = "Chỉ được xóa trong ngày xác nhận đơn!";
            return RedirectToAction("ByStaff");
        }

        // Nếu đã hoàn tất thì kiểm tra tồn kho
        if (receipt.Status == "Success")
        {
            foreach (var detail in receipt.ImportDetails)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == detail.ProductId);

                if (product == null)
                    continue;

                // 🔒 Không cho xóa nếu kho không đủ để trừ
                if (product.StockQuantity < detail.ActualQuantity)
                {
                    TempData["Error"] = $"Không thể xóa. Sản phẩm {product.Name} không đủ tồn kho để hoàn tác.";

                    return RedirectToAction("ByStaff");
                }
            }

            // Nếu tất cả hợp lệ → trừ kho
            foreach (var detail in receipt.ImportDetails)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == detail.ProductId);

                if (product != null)
                {
                    product.StockQuantity -= detail.ActualQuantity;
                }
            }
        }

        _context.ImportDetails.RemoveRange(receipt.ImportDetails);
        _context.ImportReceipts.Remove(receipt);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa đơn thành công!";
        return RedirectToAction("ByStaff");
    }

}
