namespace diplom.Models.Analytics
{
    public sealed class UserAnalyticsDto
    {
        public int UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string JobTitle { get; init; } = string.Empty;

        public DateTime FromUtc { get; init; }
        public DateTime ToUtc { get; init; }

        public int TasksAssigned { get; init; }
        public int TasksCompleted { get; init; }
        public int TasksCompletedFromAssignedInPeriod { get; init; }
        public int OverdueCompleted { get; init; }

        public double WorkedHours { get; init; }

        public List<UserAnalyticsDayDto> Days { get; init; } = new();
        public List<UserAnalyticsTaskDto> RecentCompletedTasks { get; init; } = new();
    }

    public sealed class UserAnalyticsDayDto
    {
        public DateTime DayUtc { get; set; }
        public double WorkedHours { get; set; }
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int OverdueCompleted { get; set; }
    }

    public sealed class UserAnalyticsTaskDto
    {
        public int TaskId { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime? DeadlineUtc { get; init; }
        public DateTime CompletedAtUtc { get; init; }
        public bool WasOverdue { get; init; }
        public double EstimatedHours { get; init; }
        public double ActualHours { get; init; }
    }
}
