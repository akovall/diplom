using diplom.Models.Analytics;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
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

            var openColor = isDarkTheme ? new SKColor(120, 120, 120, 170) : new SKColor(148, 163, 184, 200);
            var onTimeColor = isDarkTheme ? new SKColor(72, 187, 120, 210) : new SKColor(34, 197, 94, 200);
            var overdueColor = isDarkTheme ? new SKColor(229, 62, 62, 210) : new SKColor(239, 68, 68, 200);
            var lineColor = isDarkTheme ? new SKColor(76, 202, 240) : new SKColor(14, 116, 144);
            var chartTextColor = isDarkTheme ? new SKColor(238, 242, 247) : new SKColor(51, 65, 85);
            var separatorColor = isDarkTheme ? new SKColor(70, 70, 70, 80) : new SKColor(148, 163, 184, 90);
            var tickColor = isDarkTheme ? new SKColor(70, 70, 70, 120) : new SKColor(148, 163, 184, 140);
            var pointFill = isDarkTheme ? new SKColor(16, 16, 16, 255) : new SKColor(255, 255, 255, 255);

            // Primary Y axis: tasks (stacked columns). Secondary Y axis: worked hours (line).
            var series = new ISeries[]
            {
                new StackedColumnSeries<int>
                {
                    Name = "Відкрито (призначено - виконано)",
                    Values = openFromAssigned,
                    Fill = new SolidColorPaint(openColor),
                    Stroke = null,
                    MaxBarWidth = 26,
                    YToolTipLabelFormatter = point => $"Відкрито: {point.Coordinate.PrimaryValue:0}"
                },
                new StackedColumnSeries<int>
                {
                    Name = "Виконано вчасно",
                    Values = onTimeCompleted,
                    Fill = new SolidColorPaint(onTimeColor),
                    Stroke = null,
                    MaxBarWidth = 26,
                    YToolTipLabelFormatter = point => $"Вчасно: {point.Coordinate.PrimaryValue:0}"
                },
                new StackedColumnSeries<int>
                {
                    Name = "Виконано із запізненням",
                    Values = overdue,
                    Fill = new SolidColorPaint(overdueColor),
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
                    Stroke = new SolidColorPaint(lineColor, 2),
                    Fill = null,
                    GeometryStroke = new SolidColorPaint(lineColor, 2),
                    GeometryFill = new SolidColorPaint(pointFill),
                    YToolTipLabelFormatter = point => $"Години: {point.Coordinate.PrimaryValue:0.##}"
                }
            };

            var textPaint = new SolidColorPaint(chartTextColor);

            var xAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = textPaint,
                    SeparatorsPaint = new SolidColorPaint(separatorColor),
                    TicksPaint = new SolidColorPaint(tickColor),
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
                    SeparatorsPaint = new SolidColorPaint(separatorColor),
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
                    Position = LiveChartsCore.Measure.AxisPosition.End
                }
            };

            return new MappedChart
            {
                Series = series,
                XAxes = xAxes,
                YAxes = yAxes,
                LegendTextPaint = new SolidColorPaint(chartTextColor)
            };
        }
    }
}
