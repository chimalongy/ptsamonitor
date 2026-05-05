using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using ptsamonitor.Models;
using StackExchange.Redis;
using ptsamonitor.Classes.Utils;

namespace ptsamonitor.Services
{
    // Services/DashboardCacheService.cs
    public class DashboardCacheService
    {
        private readonly IDatabase _redis;
        private readonly string _oracleConnStr;

        // Key used to store all rows in Redis
        private const string CacheKey = "dashboard:all_transactions";
        private const string LastRefresh = "dashboard:last_refreshed";

        // How long Redis keeps the data before expiring it
        private readonly TimeSpan _expiry = TimeSpan.FromMinutes(10);

        public DashboardCacheService(IConnectionMultiplexer redis,IConfiguration config)
        {
            _redis = redis.GetDatabase();
            _oracleConnStr = Cryptor.Decrypt(config.GetConnectionString("PTSAConnection"), true);
            //_oracleConnStr = config.GetConnectionString("PTSAConnection");
        }

        // ── Step A: Read all rows from the MV ────────────────────────────────────
        private async Task<List<DashboardTransaction>> FetchFromDatabaseAsync()
        {
            const string sql = @"
            SELECT
                ID, TRANSID, TRANNUMBER, TIME,
                SOURCE, SOURCE1, DESTINATION,
                RESPCODE, RESPCODE_DESCRIPTION,
                TERMNAME, TERMRETAILERNAME,
                INST, PAN, MASKEDPAN, AUTHFINAME,
                AMOUNT, INSTITUTION_CODE
            FROM PTSA.MV_DASHBOARD_CACHE";

            var rows = new List<DashboardTransaction>();

            await using var conn = new OracleConnection(_oracleConnStr);
            await conn.OpenAsync();

            await using var cmd = new OracleCommand(sql, conn);
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                rows.Add(new DashboardTransaction
                {
                    Id = rdr.GetInt64(0),
                    TransId = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                    TranNumber = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                    Time = rdr.GetDateTime(3),
                    Source = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    Source1 = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    Destination = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                    RespCode = rdr.IsDBNull(7) ? 0 : rdr.GetInt32(7),
                    RespCodeDescription = rdr.IsDBNull(8) ? null : rdr.GetString(8),
                    TermName = rdr.IsDBNull(9) ? null : rdr.GetString(9),
                    TermRetailerName = rdr.IsDBNull(10) ? null : rdr.GetString(10),
                    Inst = rdr.IsDBNull(11) ? null : rdr.GetString(11),
                    Pan = rdr.IsDBNull(12) ? null : rdr.GetString(12),
                    MaskedPan = rdr.IsDBNull(13) ? null : rdr.GetString(13),
                    AuthFiName = rdr.IsDBNull(14) ? null : rdr.GetString(14),
                    Amount = rdr.IsDBNull(15) ? 0 : rdr.GetDecimal(15),
                    InstitutionCode = rdr.IsDBNull(16) ? null : rdr.GetString(16),
                });
            }

            return rows;
        }

        // ── Step B: Serialize and push into Redis ─────────────────────────────────
        public async Task LoadIntoRedisAsync()
        {
            var rows = await FetchFromDatabaseAsync();
            var json = JsonConvert.SerializeObject(rows);

            // Calculate time remaining until midnight
            // Data is only valid for today — expire it then
            var midnight = DateTime.Today.AddDays(1);
            var timeUntilMidnight = midnight - DateTime.Now;

            // Store until midnight — worker keeps it fresh every 5 min
            await _redis.StringSetAsync(CacheKey, json, timeUntilMidnight);

            await _redis.StringSetAsync(
                LastRefresh,
                DateTime.UtcNow.ToString("o"),
                timeUntilMidnight);

            Console.WriteLine($"Cached {rows.Count} rows. Expires at midnight ({timeUntilMidnight.Hours}h {timeUntilMidnight.Minutes}m remaining)");
        }

        // ── Step C: Read back from Redis ──────────────────────────────────────────
        public async Task<List<DashboardTransaction>> GetFromRedisAsync()
        {
            var json = await _redis.StringGetAsync(CacheKey);

            // If cache is empty or expired — reload from DB
            if (json.IsNullOrEmpty)
            {
                Console.WriteLine("Cache miss — reloading from database...");
                await LoadIntoRedisAsync();
                json = await _redis.StringGetAsync(CacheKey);
            }

            return JsonConvert.DeserializeObject<List<DashboardTransaction>>(json);
        }

        // ── Step D: Check last refresh time ───────────────────────────────────────
        public async Task<DateTime?> GetLastRefreshedAsync()
        {
            var val = await _redis.StringGetAsync(LastRefresh);
            return val.IsNullOrEmpty ? null : DateTime.Parse(val.ToString());
        }
    }
}
