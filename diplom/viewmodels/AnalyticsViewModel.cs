using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models.Analytics;
using diplom.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.viewmodels
{
    public sealed partial class AnalyticsViewModel : ObservableObject
    {
        private readonly AnalyticsService _analyticsService;
        private readonly int _userId;

        private UserAnalyticsDto? _raw;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _rangeText = string.Empty;

        [ObservableProperty] private string _kpiWorkedHours = "0h";
        [ObservableProperty] private string _kpiCompleted = "0";
        [ObservableProperty] private string _kpiOverdue = "0";
        [ObservableProperty] private string _kpiCompletionRate = "0%";

        [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();
        [ObservableProperty] private SolidColorPaint _legendTextPaint = new(new SkiaSharp.SKColor(220, 220, 220));
        [ObservableProperty] private ZoomAndPanMode _zoomMode = ZoomAndPanMode.X;

        [ObservableProperty] private PeriodOption _selectedPeriod = PeriodOption.Days30;

        public ObservableCollection<AnalyticsTaskItem> RecentCompletedTasks { get; } = new();

        public AnalyticsViewModel(int userId, string fullName, string jobTitle)
        {
            _userId = userId;
            _analyticsService = new AnalyticsService(ApiClient.Instance);
            Title = string.IsNullOrWhiteSpace(jobTitle) ? fullName : $"{fullName} · {jobTitle}";

            _ = LoadAsync();
        }

        public PeriodOption[] PeriodOptions => Enum.GetValues(typeof(PeriodOption)).Cast<PeriodOption>().ToArray();

        [RelayCommand]
        private async Task ApplyPeriodAsync() => await LoadAsync();

        [RelayCommand]
        private async Task ReloadAsync() => await LoadAsync();

        private int ResolveDays()
        {
            return SelectedPeriod switch
            {
                PeriodOption.Days7 => 7,
                PeriodOption.Days30 => 30,
                PeriodOption.Month => Math.Clamp((DateTime.UtcNow.Date - new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Days + 1, 7, 90),
                _ => 30
            };
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var dto = await _analyticsService.GetUserAnalyticsAsync(_userId, ResolveDays());
                _raw = dto;
                if (dto == null)
                {
                    RangeText = string.Empty;
                    Series = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    YAxes = Array.Empty<Axis>();
                    LegendTextPaint = new SolidColorPaint(new SkiaSharp.SKColor(220, 220, 220));
                    KpiWorkedHours = "0h";
                    KpiCompleted = "0";
                    KpiOverdue = "0";
                    KpiCompletionRate = "0%";
                    RecentCompletedTasks.Clear();
                    return;
                }

                RangeText = $"Період: {dto.FromUtc:yyyy-MM-dd} .. {dto.ToUtc:yyyy-MM-dd} (UTC)";

                KpiWorkedHours = $"{dto.WorkedHours:0.##}h";
                KpiCompleted = dto.TasksCompleted.ToString();
                KpiOverdue = dto.OverdueCompleted.ToString();
                var completionRate = dto.TasksAssigned == 0
                    ? 0
                    : (double)dto.TasksCompletedFromAssignedInPeriod / dto.TasksAssigned;
                KpiCompletionRate = $"{Math.Round(completionRate * 100, 1)}%";

                var mapped = ChartDataMapper.MapUserAnalytics(dto, ThemeService.CurrentTheme == "Dark");
                Series = mapped.Series;
                XAxes = mapped.XAxes;
                YAxes = mapped.YAxes;
                LegendTextPaint = mapped.LegendTextPaint;

                RecentCompletedTasks.Clear();
                foreach (var t in dto.RecentCompletedTasks.OrderByDescending(t => t.CompletedAtUtc))
                {
                    RecentCompletedTasks.Add(new AnalyticsTaskItem
                    {
                        TaskId = t.TaskId,
                        Title = t.Title,
                        CompletedAtText = t.CompletedAtUtc.ToString("dd.MM.yyyy"),
                        OverdueText = t.WasOverdue ? "Прострочено" : string.Empty,
                        EstimatedHours = t.EstimatedHours,
                        ActualHours = t.ActualHours
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                RangeText = $"Помилка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void RefreshChartTheme()
        {
            if (_raw == null) return;
            var mapped = ChartDataMapper.MapUserAnalytics(_raw, ThemeService.CurrentTheme == "Dark");
            Series = mapped.Series;
            XAxes = mapped.XAxes;
            YAxes = mapped.YAxes;
            LegendTextPaint = mapped.LegendTextPaint;
        }

        [RelayCommand]
        private void ExportExcel()
        {
            if (_raw == null) return;

            var dlg = new SaveFileDialog
            {
                Title = "Експорт в Excel",
                FileName = $"user-{_userId}-report.xlsx",
                Filter = "Excel (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            ExcelReportExporter.ExportUserAnalyticsReport(_raw, dlg.FileName);
        }
    }

    public enum PeriodOption
    {
        Days7,
        Days30,
        Month
    }

    public sealed class AnalyticsTaskItem
    {
        public int TaskId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string CompletedAtText { get; init; } = string.Empty;
        public string OverdueText { get; init; } = string.Empty;
        public double EstimatedHours { get; init; }
        public double ActualHours { get; init; }
    }
}
