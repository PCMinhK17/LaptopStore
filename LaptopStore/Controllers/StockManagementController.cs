using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.DTOs.StockDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        int pageSize = 99;
        var allStockEntries = _context.ImportReceipts
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

}
