using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers;

public class ErrorsController : Controller
{
    [AllowAnonymous] 
    [Route("Errors/Error404")]
    public IActionResult Error404()
    {
        Response.StatusCode = 404;
        return View();
    }
}
