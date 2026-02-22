using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models.Analytics;
using diplom.Services;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.viewmodels
{
    public sealed partial class ProjectAnalyticsViewModel : ObservableObject
    {
        private readonly AnalyticsService _analyticsService;
        private readonly int _projectId;
        private ProjectAnalyticsDto? _raw;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _rangeText = string.Empty;

        [ObservableProperty] private string _kpiWorkedHours = "0h";
        [ObservableProperty] private string _kpiAssigned = "0";
        [ObservableProperty] private string _kpiCompleted = "0";
        [ObservableProperty] private string _kpiOverdue = "0";

        [ObservableProperty] private ISeries[] _hoursSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _hoursXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _hoursYAxes = Array.Empty<Axis>();

        [ObservableProperty] private ISeries[] _tasksSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _tasksXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _tasksYAxes = Array.Empty<Axis>();

        [ObservableProperty] private ISeries[] _overdueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _overdueXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _overdueYAxes = Array.Empty<Axis>();

        [ObservableProperty] private SolidColorPaint _legendTextPaint = new(new SKColor(220, 220, 220));
        [ObservableProperty] private ZoomAndPanMode _zoomMode = ZoomAndPanMode.X;
        [ObservableProperty] private ProjectPeriodOption _selectedPeriod = ProjectPeriodOption.Days30;

        public ObservableCollection<ProjectAnalyticsTaskItem> RecentCompletedTasks { get; } = new();

        public ProjectAnalyticsViewModel(int projectId, string projectName)
        {
            _projectId = projectId;
            _analyticsService = new AnalyticsService(ApiClient.Instance);
            Title = $"Проєкт: {projectName}";
            _ = LoadAsync();
        }

        public ProjectPeriodOption[] PeriodOptions => Enum.GetValues(typeof(ProjectPeriodOption)).Cast<ProjectPeriodOption>().ToArray();

        [RelayCommand]
        private async Task ApplyPeriodAsync() => await LoadAsync();

        [RelayCommand]
        private async Task ReloadAsync() => await LoadAsync();

        [RelayCommand]
        private void ExportExcel()
        {
            if (_raw == null) return;

            var dlg = new SaveFileDialog
            {
                Title = "Експорт звіту проєкту",
                FileName = $"project-{_projectId}-report.xlsx",
                Filter = "Excel (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true) return;
            ExcelReportExporter.ExportProjectAnalyticsReport(_raw, dlg.FileName);
        }

        private int ResolveDays()
        {
            return SelectedPeriod switch
            {
                ProjectPeriodOption.Days7 => 7,
                ProjectPeriodOption.Days30 => 30,
                ProjectPeriodOption.Month => Math.Clamp((DateTime.UtcNow.Date - new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Days + 1, 7, 90),
                _ => 30
            };
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var dto = await _analyticsService.GetProjectAnalyticsAsync(_projectId, ResolveDays());
                _raw = dto;

                if (dto == null)
                {
                    RangeText = string.Empty;
                    KpiWorkedHours = "0h";
                    KpiAssigned = "0";
                    KpiCompleted = "0";
                    KpiOverdue = "0";
                    return;
                }

                RangeText = $"Період: {dto.FromUtc:yyyy-MM-dd} .. {dto.ToUtc:yyyy-MM-dd} (UTC)";
                KpiWorkedHours = $"{dto.WorkedHours:0.##}h";
                KpiAssigned = dto.TasksAssigned.ToString();
                KpiCompleted = dto.TasksCompleted.ToString();
                KpiOverdue = dto.OverdueCompleted.ToString();

                ApplyChartMapping(dto);
                LoadRecentTasks(dto);
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
            ApplyChartMapping(_raw);
        }

        private void ApplyChartMapping(ProjectAnalyticsDto dto)
        {
            var isDark = ThemeService.CurrentTheme == "Dark";

            var hours = ChartDataMapper.MapProjectHours(dto, isDark);
            HoursSeries = hours.Series;
            HoursXAxes = hours.XAxes;
            HoursYAxes = hours.YAxes;

            var tasks = ChartDataMapper.MapProjectAssignedVsCompleted(dto, isDark);
            TasksSeries = tasks.Series;
            TasksXAxes = tasks.XAxes;
            TasksYAxes = tasks.YAxes;

            var overdue = ChartDataMapper.MapProjectOverdueTrend(dto, isDark);
            OverdueSeries = overdue.Series;
            OverdueXAxes = overdue.XAxes;
            OverdueYAxes = overdue.YAxes;

            LegendTextPaint = hours.LegendTextPaint;
        }

        private void LoadRecentTasks(ProjectAnalyticsDto dto)
        {
            RecentCompletedTasks.Clear();
            foreach (var task in dto.RecentCompletedTasks.OrderByDescending(t => t.CompletedAtUtc))
            {
                RecentCompletedTasks.Add(new ProjectAnalyticsTaskItem
                {
                    TaskId = task.TaskId,
                    Title = task.Title,
                    AssigneeName = task.AssigneeName,
                    CompletedAtText = task.CompletedAtUtc.ToString("dd.MM.yyyy"),
                    OverdueText = task.WasOverdue ? "Прострочено" : string.Empty,
                    EstimatedHours = task.EstimatedHours,
                    ActualHours = task.ActualHours
                });
            }
        }
    }

    public enum ProjectPeriodOption
    {
        Days7,
        Days30,
        Month
    }

    public sealed class ProjectAnalyticsTaskItem
    {
        public int TaskId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string AssigneeName { get; init; } = string.Empty;
        public string CompletedAtText { get; init; } = string.Empty;
        public string OverdueText { get; init; } = string.Empty;
        public double EstimatedHours { get; init; }
        public double ActualHours { get; init; }
    }
}
