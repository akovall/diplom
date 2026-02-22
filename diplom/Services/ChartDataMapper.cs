using diplom.Models.Analytics;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Linq;

namespace diplom.Services
{
    public static class ChartDataMapper
    {
        public sealed class MappedChart
        {
            public ISeries[] Series { get; init; } = Array.Empty<ISeries>();
            public Axis[] XAxes { get; init; } = Array.Empty<Axis>();
            public Axis[] YAxes { get; init; } = Array.Empty<Axis>();
            public SolidColorPaint LegendTextPaint { get; init; } = new(new SKColor(220, 220, 220));
        }

        private readonly record struct ThemeChartColors(
            SKColor Text,
            SKColor Separator,
            SKColor Tick,
            SKColor Green,
            SKColor Red,
            SKColor Blue,
            SKColor Slate,
            SKColor PointFill);

        private static ThemeChartColors GetThemeColors(bool isDarkTheme)
        {
            return isDarkTheme
                ? new ThemeChartColors(
                    Text: new SKColor(238, 242, 247),
                    Separator: new SKColor(70, 70, 70, 80),
                    Tick: new SKColor(70, 70, 70, 120),
                    Green: new SKColor(72, 187, 120, 210),
                    Red: new SKColor(229, 62, 62, 210),
                    Blue: new SKColor(76, 202, 240),
                    Slate: new SKColor(120, 120, 120, 170),
                    PointFill: new SKColor(16, 16, 16, 255))
                : new ThemeChartColors(
                    Text: new SKColor(51, 65, 85),
                    Separator: new SKColor(148, 163, 184, 90),
                    Tick: new SKColor(148, 163, 184, 140),
                    Green: new SKColor(34, 197, 94, 200),
                    Red: new SKColor(239, 68, 68, 200),
                    Blue: new SKColor(14, 116, 144),
                    Slate: new SKColor(148, 163, 184, 200),
                    PointFill: new SKColor(255, 255, 255, 255));
        }

        public static MappedChart MapUserAnalytics(UserAnalyticsDto dto, bool isDarkTheme)
        {
            var days = dto.Days.OrderBy(d => d.DayUtc).ToList();
            var labels = days.Select(d => d.DayUtc.ToString("dd.MM")).ToArray();

            var worked = days.Select(d => d.WorkedHours).ToArray();
            var assigned = days.Select(d => d.TasksAssigned).ToArray();
            var completed = days.Select(d => d.TasksCompleted).ToArray();
            var overdue = days.Select(d => d.OverdueCompleted).ToArray();
            var onTimeCompleted = completed.Zip(overdue, (c, o) => Math.Max(0, c - o)).ToArray();
            var openFromAssigned = assigned.Zip(completed, (a, c) => Math.Max(0, a - c)).ToArray();

            var colors = GetThemeColors(isDarkTheme);
            var textPaint = new SolidColorPaint(colors.Text);

            var series = new ISeries[]
            {
                new StackedColumnSeries<int>
                {
                    Name = "Відкрито (призначено - виконано)",
                    Values = openFromAssigned,
                    Fill = new SolidColorPaint(colors.Slate),
                    Stroke = null,
                    MaxBarWidth = 26,
                    YToolTipLabelFormatter = point => $"Відкрито: {point.Coordinate.PrimaryValue:0}"
                },
                new StackedColumnSeries<int>
                {
                    Name = "Виконано вчасно",
                    Values = onTimeCompleted,
                    Fill = new SolidColorPaint(colors.Green),
                    Stroke = null,
                    MaxBarWidth = 26,
                    YToolTipLabelFormatter = point => $"Вчасно: {point.Coordinate.PrimaryValue:0}"
                },
                new StackedColumnSeries<int>
                {
                    Name = "Виконано із запізненням",
                    Values = overdue,
                    Fill = new SolidColorPaint(colors.Red),
                    Stroke = null,
                    MaxBarWidth = 26,
                    YToolTipLabelFormatter = point => $"Із запізненням: {point.Coordinate.PrimaryValue:0}"
                },
                new LineSeries<double>
                {
                    Name = "Відпрацьовані години",
                    Values = worked,
                    ScalesYAt = 1,
                    GeometrySize = 6,
                    Stroke = new SolidColorPaint(colors.Blue, 2),
                    Fill = null,
                    GeometryStroke = new SolidColorPaint(colors.Blue, 2),
                    GeometryFill = new SolidColorPaint(colors.PointFill),
                    YToolTipLabelFormatter = point => $"Години: {point.Coordinate.PrimaryValue:0.##}"
                }
            };

            var xAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = textPaint,
                    SeparatorsPaint = new SolidColorPaint(colors.Separator),
                    TicksPaint = new SolidColorPaint(colors.Tick),
                    TextSize = 11
                }
            };

            var yAxes = new[]
            {
                new Axis
                {
                    Name = "Задачі",
                    NamePaint = textPaint,
                    LabelsPaint = textPaint,
                    SeparatorsPaint = new SolidColorPaint(colors.Separator),
                    TextSize = 11,
                    MinLimit = 0
                },
                new Axis
                {
                    Name = "Години",
                    NamePaint = textPaint,
                    LabelsPaint = textPaint,
                    SeparatorsPaint = null,
                    TextSize = 11,
                    MinLimit = 0,
                    Position = AxisPosition.End
                }
            };

            return new MappedChart
            {
                Series = series,
                XAxes = xAxes,
                YAxes = yAxes,
                LegendTextPaint = textPaint
            };
        }

        public static MappedChart MapProjectHours(ProjectAnalyticsDto dto, bool isDarkTheme)
        {
            var days = dto.Days.OrderBy(d => d.DayUtc).ToList();
            var labels = days.Select(d => d.DayUtc.ToString("dd.MM")).ToArray();
            var worked = days.Select(d => d.WorkedHours).ToArray();

            var colors = GetThemeColors(isDarkTheme);
            var textPaint = new SolidColorPaint(colors.Text);

            var series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Години",
                    Values = worked,
                    Fill = new SolidColorPaint(colors.Blue),
                    Stroke = null,
                    MaxBarWidth = 22,
                    YToolTipLabelFormatter = point => $"Години: {point.Coordinate.PrimaryValue:0.##}"
                }
            };

            return new MappedChart
            {
                Series = series,
                XAxes = BuildSharedXAxes(labels, textPaint, colors),
                YAxes = new[]
                {
                    new Axis
                    {
                        Name = "Години",
                        NamePaint = textPaint,
                        LabelsPaint = textPaint,
                        SeparatorsPaint = new SolidColorPaint(colors.Separator),
                        MinLimit = 0,
                        TextSize = 11
                    }
                },
                LegendTextPaint = textPaint
            };
        }

        public static MappedChart MapProjectAssignedVsCompleted(ProjectAnalyticsDto dto, bool isDarkTheme)
        {
            var days = dto.Days.OrderBy(d => d.DayUtc).ToList();
            var labels = days.Select(d => d.DayUtc.ToString("dd.MM")).ToArray();
            var assigned = days.Select(d => d.TasksAssigned).ToArray();
            var completed = days.Select(d => d.TasksCompleted).ToArray();

            var colors = GetThemeColors(isDarkTheme);
            var textPaint = new SolidColorPaint(colors.Text);

            var series = new ISeries[]
            {
                new StackedColumnSeries<int>
                {
                    Name = "Призначено",
                    Values = assigned,
                    Fill = new SolidColorPaint(colors.Slate),
                    Stroke = null,
                    MaxBarWidth = 22,
                    YToolTipLabelFormatter = point => $"Призначено: {point.Coordinate.PrimaryValue:0}"
                },
                new StackedColumnSeries<int>
                {
                    Name = "Виконано",
                    Values = completed,
                    Fill = new SolidColorPaint(colors.Green),
                    Stroke = null,
                    MaxBarWidth = 22,
                    YToolTipLabelFormatter = point => $"Виконано: {point.Coordinate.PrimaryValue:0}"
                }
            };

            return new MappedChart
            {
                Series = series,
                XAxes = BuildSharedXAxes(labels, textPaint, colors),
                YAxes = new[]
                {
                    new Axis
                    {
                        Name = "Кількість задач",
                        NamePaint = textPaint,
                        LabelsPaint = textPaint,
                        SeparatorsPaint = new SolidColorPaint(colors.Separator),
                        MinLimit = 0,
                        TextSize = 11
                    }
                },
                LegendTextPaint = textPaint
            };
        }

        public static MappedChart MapProjectOverdueTrend(ProjectAnalyticsDto dto, bool isDarkTheme)
        {
            var days = dto.Days.OrderBy(d => d.DayUtc).ToList();
            var labels = days.Select(d => d.DayUtc.ToString("dd.MM")).ToArray();
            var overdue = days.Select(d => d.OverdueCompleted).ToArray();

            var colors = GetThemeColors(isDarkTheme);
            var textPaint = new SolidColorPaint(colors.Text);

            var series = new ISeries[]
            {
                new LineSeries<int>
                {
                    Name = "Прострочки",
                    Values = overdue,
                    GeometrySize = 6,
                    Stroke = new SolidColorPaint(colors.Red, 2),
                    Fill = null,
                    GeometryStroke = new SolidColorPaint(colors.Red, 2),
                    GeometryFill = new SolidColorPaint(colors.PointFill),
                    YToolTipLabelFormatter = point => $"Прострочки: {point.Coordinate.PrimaryValue:0}"
                }
            };

            return new MappedChart
            {
                Series = series,
                XAxes = BuildSharedXAxes(labels, textPaint, colors),
                YAxes = new[]
                {
                    new Axis
                    {
                        Name = "Кількість",
                        NamePaint = textPaint,
                        LabelsPaint = textPaint,
                        SeparatorsPaint = new SolidColorPaint(colors.Separator),
                        MinLimit = 0,
                        TextSize = 11
                    }
                },
                LegendTextPaint = textPaint
            };
        }

        private static Axis[] BuildSharedXAxes(string[] labels, SolidColorPaint textPaint, ThemeChartColors colors)
        {
            return new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = textPaint,
                    SeparatorsPaint = new SolidColorPaint(colors.Separator),
                    TicksPaint = new SolidColorPaint(colors.Tick),
                    TextSize = 11
                }
            };
        }
    }
}
