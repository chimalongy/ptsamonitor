using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ptsamonitor.Classes.Utils;
using ptsamonitor.Data;
using ptsamonitor.Models.ViewModels;
using System.Security.Claims;

namespace ptsamonitor.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public DashboardController(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    // ══════════════════════════════════════════════════════════════════════
    // VIEWS
    // ══════════════════════════════════════════════════════════════════════

    public IActionResult Index()
    {
        return View("~/Views/Dashboard/Index.cshtml");
    }

    [Route("Dashboard/Transactions")]
    public IActionResult Transactions()
    {
        return View("~/Views/Dashboard/Transactions.cshtml");
    }

    [Route("Dashboard/Institutions")]
    public IActionResult Institutions()
    {
        return View("~/Views/Dashboard/Institutions.cshtml");
    }

    [Route("Dashboard/Users")]
    public IActionResult Users()
    {
        return View("~/Views/Dashboard/Users.cshtml");
    }

    [Route("Dashboard/Audit")]
    public IActionResult Audit()
    {
        return View("~/Views/Dashboard/Audit.cshtml");
    }

    // ══════════════════════════════════════════════════════════════════════
    // DASHBOARD DATA API — reads directly from MV_DASHBOARD_CACHE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the current user's institution info needed by the dashboard.
    /// </summary>
    private async Task<(string InstitutionName, string InstitutionType, string? InstitutionCode, string? InstitutionSubCodes)> GetUserInstitutionAsync()
    {
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var user = await _db.PtsaUsers.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user?.Institution == null)
            return ("", "", null, null);

        var institution = await _db.Institutions
            .FirstOrDefaultAsync(i => i.InstitutionName == user.Institution);

        return (
            user.Institution,
            institution?.InstitutionType ?? "",
            institution?.InstitutionCode,
            institution?.InstitutionSubCodes
        );
    }

    private string GetOracleConnectionString()
    {
        var encrypted = _config.GetConnectionString("PTSAConnection")
            ?? throw new Exception("PTSAConnection not found in config.");
        return Cryptor.Decrypt(encrypted, true);
    }

    [HttpGet]
    [Route("Dashboard/Api/PieChart")]
    public async Task<IActionResult> PieChart()
    {
        var (_, instType, instCode, subCodes) = await GetUserInstitutionAsync();
        var connStr = GetOracleConnectionString();
        var rows = new List<object>();

        const string unifiedSql = @"
            SELECT COUNT(1) AS TXN_COUNT, SUM(AMOUNT) AS TOTAL_AMOUNT,
                   RESPCODE, RESPCODE_DESCRIPTION
            FROM PTSA.MV_DASHBOARD_CACHE
            GROUP BY RESPCODE, RESPCODE_DESCRIPTION
            ORDER BY TXN_COUNT DESC";

        const string bankSql = @"
            SELECT COUNT(1) AS TXN_COUNT, SUM(AMOUNT) AS TOTAL_AMOUNT,
                   RESPCODE, RESPCODE_DESCRIPTION
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE INSTITUTION_CODE = UPPER(:inst_code)
            GROUP BY RESPCODE, RESPCODE_DESCRIPTION
            ORDER BY TXN_COUNT DESC";

        const string nonBankSql = @"
            SELECT COUNT(1) AS TXN_COUNT, SUM(AMOUNT) AS TOTAL_AMOUNT,
                   RESPCODE, RESPCODE_DESCRIPTION
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE SOURCE1 IN ({0})
            GROUP BY RESPCODE, RESPCODE_DESCRIPTION
            ORDER BY TXN_COUNT DESC";

        string sql;
        OracleParameter[]? parameters = null;

        if (instType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
        {
            sql = bankSql;
            parameters = new[] { new OracleParameter("inst_code", instCode ?? "") };
        }
        else if (instType.Equals("Non Bank", StringComparison.OrdinalIgnoreCase) && !string.Equals(instCode, "UP001", StringComparison.OrdinalIgnoreCase))
        {
            // Non-bank (not Unified Payments)
            var codes = (subCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (codes.Length == 0) codes = new[] { "WEB", "MOBILE", "USSD", "AGENT", "POS", "ATM" };
            var placeholders = string.Join(", ", codes.Select((_, i) => $":p{i}"));
            sql = string.Format(nonBankSql, placeholders);
            parameters = codes.Select((c, i) => new OracleParameter($"p{i}", c)).ToArray();
        }
        else
        {
            // Unified Payments (or fallback)
            sql = unifiedSql;
        }

        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        if (parameters != null) cmd.Parameters.AddRange(parameters);
        await using var rdr = await cmd.ExecuteReaderAsync();

        while (await rdr.ReadAsync())
        {
            rows.Add(new
            {
                TxnCount = rdr.IsDBNull(0) ? 0 : rdr.GetInt64(0),
                TotalAmount = rdr.IsDBNull(1) ? 0m : rdr.GetDecimal(1),
                RespCode = rdr.IsDBNull(2) ? 0 : rdr.GetInt32(2),
                RespCodeDescription = rdr.IsDBNull(3) ? null : rdr.GetString(3)
            });
        }

        return Json(rows);
    }

    [HttpGet]
    [Route("Dashboard/Api/DonutChart")]
    public async Task<IActionResult> DonutChart()
    {
        var (_, instType, instCode, subCodes) = await GetUserInstitutionAsync();
        var connStr = GetOracleConnectionString();
        var rows = new List<object>();

        const string unifiedSql = @"
            SELECT RESPCODE_DESCRIPTION, COUNT(DISTINCT ID) AS TXN_COUNT
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE RESPCODE NOT IN (0, 00)
            GROUP BY RESPCODE_DESCRIPTION
            ORDER BY RESPCODE_DESCRIPTION";

        const string bankSql = @"
            SELECT RESPCODE_DESCRIPTION, COUNT(DISTINCT ID) AS TXN_COUNT
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE RESPCODE NOT IN (0, 00)
              AND INSTITUTION_CODE = UPPER(:inst_code)
            GROUP BY RESPCODE_DESCRIPTION
            ORDER BY RESPCODE_DESCRIPTION";

        const string nonBankSql = @"
            SELECT RESPCODE_DESCRIPTION, COUNT(DISTINCT ID) AS TXN_COUNT
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE RESPCODE NOT IN (0, 00)
              AND SOURCE1 IN ({0})
            GROUP BY RESPCODE_DESCRIPTION
            ORDER BY RESPCODE_DESCRIPTION";

        string sql;
        OracleParameter[]? parameters = null;

        if (instType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
        {
            sql = bankSql;
            parameters = new[] { new OracleParameter("inst_code", instCode ?? "") };
        }
        else if (instType.Equals("Non Bank", StringComparison.OrdinalIgnoreCase) && !string.Equals(instCode, "UP001", StringComparison.OrdinalIgnoreCase))
        {
            var codes = (subCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (codes.Length == 0) codes = new[] { "WEB", "MOBILE", "USSD", "AGENT", "POS", "ATM" };
            var placeholders = string.Join(", ", codes.Select((_, i) => $":p{i}"));
            sql = string.Format(nonBankSql, placeholders);
            parameters = codes.Select((c, i) => new OracleParameter($"p{i}", c)).ToArray();
        }
        else
        {
            sql = unifiedSql;
        }

        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        if (parameters != null) cmd.Parameters.AddRange(parameters);
        await using var rdr = await cmd.ExecuteReaderAsync();

        while (await rdr.ReadAsync())
        {
            rows.Add(new
            {
                RespCodeDescription = rdr.IsDBNull(0) ? null : rdr.GetString(0),
                TxnCount = rdr.IsDBNull(1) ? 0 : rdr.GetInt64(1)
            });
        }

        return Json(rows);
    }

    [HttpGet]
    [Route("Dashboard/Api/BarChart")]
    public async Task<IActionResult> BarChart()
    {
        var (_, instType, instCode, subCodes) = await GetUserInstitutionAsync();
        var connStr = GetOracleConnectionString();
        var rows = new List<object>();

        const string unifiedSql = @"
            SELECT COUNT(1) AS TXN_COUNT, INST
            FROM PTSA.MV_DASHBOARD_CACHE
            GROUP BY INST
            ORDER BY TXN_COUNT DESC";

        const string bankSql = @"
            SELECT COUNT(1) AS TXN_COUNT, INST
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE INSTITUTION_CODE = UPPER(:inst_code)
            GROUP BY INST
            ORDER BY TXN_COUNT DESC";

        const string nonBankSql = @"
            SELECT COUNT(1) AS TXN_COUNT, INST
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE SOURCE1 IN ({0})
            GROUP BY INST
            ORDER BY TXN_COUNT DESC";

        string sql;
        OracleParameter[]? parameters = null;

        if (instType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
        {
            sql = bankSql;
            parameters = new[] { new OracleParameter("inst_code", instCode ?? "") };
        }
        else if (instType.Equals("Non Bank", StringComparison.OrdinalIgnoreCase) && !string.Equals(instCode, "UP001", StringComparison.OrdinalIgnoreCase))
        {
            var codes = (subCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (codes.Length == 0) codes = new[] { "WEB", "MOBILE", "USSD", "AGENT", "POS", "ATM" };
            var placeholders = string.Join(", ", codes.Select((_, i) => $":p{i}"));
            sql = string.Format(nonBankSql, placeholders);
            parameters = codes.Select((c, i) => new OracleParameter($"p{i}", c)).ToArray();
        }
        else
        {
            sql = unifiedSql;
        }

        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        if (parameters != null) cmd.Parameters.AddRange(parameters);
        await using var rdr = await cmd.ExecuteReaderAsync();

        while (await rdr.ReadAsync())
        {
            rows.Add(new
            {
                TxnCount = rdr.IsDBNull(0) ? 0 : rdr.GetInt64(0),
                Inst = rdr.IsDBNull(1) ? null : rdr.GetString(1)
            });
        }

        return Json(rows);
    }

    [HttpGet]
    [Route("Dashboard/Api/Transactions")]
    public async Task<IActionResult> TransactionsData(int offset = 0, int limit = 100)
    {
        var (_, instType, instCode, subCodes) = await GetUserInstitutionAsync();
        var connStr = GetOracleConnectionString();
        var rows = new List<object>();
        long total = 0;

        const string unifiedDataSql = @"
            SELECT SOURCE, TRANNUMBER AS RRN, MASKEDPAN AS PAN, TERMNAME AS TERMINALID,
                   RESPCODE, RESPCODE_DESCRIPTION, TIME, TERMRETAILERNAME AS MERCHANTID, DESTINATION
            FROM PTSA.MV_DASHBOARD_CACHE
            ORDER BY TIME DESC
            OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";

        const string unifiedCountSql = @"SELECT COUNT(*) FROM PTSA.MV_DASHBOARD_CACHE";

        const string bankDataSql = @"
            SELECT SOURCE, TRANNUMBER AS RRN, MASKEDPAN AS PAN, TERMNAME AS TERMINALID,
                   RESPCODE, RESPCODE_DESCRIPTION, TIME, TERMRETAILERNAME AS MERCHANTID, DESTINATION
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE INSTITUTION_CODE = UPPER(:inst_code)
            ORDER BY TIME DESC
            OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";

        const string bankCountSql = @"
            SELECT COUNT(*) FROM PTSA.MV_DASHBOARD_CACHE WHERE INSTITUTION_CODE = UPPER(:inst_code)";

        const string nonBankDataSql = @"
            SELECT SOURCE, TRANNUMBER AS RRN, MASKEDPAN AS PAN, TERMNAME AS TERMINALID,
                   RESPCODE, RESPCODE_DESCRIPTION, TIME, TERMRETAILERNAME AS MERCHANTID, DESTINATION
            FROM PTSA.MV_DASHBOARD_CACHE
            WHERE SOURCE1 IN ({0})
            ORDER BY TIME DESC
            OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";

        const string nonBankCountSql = @"
            SELECT COUNT(*) FROM PTSA.MV_DASHBOARD_CACHE WHERE SOURCE1 IN ({0})";

        string dataSql, countSql;
        OracleParameter[]? parameters = null;

        if (instType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
        {
            dataSql = bankDataSql;
            countSql = bankCountSql;
            parameters = new[]
            {
                new OracleParameter("inst_code", instCode ?? ""),
                new OracleParameter("offset", offset),
                new OracleParameter("limit", limit)
            };
        }
        else if (instType.Equals("Non Bank", StringComparison.OrdinalIgnoreCase) && !string.Equals(instCode, "UP001", StringComparison.OrdinalIgnoreCase))
        {
            var codes = (subCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (codes.Length == 0) codes = new[] { "WEB", "MOBILE", "USSD", "AGENT", "POS", "ATM" };
            var placeholders = string.Join(", ", codes.Select((_, i) => $":p{i}"));
            dataSql = string.Format(nonBankDataSql, placeholders);
            countSql = string.Format(nonBankCountSql, placeholders);
            var codeParams = codes.Select((c, i) => new OracleParameter($"p{i}", c)).ToList();
            codeParams.Add(new OracleParameter("offset", offset));
            codeParams.Add(new OracleParameter("limit", limit));
            parameters = codeParams.ToArray();
        }
        else
        {
            dataSql = unifiedDataSql;
            countSql = unifiedCountSql;
            parameters = new[]
            {
                new OracleParameter("offset", offset),
                new OracleParameter("limit", limit)
            };
        }

        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        // Count
        await using (var countCmd = new OracleCommand(countSql, conn))
        {
            if (instType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
                countCmd.Parameters.Add(new OracleParameter("inst_code", instCode ?? ""));
            else if (instType.Equals("Non Bank", StringComparison.OrdinalIgnoreCase) && !string.Equals(instCode, "UP001", StringComparison.OrdinalIgnoreCase))
            {
                var codes = (subCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (codes.Length == 0) codes = new[] { "WEB", "MOBILE", "USSD", "AGENT", "POS", "ATM" };
                for (int i = 0; i < codes.Length; i++)
                    countCmd.Parameters.Add(new OracleParameter($"p{i}", codes[i]));
            }
            var countResult = await countCmd.ExecuteScalarAsync();
            total = countResult == null || countResult == DBNull.Value ? 0 : Convert.ToInt64(countResult);
        }

        // Data
        await using (var cmd = new OracleCommand(dataSql, conn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                rows.Add(new
                {
                    Source = rdr.IsDBNull(0) ? null : rdr.GetString(0),
                    Rrn = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                    Pan = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                    TerminalId = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    RespCode = rdr.IsDBNull(4) ? 0 : rdr.GetInt32(4),
                    RespCodeDescription = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    Time = rdr.IsDBNull(6) ? (DateTime?)null : rdr.GetDateTime(6),
                    MerchantId = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                    Destination = rdr.IsDBNull(8) ? null : rdr.GetString(8)
                });
            }
        }

        return Json(new { data = rows, total, offset, limit });
    }

    // ══════════════════════════════════════════════════════════════════════
    // INSTITUTIONS API
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    [Route("Dashboard/Institutions/GetAll")]
    public async Task<IActionResult> InstitutionsGetAll()
    {
        var institutions = await GlobalFunctions.GetAllInstitutionsAsync(_db);
        return Json(institutions);
    }

    [HttpPost]
    [Route("Dashboard/Institutions/Create")]
    public async Task<IActionResult> InstitutionsCreate([FromForm] CreateInstitutionRequest req, IFormFile? logoFile)
    {
        string? logoPath = null;

        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(uploadsDir);

            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(logoFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            logoPath = $"/uploads/logos/{safeName}";
        }

        var (result, error) = await GlobalFunctions.CreateInstitutionAsync(_db, req, logoPath);

        if (error == "CONFLICT")
            return Conflict(new { message = "An institution with this name already exists." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - CREATED INSTITUTION - {req.InstitutionName}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Institutions/Update/{id:int}")]
    public async Task<IActionResult> InstitutionsUpdate(int id, [FromForm] CreateInstitutionRequest req, IFormFile? logoFile)
    {
        string? logoPath = null;

        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(uploadsDir);

            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(logoFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            logoPath = $"/uploads/logos/{safeName}";
        }

        var (result, error) = await GlobalFunctions.UpdateInstitutionAsync(_db, id, req, logoPath);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "Institution not found." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - UPDATED INSTITUTION ID: {id}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Institutions/Delete/{id:int}")]
    public async Task<IActionResult> InstitutionsDelete(int id)
    {
        var error = await GlobalFunctions.DeleteInstitutionAsync(_db, id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "Institution not found." });

        if (error == "HAS_USERS")
            return BadRequest(new { message = "Cannot delete institution with assigned users." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - DELETED INSTITUTION ID: {id}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(new { message = "Institution deleted." });
    }

    // ══════════════════════════════════════════════════════════════════════
    // USERS API
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    [Route("Dashboard/Users/GetAll")]
    public async Task<IActionResult> UsersGetAll()
    {
        var users = await GlobalFunctions.GetAllUsersAsync(_db);
        return Json(users);
    }

    [HttpPost]
    [Route("Dashboard/Users/Create")]
    public async Task<IActionResult> UsersCreate([FromBody] CreateUserRequest req)
    {
        var (result, error) = await GlobalFunctions.CreateUserAsync(
            _db, _config,
            req.UserName, req.Email, req.Institution, req.UserType, req.Privileges);

        if (error == "CONFLICT")
            return Conflict(new { message = "A user with this username already exists." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - CREATED USER - {req.UserName}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Users/Update/{id:int}")]
    public async Task<IActionResult> UsersUpdate(int id, [FromBody] CreateUserRequest req)
    {
        var (result, error) = await GlobalFunctions.UpdateUserAsync(_db, id, req);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - UPDATED USER ID: {id}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Users/Delete/{id:int}")]
    public async Task<IActionResult> UsersDelete(int id)
    {
        var error = await GlobalFunctions.DeleteUserAsync(_db, id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - DELETED USER ID: {id}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(new { message = "User deleted." });
    }

    [HttpPost]
    [Route("Dashboard/Users/UpdateStatus/{id:int}")]
    public async Task<IActionResult> UsersUpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var (result, error) = await GlobalFunctions.UpdateUserStatusAsync(_db, id, req.Status);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{userName} - UPDATED USER ID: {id} STATUS TO {req.Status}",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return Json(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUDIT LOGS API
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    [Route("Dashboard/Audit/GetAll")]
    public async Task<IActionResult> AuditLogsGetAll()
    {
        var logs = await GlobalFunctions.GetAllAuditLogsAsync(_db);
        return Json(logs);
    }
}