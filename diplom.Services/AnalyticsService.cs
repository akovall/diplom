using diplom.Models.Analytics;
using System.Threading.Tasks;

namespace diplom.Services
{
    public sealed class AnalyticsService
    {
        private readonly ApiClient _api;

        public AnalyticsService(ApiClient api)
        {
            _api = api;
        }

        public Task<UserAnalyticsDto?> GetUserAnalyticsAsync(int userId, int days)
        {
            days = days < 7 ? 7 : days > 90 ? 90 : days;
            return _api.GetAsync<UserAnalyticsDto>($"/api/users/{userId}/analytics?days={days}");
        }
    }
}

