//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using ptsamonitor.Classes.Utils;
//using ptsamonitor.Data;
//using ptsamonitor.Models.ViewModels;

//namespace ptsamonitor.Controllers;

//[Authorize]
//public class DashboardController : Controller
//{
//    private readonly AppDbContext _db;
//    private readonly IConfiguration _config;
//    private readonly IWebHostEnvironment _env;

//    public DashboardController(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
//    {
//        _db = db;
//        _config = config;
//        _env = env;
//    }

//    // ══════════════════════════════════════════════════════════════════════
//    // VIEWS
//    // ══════════════════════════════════════════════════════════════════════

//    public IActionResult Index()
//    {
//        return View("~/Views/Dashboard/Index.cshtml");
//    }

//    [Route("Dashboard/Transactions")]
//    public IActionResult Transactions()
//    {
//        return View("~/Views/Dashboard/Transactions.cshtml");
//    }

//    [Route("Dashboard/Institutions")]
//    public IActionResult Institutions()
//    {
//        return View("~/Views/Dashboard/Institutions.cshtml");
//    }

//    [Route("Dashboard/Users")]
//    public IActionResult Users()
//    {
//        return View("~/Views/Dashboard/Users.cshtml");
//    }

//    [Route("Dashboard/Audit")]
//    public IActionResult Audit()
//    {
//        return View("~/Views/Dashboard/Audit.cshtml");
//    }

//    // ══════════════════════════════════════════════════════════════════════
//    // INSTITUTIONS API
//    // ══════════════════════════════════════════════════════════════════════

//    [HttpGet]
//    [Route("Dashboard/Institutions/GetAll")]
//    public async Task<IActionResult> InstitutionsGetAll()
//    {
//        var institutions = await GlobalFunctions.GetAllInstitutionsAsync(_db);
//        return Json(institutions);
//    }

//    [HttpPost]
//    [Route("Dashboard/Institutions/Create")]
//    public async Task<IActionResult> InstitutionsCreate([FromForm] CreateInstitutionRequest req, IFormFile? logoFile)
//    {
//        string? logoPath = null;

//        if (logoFile != null && logoFile.Length > 0)
//        {
//            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logos");
//            Directory.CreateDirectory(uploadsDir);

//            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(logoFile.FileName)}";
//            var filePath = Path.Combine(uploadsDir, safeName);

//            using (var stream = new FileStream(filePath, FileMode.Create))
//            {
//                await logoFile.CopyToAsync(stream);
//            }

//            logoPath = $"/uploads/logos/{safeName}";
//        }

//        var (result, error) = await GlobalFunctions.CreateInstitutionAsync(_db, req, logoPath);

//        if (error == "CONFLICT")
//            return Conflict(new { message = "An institution with this name already exists." });

//        if (error is not null)
//            return BadRequest(new { message = error });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - CREATED INSTITUTION - {req.InstitutionName}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(result);
//    }

//    [HttpPost]
//    [Route("Dashboard/Institutions/Update/{id:int}")]
//    public async Task<IActionResult> InstitutionsUpdate(int id, [FromForm] CreateInstitutionRequest req, IFormFile? logoFile)
//    {
//        string? logoPath = null;

//        if (logoFile != null && logoFile.Length > 0)
//        {
//            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logos");
//            Directory.CreateDirectory(uploadsDir);

//            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(logoFile.FileName)}";
//            var filePath = Path.Combine(uploadsDir, safeName);

//            using (var stream = new FileStream(filePath, FileMode.Create))
//            {
//                await logoFile.CopyToAsync(stream);
//            }

//            logoPath = $"/uploads/logos/{safeName}";
//        }

//        var (result, error) = await GlobalFunctions.UpdateInstitutionAsync(_db, id, req, logoPath);

//        if (error == "NOT_FOUND")
//            return NotFound(new { message = "Institution not found." });

//        if (error is not null)
//            return BadRequest(new { message = error });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - UPDATED INSTITUTION ID: {id}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(result);
//    }

//    [HttpPost]
//    [Route("Dashboard/Institutions/Delete/{id:int}")]
//    public async Task<IActionResult> InstitutionsDelete(int id)
//    {
//        var error = await GlobalFunctions.DeleteInstitutionAsync(_db, id);

//        if (error == "NOT_FOUND")
//            return NotFound(new { message = "Institution not found." });

//        if (error == "HAS_USERS")
//            return BadRequest(new { message = "Cannot delete institution with assigned users." });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - DELETED INSTITUTION ID: {id}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(new { message = "Institution deleted." });
//    }

//    // ══════════════════════════════════════════════════════════════════════
//    // USERS API
//    // ══════════════════════════════════════════════════════════════════════

//    [HttpGet]
//    [Route("Dashboard/Users/GetAll")]
//    public async Task<IActionResult> UsersGetAll()
//    {
//        var users = await GlobalFunctions.GetAllUsersAsync(_db);
//        return Json(users);
//    }

//    [HttpPost]
//    [Route("Dashboard/Users/Create")]
//    public async Task<IActionResult> UsersCreate([FromBody] CreateUserRequest req)
//    {
//        var (result, error) = await GlobalFunctions.CreateUserAsync(
//            _db, _config,
//            req.UserName, req.Email, req.Institution, req.UserType, req.Privileges);

//        if (error == "CONFLICT")
//            return Conflict(new { message = "A user with this username already exists." });

//        if (error is not null)
//            return BadRequest(new { message = error });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - CREATED USER - {req.UserName}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(result);
//    }

//    [HttpPost]
//    [Route("Dashboard/Users/Update/{id:int}")]
//    public async Task<IActionResult> UsersUpdate(int id, [FromBody] CreateUserRequest req)
//    {
//        var (result, error) = await GlobalFunctions.UpdateUserAsync(_db, id, req);

//        if (error == "NOT_FOUND")
//            return NotFound(new { message = "User not found." });

//        if (error is not null)
//            return BadRequest(new { message = error });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - UPDATED USER ID: {id}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(result);
//    }

//    [HttpPost]
//    [Route("Dashboard/Users/Delete/{id:int}")]
//    public async Task<IActionResult> UsersDelete(int id)
//    {
//        var error = await GlobalFunctions.DeleteUserAsync(_db, id);

//        if (error == "NOT_FOUND")
//            return NotFound(new { message = "User not found." });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - DELETED USER ID: {id}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(new { message = "User deleted." });
//    }

//    [HttpPost]
//    [Route("Dashboard/Users/UpdateStatus/{id:int}")]
//    public async Task<IActionResult> UsersUpdateStatus(int id, [FromBody] UpdateStatusRequest req)
//    {
//        var (result, error) = await GlobalFunctions.UpdateUserStatusAsync(_db, id, req.Status);

//        if (error == "NOT_FOUND")
//            return NotFound(new { message = "User not found." });

//        if (error is not null)
//            return BadRequest(new { message = error });

//        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var userName = User.FindFirstValue(ClaimTypes.Name);

//        await AuditLogger.LogAsync(
//            db: _db,
//            eventName: $"{userName} - UPDATED USER ID: {id} STATUS TO {req.Status}",
//            userId: userId,
//            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
//            pageUrl: HttpContext.Request.Path
//        );

//        return Json(result);
//    }

//    // ══════════════════════════════════════════════════════════════════════
//    // AUDIT LOGS API
//    // ══════════════════════════════════════════════════════════════════════

//    [HttpGet]
//    [Route("Dashboard/Audit/GetAll")]
//    public async Task<IActionResult> AuditLogsGetAll()
//    {
//        var logs = await GlobalFunctions.GetAllAuditLogsAsync(_db);
//        return Json(logs);
//    }
//}
