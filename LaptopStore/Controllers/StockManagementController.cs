using Azure.Core;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Excel;
using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.DTOs.StockDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LaptopStore.Controllers;

public class StockManagementController : Controller
{
    private readonly ILogger<StockManagementController> _logger;
    private readonly LaptopStoreDbContext _context;
    
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
        ViewBag.Staffs = _context.Users.Where(u => u.Role == "staff" && !u.Status.Equals("banned")).ToList();
        ViewBag.Products = _context.Products.Include(p => p.ProductImages).Select(p => new ProductResponse
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
            ProductImages = p.ProductImages.Select(i => new ProductImageResponse
            {
                ImageUrl = i.ImageUrl,
                IsThumbnail = i.IsThumbnail
            }).ToList()
        }).ToList();
        return View("~/Views/Manager/AddNewStockInOrder.cshtml");
    }

    [HttpPost] 
    public IActionResult AddNewStockInOrder(string supplierName, int staffId, string itemsJson)
    {
        if (string.IsNullOrEmpty(itemsJson)) return BadRequest("Danh sách trống");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = JsonSerializer.Deserialize<List<StockInItemDto>>(itemsJson, options);


        var model = new StockInOrderRequest
        {
            SupplierName = supplierName,
            StaffId = staffId,
            StaffName = _context.Users.Where(u => u.Id == staffId).Select(u => u.FullName).FirstOrDefault() ?? "Không thấy",
            Items = items.Select(i => new StockInItemRequest
            {
                Product = new ProductResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Sku = i.Sku,
                    BrandName = i.BrandName,
                    CategoryName = i.CategoryName,
                    Price = i.Price,
                    OldPrice = i.OldPrice,
                    StockQuantity = i.StockQuantity,
                    Description = i.Description,
                    ShortDescription = i.ShortDescription,
                    Cpu = i.Cpu,
                    Ram = i.Ram,
                    HardDrive = i.HardDrive,
                    Gpu = i.Gpu,
                    ScreenSize = i.ScreenSize,
                    Weight = i.Weight,
                    IsActive = i.IsActive,
                    ProductImages = i.ProductImages,
                    CreatedAt = i.CreatedAt
                },
                Quantity = i.Quantity
            }).ToList()
        };

        return View("~/Views/Manager/ConfirmAddNewStockInOrder.cshtml", model);
    }

    [HttpPost]
    public IActionResult ConfirmAddNewStockInOrder(StockInOrderRequest request)
    {
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest("Dữ liệu đơn hàng không hợp lệ.");
        }
 
        var newImportReceipt = new ImportReceipt
        {
            SupplierName = request.SupplierName,
            StaffId = request.StaffId,
            CreatedAt = DateTime.Now,
            Status = "pending"
        };

        _context.ImportReceipts.Add(newImportReceipt);
        _context.SaveChanges();

        int importReceiptId = newImportReceipt.Id;

        foreach (var item in request.Items)
        {
            var importDetail = new ImportDetail
            {
                ReceiptId = importReceiptId,
                ProductId = item.Product.Id,
                RequestedQuantity = item.Quantity,
                ActualQuantity = 0
            };
            _context.ImportDetails.Add(importDetail);
        }

        _context.SaveChanges();

        //Create notification for staff
        var notification = new Notification
        {
            UserId = request.StaffId,
            Title = $"Bạn được giao đơn nhập hàng mới mã #{importReceiptId}",
            Message = $"Đơn nhập hàng từ nhà cung cấp {request.SupplierName} đã được tạo. Vui lòng kiểm tra và xác nhận đơn hàng.",
            Type = "receipt",
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);

        _context.SaveChanges();

        return RedirectToAction("StockDetails", new { id = importReceiptId });
    }
    
    [HttpGet]
    public IActionResult StockDetails(int id)
    {
        var order = _context.ImportReceipts.Include(r => r.ImportDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductImages).Include(r => r.Staff).FirstOrDefault(r => r.Id == id);
        if (order == null) {
            return NotFound("Không tìm thấy đơn hàng.");
        }
        var orderDto = new StockInOrderResponse
        {
            Id = id,
            SupplierName = order.SupplierName ?? "",
            StaffName = order.Staff?.FullName ?? "Không thấy",
            StaffAvatarUrl = order.Staff?.AvatarUrl ?? "/images/image-not-found.jpg",
            StaffEmail = order.Staff?.Email ?? "Không thấy",
            TotalCost = order.TotalCost,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            DeliveredAt = order.DeliveredAt,
            Items = order.ImportDetails.Select(d => new StockInItemResponse
            {
                ProductId = d.ProductId ?? 0,
                ProductName = d.Product != null ? d.Product.Name : "Không thấy",
                ImageUrl = d.Product?.ProductImages.FirstOrDefault(i => i.IsThumbnail == true)?.ImageUrl ?? "/images/image-not-found.jpg",
                RequestedQuantity = d.RequestedQuantity,
                ActualQuantity = d.ActualQuantity,
                ImportPrice = d.ImportPrice
            }).ToList()
        };

        return View("~/Views/Manager/StockDetails.cshtml", orderDto);
    }

    [HttpPost]
    public IActionResult CancelPendingReceipt(int id)
    {
        var receipt = _context.ImportReceipts.Include(r => r.ImportDetails).Include(r => r.Staff).FirstOrDefault(r => r.Id == id);
        if (receipt != null)
        {
            _context.ImportDetails.RemoveRange(receipt.ImportDetails);
            _context.ImportReceipts.Remove(receipt);
            _context.SaveChanges();
        }

        //Create notification for staff
        var notification = new Notification
        {
            UserId = _context.Users.FirstOrDefault(u => u.Role == "admin")?.Id ?? 0,
            Title = $"Đơn nhập hàng mã #{id} đã bị hủy",
            Message = $" Nhân viên {receipt?.Staff?.FullName ?? "Không rõ"} đã hủy đơn nhập hàng mã #{id} từ nhà cung cấp {receipt.SupplierName}.",
            Type = "receipt",
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);

        _context.SaveChanges();

        return RedirectToAction("Index");
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
            Id = order.Id,
            SupplierName = order.SupplierName ?? "",
            TotalCost = order.TotalCost,
            CreatedAt = order.CreatedAt,
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
            .Include(r => r.Staff)
            .FirstOrDefaultAsync(r => r.Id == request.StockId);

        if (receipt == null)
            return NotFound("Không tìm thấy đơn nhập.");

        if (receipt.Status.ToLower() == "success")
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

        receipt.Status = "success";
        receipt.DeliveredAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = _context.Users.FirstOrDefault(u => u.Role == "admin")?.Id ?? 0,
            Title = $"Đơn nhập hàng mã #{request.StockId} đã được xác nhận",
            Message = $" Nhân viên {receipt?.Staff?.FullName ?? "Không rõ"} đã xác nhận đơn nhập hàng mã #{request.StockId} từ nhà cung cấp {receipt.SupplierName}.",
            Type = "receipt",
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        await _context.Notifications.AddAsync(notification);

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
        if (receipt.Status.ToLower() != "success")
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
            .Include(r => r.Staff)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null)
            return NotFound();
        // Chỉ cho xóa khi đã confirm
        if (receipt.Status.ToLower() != "success")
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
        if (receipt.Status.ToLower() == "success")
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

        var notification = new Notification
        {
            UserId = _context.Users.FirstOrDefault(u => u.Role == "admin")?.Id ?? 0,
            Title = $"Đơn nhập hàng mã #{id} đã được xóa",
            Message = $" Nhân viên {receipt?.Staff?.FullName ?? "Không rõ"} đã xóa nhập hàng mã #{id} từ nhà cung cấp {receipt.SupplierName}.",
            Type = "receipt",
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        await _context.Notifications.AddAsync(notification);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa đơn thành công!";
        return RedirectToAction("ByStaff");
    }

}
