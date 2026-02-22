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

            // Overdue penalty: score is multiplied by (1 - min(MaxPenalty, OverdueCount * PerTaskPenalty))
            public double OverduePerTaskPenalty { get; init; } = 0.05;
            public double OverdueMaxPenalty { get; init; } = 0.30;

            // Planning quality: hints appear when many tasks have no estimate or estimates are far off.
            public double MissingEstimateThreshold { get; init; } = 0.30; // 30%+
            public double DeviationThreshold { get; init; } = 0.60; // 60%+ avg relative deviation
        }

        public static ProductivityResult CalculateForWeek(
            IEnumerable<TaskItem> tasks,
            DateTime weekStartUtc,
            DateTime weekEndUtc,
            Settings? settings = null)
        {
            settings ??= new Settings();

            // Week scope:
            // - TotalTasks: tasks assigned in [weekStartUtc, weekEndUtc)
            // - DoneTasks: among those, tasks completed in [weekStartUtc, weekEndUtc)
            var tasksAssignedThisWeek = tasks
                .Where(t => t.AssignedAtUtc.HasValue)
                .Where(t => t.AssignedAtUtc!.Value >= weekStartUtc && t.AssignedAtUtc!.Value < weekEndUtc)
                .ToList();

            var total = tasksAssignedThisWeek.Count;
            var done = tasksAssignedThisWeek.Count(t =>
                t.CompletedAtUtc.HasValue &&
                t.CompletedAtUtc.Value >= weekStartUtc &&
                t.CompletedAtUtc.Value < weekEndUtc);

            var completion = total == 0 ? 0.0 : (double)done / total;

            var priorTotal = Math.Max(0.0, settings.PriorTotalTasks);
            var priorDone = Math.Clamp(settings.PriorDoneTasks, 0.0, priorTotal);
            var smoothed = (done + priorDone) / (total + priorTotal);

            // Efficiency uses time entries that ended within the week window.
            // Per-task efficiency = clamp(EstimatedHours / ActualHours, 0..1), averaged across tasks that have both.
            var perTaskEfficiencies = new List<double>();
            var perTaskDeviation = new List<double>();
            var missingEstimateCount = 0;

            foreach (var task in tasksAssignedThisWeek)
            {
                if (task.EstimatedHours <= 0)
                    missingEstimateCount++;
                if (task.TimeEntries == null || task.TimeEntries.Count == 0)
                    continue;

                var actualTicks = task.TimeEntries
                    .Where(e => e.EndTime.HasValue)
                    .Where(e => e.EndTime!.Value >= weekStartUtc && e.EndTime!.Value < weekEndUtc)
                    .Sum(e => e.Duration.Ticks);

                var actualHours = TimeSpan.FromTicks(actualTicks).TotalHours;
                if (actualHours <= 0)
                    continue;

                if (task.EstimatedHours > 0)
                {
                    // 0 means perfect planning, 1 means 100% off.
                    var deviation = Math.Abs(actualHours - task.EstimatedHours) / task.EstimatedHours;
                    perTaskDeviation.Add(deviation);
                }

                if (task.EstimatedHours <= 0)
                    continue;

                var eff = task.EstimatedHours / actualHours;
                eff = Math.Clamp(eff, 0.0, 1.0);
                perTaskEfficiencies.Add(eff);
            }

            var efficiency = perTaskEfficiencies.Count == 0 ? 0.5 : perTaskEfficiencies.Average();
            efficiency = Math.Clamp(efficiency, 0.0, 1.0);

            var effMult = settings.EfficiencyMultiplierMin +
                          (settings.EfficiencyMultiplierMax - settings.EfficiencyMultiplierMin) * efficiency;

            // Overdue: count tasks assigned this week that are overdue by weekEndUtc.
            // Penalize if not done by week end OR completed after deadline.
            var overdueCount = tasksAssignedThisWeek.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < weekEndUtc &&
                (
                    !t.CompletedAtUtc.HasValue ||
                    t.CompletedAtUtc.Value > t.Deadline.Value
                ));

            var overduePenalty = Math.Min(settings.OverdueMaxPenalty, overdueCount * settings.OverduePerTaskPenalty);
            overduePenalty = Math.Clamp(overduePenalty, 0.0, 1.0);

            var score = smoothed * effMult * (1.0 - overduePenalty) * 100.0;
            score = Math.Clamp(score, settings.MinScorePercent, settings.MaxScorePercent);
            score = Math.Round(score, 1);

            // Planning quality heuristic: combine estimate coverage and deviation.
            var estimateCoverage = total == 0 ? 1.0 : 1.0 - ((double)missingEstimateCount / total);
            estimateCoverage = Math.Clamp(estimateCoverage, 0.0, 1.0);

            var avgDeviation = perTaskDeviation.Count == 0 ? 0.0 : perTaskDeviation.Average();
            // Map deviation to 0..1 where 1 is great: 0 deviation => 1, 100% off => 0.
            var deviationQuality = 1.0 - Math.Clamp(avgDeviation, 0.0, 1.0);

            var planningQuality = Math.Clamp((estimateCoverage * 0.6) + (deviationQuality * 0.4), 0.0, 1.0);

            string? hintKey = null;
            if (total > 0)
            {
                var missingRate = 1.0 - estimateCoverage;
                if (missingRate >= settings.MissingEstimateThreshold)
                    hintKey = "ProductivityHintAddEstimates";
                else if (avgDeviation >= settings.DeviationThreshold)
                    hintKey = "ProductivityHintReviewEstimates";
            }

            return new ProductivityResult
            {
                ScorePercent = score,
                TotalTasks = total,
                DoneTasks = done,
                CompletionRate = completion,
                SmoothedCompletionRate = smoothed,
                EfficiencyRate = efficiency,
                OverdueCount = overdueCount,
                PlanningQualityRate = Math.Round(planningQuality, 3),
                PlanningHintKey = hintKey
            };
        }
    }
}
