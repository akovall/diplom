using diplom.Models.enums;

namespace diplom.Models.Analytics
{
    public static class ProductivityCalculator
    {
        public sealed class Settings
        {
            // Bayesian smoothing: (done + PriorDone) / (total + PriorTotal)
            public double PriorTotalTasks { get; init; } = 5.0;
            public double PriorDoneTasks { get; init; } = 3.0; // 60% default baseline

            // Final score = SmoothedCompletion * EfficiencyMultiplier * 100
            // EfficiencyMultiplier in [0.8..1.2] (when efficiency 0..1)
            public double EfficiencyMultiplierMin { get; init; } = 0.8;
            public double EfficiencyMultiplierMax { get; init; } = 1.2;

            // Clamp final score.
            public double MinScorePercent { get; init; } = 0.0;
            public double MaxScorePercent { get; init; } = 100.0;
        }

        public static ProductivityResult CalculateForWeek(
            IEnumerable<TaskItem> tasks,
            DateTime weekStartUtc,
            DateTime weekEndUtc,
            Settings? settings = null)
        {
            settings ??= new Settings();

            // "Assigned this week" approximated by CreatedAt in [weekStartUtc, weekEndUtc).
            // This keeps the metric week-specific even without an explicit AssignedAt/CompletedAt.
            var tasksThisWeek = tasks
                .Where(t => t.CreatedAt >= weekStartUtc && t.CreatedAt < weekEndUtc)
                .ToList();

            var total = tasksThisWeek.Count;
            var done = tasksThisWeek.Count(t => t.Status == AppTaskStatus.Done);

            var completion = total == 0 ? 0.0 : (double)done / total;

            var priorTotal = Math.Max(0.0, settings.PriorTotalTasks);
            var priorDone = Math.Clamp(settings.PriorDoneTasks, 0.0, priorTotal);
            var smoothed = (done + priorDone) / (total + priorTotal);

            // Efficiency uses time entries that ended within the week window.
            // Per-task efficiency = clamp(EstimatedHours / ActualHours, 0..1), averaged across tasks that have both.
            var perTaskEfficiencies = new List<double>();
            foreach (var task in tasksThisWeek)
            {
                if (task.EstimatedHours <= 0)
                    continue;
                if (task.TimeEntries == null || task.TimeEntries.Count == 0)
                    continue;

                var actualTicks = task.TimeEntries
                    .Where(e => e.EndTime.HasValue)
                    .Where(e => e.EndTime!.Value >= weekStartUtc && e.EndTime!.Value < weekEndUtc)
                    .Sum(e => e.Duration.Ticks);

                var actualHours = TimeSpan.FromTicks(actualTicks).TotalHours;
                if (actualHours <= 0)
                    continue;

                var eff = task.EstimatedHours / actualHours;
                eff = Math.Clamp(eff, 0.0, 1.0);
                perTaskEfficiencies.Add(eff);
            }

            var efficiency = perTaskEfficiencies.Count == 0 ? 0.5 : perTaskEfficiencies.Average();
            efficiency = Math.Clamp(efficiency, 0.0, 1.0);

            var effMult = settings.EfficiencyMultiplierMin +
                          (settings.EfficiencyMultiplierMax - settings.EfficiencyMultiplierMin) * efficiency;

            var score = smoothed * effMult * 100.0;
            score = Math.Clamp(score, settings.MinScorePercent, settings.MaxScorePercent);
            score = Math.Round(score, 1);

            return new ProductivityResult
            {
                ScorePercent = score,
                TotalTasks = total,
                DoneTasks = done,
                CompletionRate = completion,
                SmoothedCompletionRate = smoothed,
                EfficiencyRate = efficiency
            };
        }
    }
}

