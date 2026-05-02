using Microsoft.AspNetCore.Mvc;

namespace ptsamonitor.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Login", "Auth");
    }
}
