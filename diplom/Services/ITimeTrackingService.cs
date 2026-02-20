using diplom.Models;
using System;
using System.Threading.Tasks;

namespace diplom.Services
{
    public interface ITimeTrackingService
    {
        int? ActiveTaskId { get; }
        DateTime? ActiveStartTimeLocal { get; }
        bool HasActiveSession { get; }

        void Start(int taskId, DateTime? startTimeLocal = null);
        Task<TimeEntry?> StopActiveAsync(string comment = "Timer session");
        Task<TimeEntry> AddManualEntryAsync(int taskId, DateTime dateLocal, TimeSpan duration, string comment);
        string FormatTimeSpan(TimeSpan duration);
    }
}
