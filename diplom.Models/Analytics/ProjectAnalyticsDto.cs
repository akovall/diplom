using System;
using System.Collections.Generic;

namespace diplom.Models.Analytics
{
    public sealed class ProjectAnalyticsDto
    {
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int OverdueCompleted { get; set; }
        public double WorkedHours { get; set; }

        public List<ProjectAnalyticsDayDto> Days { get; set; } = new();
        public List<ProjectAnalyticsTaskDto> RecentCompletedTasks { get; set; } = new();
    }

    public sealed class ProjectAnalyticsDayDto
    {
        public DateTime DayUtc { get; set; }
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int OverdueCompleted { get; set; }
        public double WorkedHours { get; set; }
    }

    public sealed class ProjectAnalyticsTaskDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? DeadlineUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public bool WasOverdue { get; set; }
        public double EstimatedHours { get; set; }
        public double ActualHours { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
    }
}
