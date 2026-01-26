using LaptopStore.Extensions;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaptopStore.Controllers
{
    public class UserController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public UserController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        //public IActionResult Index()
        //{
        //    int? userId = Identity.GetUserId(User);

        //    if (userId == null)
        //    {
        //        return Content("Không xác định được UserId");
        //    }

        //    return RedirectToAction("Profile", new { id = userId });
        //}

        //public IActionResult Profile(int id, bool edit = false)
        //{
        //    var user = _context.Users.FirstOrDefault(x => x.Id == id);
        //    if (user == null) return NotFound();

        //    if (edit) ViewBag.EditMode = true;

        //    return View(user);
        //}

        //[HttpPost]
        //public IActionResult Profile(User model)
        //{
        //    var user = _context.Users.First(x => x.Id == model.Id);
        //    user.FullName = model.FullName;
        //    user.PhoneNumber = model.PhoneNumber;
        //    user.Address = model.Address;

        //    _context.SaveChanges();

        //    return RedirectToAction("Profile", new { id = user.Id });
        //}

        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        public IActionResult Profile(bool edit = false)
        {
            int? userId = Identity.GetUserId(User);

            if (userId == null)
            {
                return Content("Không xác định được UserId");
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) return NotFound();

            if (edit) ViewBag.EditMode = true;

            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(User model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);
            if (user == null) return NotFound();

            // Update thông tin thường
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            // 👉 ĐỔI MẬT KHẨU (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            _context.Users.Update(user);
            _context.SaveChanges();
            return RedirectToAction("Profile");
        }

    }
}
