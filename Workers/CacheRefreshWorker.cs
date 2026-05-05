using ptsamonitor.Services;

namespace ptsamonitor.Workers
{
    // Workers/CacheRefreshWorker.cs
    public class CacheRefreshWorker : BackgroundService
    {
        private readonly DashboardCacheService _cache;
        private readonly ILogger<CacheRefreshWorker> _logger;

        public CacheRefreshWorker(
            DashboardCacheService cache,
            ILogger<CacheRefreshWorker> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Load immediately when app starts
            await SafeLoadAsync();

            while (!ct.IsCancellationRequested)
            {
                // Wait 5 minutes then reload
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
                await SafeLoadAsync();
            }
        }

        private async Task SafeLoadAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing Redis cache...");
                await _cache.LoadIntoRedisAsync();
                _logger.LogInformation("Redis cache refreshed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                // Don't crash — keep serving whatever is already in Redis
                _logger.LogError(ex, "Cache refresh failed.");
            }
        }
    }
}
