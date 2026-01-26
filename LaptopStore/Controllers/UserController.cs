using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers
{
    public class UserController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public UserController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        // VIEW PROFILE
        public IActionResult Profile(int id, bool edit = false)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user == null) return NotFound();

            if (edit) ViewBag.EditMode = true;

            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(User model)
        {
            var user = _context.Users.First(x => x.Id == model.Id);
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            _context.SaveChanges();

            return RedirectToAction("Profile", new { id = user.Id });
        }

    }
}
