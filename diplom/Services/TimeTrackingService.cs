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

        public int? ActiveTaskId { get; private set; }
        public DateTime? ActiveStartTimeLocal { get; private set; }
        public bool HasActiveSession => ActiveTaskId.HasValue && ActiveStartTimeLocal.HasValue;

        public TimeTrackingService(AppDataService dataService)
        {
            _dataService = dataService;
        }

        public void Start(int taskId, DateTime? startTimeLocal = null)
        {
            if (HasActiveSession && ActiveTaskId != taskId)
                throw new InvalidOperationException("Another task timer is already running.");

            if (HasActiveSession && ActiveTaskId == taskId)
                return;

            ActiveTaskId = taskId;
            ActiveStartTimeLocal = startTimeLocal ?? DateTime.Now;
        }

        public async Task<TimeEntry?> StopActiveAsync(string comment = "Timer session")
        {
            if (!HasActiveSession)
                return null;

            var entry = new TimeEntry
            {
                TaskId = ActiveTaskId!.Value,
                StartTime = ActiveStartTimeLocal!.Value.ToUniversalTime(),
                EndTime = DateTime.UtcNow,
                IsManual = false,
                Comment = comment ?? string.Empty
            };

            var created = await _dataService.CreateTimeEntryAsync(entry);
            ActiveTaskId = null;
            ActiveStartTimeLocal = null;
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
