namespace diplom.Models.Analytics
{
    public sealed class ProductivityResult
    {
        public double ScorePercent { get; init; }

        public int TotalTasks { get; init; }
        public int DoneTasks { get; init; }

        public double CompletionRate { get; init; } // 0..1 (raw, unsmoothed)
        public double SmoothedCompletionRate { get; init; } // 0..1 (Bayesian prior)
        public double EfficiencyRate { get; init; } // 0..1 (estimated vs actual time)
    }
}

