using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;
using System.Security.Claims;
using ClosedXML.Excel;

namespace LaptopStore.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly LaptopStoreDbContext _context;
        private readonly Services.IEmailService _emailService;
        private readonly Services.IAuthService _authService;

        public UserManagementController(LaptopStoreDbContext context, Services.IEmailService emailService, Services.IAuthService authService)
        {
            _context = context;
            _emailService = emailService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect to CustomerList as default or show a landing page. 
            // For now let's redirect to CustomerList
            return RedirectToAction(nameof(CustomerList));
        }

        public async Task<IActionResult> StaffList(int? pageNumber)
        {
            var currentUserId = 0;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                currentUserId = id;
            }

            var usersQuery = _context.Users
                .Where(u => u.Id != currentUserId && u.Role == "staff")
                .OrderByDescending(u => u.Id)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt
                });

            int pageSize = 10;
            return View("~/Views/Manager/UserList.cshtml", await PaginatedList<UserViewModel>.CreateAsync(usersQuery, pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> CustomerList(int? pageNumber)
        {
            var usersQuery = _context.Users
                .Where(u => u.Role == "customer")
                .OrderByDescending(u => u.Id)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt
                });

            int pageSize = 10;
            return View("~/Views/Manager/UserList.cshtml", await PaginatedList<UserViewModel>.CreateAsync(usersQuery, pageNumber ?? 1, pageSize));
        }

        // GET: /UserManagement/ExportExcel
        public async Task<IActionResult> ExportExcel(string? role)
        {
            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                usersQuery = usersQuery.Where(u => u.Role == role);
            }

            var users = await usersQuery.OrderByDescending(u => u.Id).ToListAsync();

            using var workbook = new XLWorkbook();
            var sheetName = role == "staff" ? "Nhân viên" : role == "customer" ? "Khách hàng" : "Người dùng";
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Header styling
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Email";
            worksheet.Cell(1, 3).Value = "Họ tên";
            worksheet.Cell(1, 4).Value = "Số điện thoại";
            worksheet.Cell(1, 5).Value = "Địa chỉ";
            worksheet.Cell(1, 6).Value = "Vai trò";
            worksheet.Cell(1, 7).Value = "Trạng thái";
            worksheet.Cell(1, 8).Value = "Ngày tạo";

            // Data rows
            int row = 2;
            foreach (var user in users)
            {
                var roleText = user.Role switch
                {
                    "admin" => "Quản trị viên",
                    "staff" => "Nhân viên",
                    "customer" => "Khách hàng",
                    _ => user.Role ?? ""
                };
                var statusText = user.Status switch
                {
                    "active" => "Hoạt động",
                    "inactive" => "Vô hiệu",
                    "pending" => "Chờ xác thực",
                    _ => user.Status ?? ""
                };

                worksheet.Cell(row, 1).Value = user.Id;
                worksheet.Cell(row, 2).Value = user.Email ?? "";
                worksheet.Cell(row, 3).Value = user.FullName ?? "";
                worksheet.Cell(row, 4).Value = user.PhoneNumber ?? "";
                worksheet.Cell(row, 5).Value = user.Address ?? "";
                worksheet.Cell(row, 6).Value = roleText;
                worksheet.Cell(row, 7).Value = statusText;
                worksheet.Cell(row, 8).Value = user.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";

                if (row % 2 == 0)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var dataRange = worksheet.Range(1, 1, row - 1, 8);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var filePrefix = role == "staff" ? "NhanVien" : role == "customer" ? "KhachHang" : "NguoiDung";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Manager/UserCreate.cshtml", new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View("~/Views/Manager/UserCreate.cshtml", model);
                }

                var user = new User
                {
                    Email = model.Email,
                    Password = BCryptNet.HashPassword(Guid.NewGuid().ToString()), // Random password initially
                    FullName = model.FullName,
                    Role = model.Role,
                    Status = "pending", // Status pending verification
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate Auth Token
                var verificationToken = await _authService.GenerateEmailVerificationTokenAsync(user.Id);

                // Send setup email
                var setupLink = Url.Action("SetupAccount", "Account", 
                    new { token = verificationToken, email = user.Email }, Request.Scheme);
                
                if (!string.IsNullOrEmpty(setupLink))
                {
                    await _emailService.SendAccountSetupEmailAsync(user.Email, user.FullName, setupLink);
                }

                TempData["ToastMessage"] = "Tạo người dùng và gửi email xác thực thành công";
                TempData["ToastType"] = "success";
                
                if (model.Role == "staff")
                    return RedirectToAction(nameof(StaffList));
                else
                    return RedirectToAction(nameof(CustomerList));
            }

            return View("~/Views/Manager/UserCreate.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUserId = 0;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId))
            {
                currentUserId = currentId;
            }

            if (id == currentUserId)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == "admin") 
            {
                 // Prevent editing other admins unless super admin logic is implemented, for now block
                 // Or if current user is admin, allow editing staff/customer
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role
            };

            return View("~/Views/Manager/UserEdit.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var currentUserId = 0;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId))
            {
                currentUserId = currentId;
            }

            if (model.Id == currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                if (user.Email != model.Email && await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View("~/Views/Manager/UserEdit.cshtml", model);
                }

                user.Email = model.Email;
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.Role = model.Role;
                user.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    user.Password = BCryptNet.HashPassword(model.NewPassword);
                }

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Cập nhật thông tin thành công";
                TempData["ToastType"] = "success";
                
                if (user.Role == "staff")
                    return RedirectToAction(nameof(StaffList));
                else
                    return RedirectToAction(nameof(CustomerList));
            }

            return View("~/Views/Manager/UserEdit.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUserId = 0;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId))
            {
                currentUserId = currentId;
            }

            if (id == currentUserId)
            {
                return Json(new { success = false, message = "Cannot change your own status" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            if (user.Status == "active")
            {
                user.Status = "locked";
            }
            else
            {
                user.Status = "active";
            }
            
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = user.Status });
        }
    }
}
