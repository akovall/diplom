using diplom.Models;
using System;
using System.Threading.Tasks;

namespace diplom.Services
{
    public sealed class TimeTrackingService : ITimeTrackingService
    {
        private static TimeTrackingService? _instance;
        private static readonly object _lock = new();

        public static TimeTrackingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new TimeTrackingService(AppDataService.Instance);
                    }
                }

                return _instance;
            }
        }

        private readonly AppDataService _dataService;
        private readonly ApiClient _api;
        private int? _activeTimeEntryId;

        public int? ActiveTaskId { get; private set; }
        public DateTime? ActiveStartTimeLocal { get; private set; }
        public bool HasActiveSession => ActiveTaskId.HasValue && ActiveStartTimeLocal.HasValue;

        public TimeTrackingService(AppDataService dataService)
        {
            _dataService = dataService;
            _api = ApiClient.Instance;
        }

        public void Start(int taskId, DateTime? startTimeLocal = null)
        {
            if (HasActiveSession && ActiveTaskId != taskId)
                throw new InvalidOperationException("Another task timer is already running.");

            if (HasActiveSession && ActiveTaskId == taskId)
                return;

            ActiveTaskId = taskId;
            ActiveStartTimeLocal = startTimeLocal ?? DateTime.Now;

            _ = TryCreateOpenTimeEntryAsync(taskId);
        }

        private async Task TryCreateOpenTimeEntryAsync(int taskId)
        {
            try
            {
                var entry = new TimeEntry
                {
                    TaskId = taskId,
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    IsManual = false,
                    Comment = "Timer session"
                };

                var created = await _api.PostAsync<TimeEntry>("/api/timeentries", entry);
                if (created != null)
                    _activeTimeEntryId = created.Id;
            }
            catch
            {
                // best-effort; presence "active" relies on open entries, but timer can still run locally
            }
        }

        public async Task<TimeEntry?> StopActiveAsync(string comment = "Timer session")
        {
            if (!HasActiveSession)
                return null;

            TimeEntry? created;
            if (_activeTimeEntryId.HasValue)
            {
                created = await _api.PutAsync<TimeEntry>($"/api/timeentries/{_activeTimeEntryId.Value}", new
                {
                    EndTime = DateTime.UtcNow,
                    Comment = comment ?? string.Empty,
                    IsManual = false
                });
            }
            else
            {
                var entry = new TimeEntry
                {
                    TaskId = ActiveTaskId!.Value,
                    StartTime = ActiveStartTimeLocal!.Value.ToUniversalTime(),
                    EndTime = DateTime.UtcNow,
                    IsManual = false,
                    Comment = comment ?? string.Empty
                };

                created = await _dataService.CreateTimeEntryAsync(entry);
            }

            ActiveTaskId = null;
            ActiveStartTimeLocal = null;
            _activeTimeEntryId = null;
            return created;
        }

        public Task<TimeEntry> AddManualEntryAsync(int taskId, DateTime dateLocal, TimeSpan duration, string comment)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

            var localDayStart = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Local);

            var entry = new TimeEntry
            {
                TaskId = taskId,
                StartTime = localDayStart.ToUniversalTime(),
                EndTime = localDayStart.Add(duration).ToUniversalTime(),
                IsManual = true,
                Comment = comment ?? string.Empty
            };

            return _dataService.CreateTimeEntryAsync(entry);
        }

        public string FormatTimeSpan(TimeSpan duration)
        {
            var totalHours = (int)duration.TotalHours;
            return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }
    }
}
