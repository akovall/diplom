using ClosedXML.Excel;
using diplom.Models.Analytics;
using System;
using System.Linq;

namespace diplom.Services
{
    public static class ExcelReportExporter
    {
        public static void ExportUserAnalyticsReport(UserAnalyticsDto dto, string filePath)
        {
            using var wb = new XLWorkbook();

            var wsSummary = wb.Worksheets.Add("Зведення");
            wsSummary.Style.Font.FontName = "Segoe UI";
            wsSummary.Style.Font.FontSize = 10;
            wsSummary.Cell(1, 1).Value = "Звіт аналітики користувача";
            wsSummary.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF"));
            wsSummary.Cell(3, 1).Value = "Користувач";
            wsSummary.Cell(3, 2).Value = dto.FullName;
            wsSummary.Cell(4, 1).Value = "Посада";
            wsSummary.Cell(4, 2).Value = dto.JobTitle;
            wsSummary.Cell(5, 1).Value = "Період (UTC)";
            wsSummary.Cell(5, 2).Value = $"{dto.FromUtc:yyyy-MM-dd} .. {dto.ToUtc:yyyy-MM-dd}";
            var completionRate = dto.TasksAssigned == 0 ? 0d : (double)dto.TasksCompletedFromAssignedInPeriod / dto.TasksAssigned;
            var overdueRate = dto.TasksCompleted == 0 ? 0d : (double)dto.OverdueCompleted / dto.TasksCompleted;
            wsSummary.Cell(7, 1).Value = "Відпрацьовано годин";
            wsSummary.Cell(7, 2).Value = dto.WorkedHours;
            wsSummary.Cell(8, 1).Value = "Призначено задач";
            wsSummary.Cell(8, 2).Value = dto.TasksAssigned;
            wsSummary.Cell(9, 1).Value = "Виконано задач (усі завершення в періоді)";
            wsSummary.Cell(9, 2).Value = dto.TasksCompleted;
            wsSummary.Cell(10, 1).Value = "Виконано прострочених";
            wsSummary.Cell(10, 2).Value = dto.OverdueCompleted;
            wsSummary.Cell(11, 1).Value = "Виконано з числа призначених у періоді";
            wsSummary.Cell(11, 2).Value = dto.TasksCompletedFromAssignedInPeriod;
            wsSummary.Cell(12, 1).Value = "Рівень виконання (призначені vs виконані з призначених)";
            wsSummary.Cell(12, 2).Value = completionRate;
            wsSummary.Cell(12, 2).Style.NumberFormat.Format = "0.0%";
            wsSummary.Cell(13, 1).Value = "Частка прострочених (від виконаних)";
            wsSummary.Cell(13, 2).Value = overdueRate;
            wsSummary.Cell(13, 2).Style.NumberFormat.Format = "0.0%";

            wsSummary.Cell(15, 1).Value = "Легенда кольорів (аркуш 'Щоденно')";
            wsSummary.Range(15, 1, 15, 2).Merge().Style.Font.SetBold();
            wsSummary.Cell(16, 1).Value = "Сині смуги";
            wsSummary.Cell(16, 2).Value = "Відпрацьовані години";
            wsSummary.Cell(17, 1).Value = "Зелені смуги";
            wsSummary.Cell(17, 2).Value = "Виконано задач";
            wsSummary.Cell(18, 1).Value = "Червоні смуги";
            wsSummary.Cell(18, 2).Value = "Прострочені виконання";
            wsSummary.Cell(19, 1).Value = "Сірі смуги";
            wsSummary.Cell(19, 2).Value = "Чиста зміна беклогу (призначено - виконано)";
            wsSummary.Cell(16, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));
            wsSummary.Cell(17, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
            wsSummary.Cell(18, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEE2E2"));
            wsSummary.Cell(19, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E5E7EB"));

            wsSummary.Range(3, 1, 19, 1).Style.Font.SetBold();
            wsSummary.Range(3, 2, 19, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            wsSummary.Columns().AdjustToContents();

            var wsDaily = wb.Worksheets.Add("Щоденно");
            wsDaily.Cell(1, 1).Value = "День (UTC)";
            wsDaily.Cell(1, 2).Value = "Відпрацьовані години";
            wsDaily.Cell(1, 3).Value = "Призначено задач";
            wsDaily.Cell(1, 4).Value = "Виконано задач";
            wsDaily.Cell(1, 5).Value = "Було прострочено";
            wsDaily.Cell(1, 6).Value = "Виконано вчасно";
            wsDaily.Cell(1, 7).Value = "Чиста зміна беклогу";
            wsDaily.Range(1, 1, 1, 7).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));
            wsDaily.Range(1, 1, 1, 7).Style.Font.FontColor = XLColor.Black;

            var row = 2;
            foreach (var d in dto.Days.OrderBy(x => x.DayUtc))
            {
                wsDaily.Cell(row, 1).Value = d.DayUtc.ToString("yyyy-MM-dd");
                wsDaily.Cell(row, 2).Value = d.WorkedHours;
                wsDaily.Cell(row, 3).Value = d.TasksAssigned;
                wsDaily.Cell(row, 4).Value = d.TasksCompleted;
                wsDaily.Cell(row, 5).Value = d.OverdueCompleted;
                wsDaily.Cell(row, 6).Value = Math.Max(0, d.TasksCompleted - d.OverdueCompleted);
                wsDaily.Cell(row, 7).Value = d.TasksAssigned - d.TasksCompleted;
                row++;
            }

            if (row > 2)
            {
                var dataRange = wsDaily.Range(1, 1, row - 1, 7);
                var dailyTable = dataRange.CreateTable("ЩоденнаАналітика");
                dailyTable.Theme = XLTableTheme.None;
                dailyTable.HeadersRow().Style.Font.SetBold();
                dailyTable.HeadersRow().Style.Font.FontColor = XLColor.Black;
                dailyTable.HeadersRow().Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));

                wsDaily.Range(2, 2, row - 1, 2).Style.NumberFormat.Format = "0.00";
                wsDaily.Range(2, 3, row - 1, 7).Style.NumberFormat.Format = "0";

                wsDaily.Range(2, 2, row - 1, 2).AddConditionalFormat().DataBar(XLColor.FromHtml("#60A5FA"));
                wsDaily.Range(2, 4, row - 1, 4).AddConditionalFormat().DataBar(XLColor.FromHtml("#22C55E"));
                wsDaily.Range(2, 5, row - 1, 5).AddConditionalFormat().DataBar(XLColor.FromHtml("#EF4444"));
                wsDaily.Range(2, 7, row - 1, 7).AddConditionalFormat().DataBar(XLColor.FromHtml("#94A3B8"));
            }

            wsDaily.Columns().AdjustToContents();
            wsDaily.SheetView.FreezeRows(1);

            var wsTasks = wb.Worksheets.Add("Останні виконані задачі");
            wsTasks.Cell(1, 1).Value = "ID задачі";
            wsTasks.Cell(1, 2).Value = "Назва";
            wsTasks.Cell(1, 3).Value = "Завершено (UTC)";
            wsTasks.Cell(1, 4).Value = "Дедлайн (UTC)";
            wsTasks.Cell(1, 5).Value = "Було прострочено";
            wsTasks.Cell(1, 6).Value = "Оцінка, год";
            wsTasks.Cell(1, 7).Value = "Факт, год";
            wsTasks.Cell(1, 8).Value = "Співвідношення факт/оцінка";
            wsTasks.Range(1, 1, 1, 8).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FFF7ED"));
            wsTasks.Range(1, 1, 1, 8).Style.Font.FontColor = XLColor.Black;

            row = 2;
            foreach (var t in dto.RecentCompletedTasks.OrderByDescending(x => x.CompletedAtUtc))
            {
                wsTasks.Cell(row, 1).Value = t.TaskId;
                wsTasks.Cell(row, 2).Value = t.Title;
                wsTasks.Cell(row, 3).Value = t.CompletedAtUtc.ToString("O");
                wsTasks.Cell(row, 4).Value = t.DeadlineUtc?.ToString("O") ?? "";
                wsTasks.Cell(row, 5).Value = t.WasOverdue ? "так" : "ні";
                wsTasks.Cell(row, 6).Value = t.EstimatedHours;
                wsTasks.Cell(row, 7).Value = t.ActualHours;
                wsTasks.Cell(row, 8).Value = t.EstimatedHours <= 0 ? 0 : t.ActualHours / t.EstimatedHours;
                row++;
            }
            if (row > 2)
            {
                wsTasks.Range(2, 6, row - 1, 8).Style.NumberFormat.Format = "0.00";
                wsTasks.Range(2, 8, row - 1, 8).Style.NumberFormat.Format = "0.00x";
                var tasksTable = wsTasks.Range(1, 1, row - 1, 8).CreateTable("ОстанніВиконаніЗадачі");
                tasksTable.Theme = XLTableTheme.None;
                tasksTable.HeadersRow().Style.Font.SetBold();
                tasksTable.HeadersRow().Style.Font.FontColor = XLColor.Black;
                tasksTable.HeadersRow().Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FFF7ED"));
                wsTasks.Range(2, 8, row - 1, 8).AddConditionalFormat().ColorScale()
                    .LowestValue(XLColor.FromHtml("#22C55E"))
                    .Midpoint(XLCFContentType.Number, "1", XLColor.FromHtml("#FACC15"))
                    .HighestValue(XLColor.FromHtml("#EF4444"));
            }
            wsTasks.Columns().AdjustToContents();
            wsTasks.SheetView.FreezeRows(1);

            wb.SaveAs(filePath);
        }

        public static void ExportProjectAnalyticsReport(ProjectAnalyticsDto dto, string filePath)
        {
            using var wb = new XLWorkbook();

            var wsSummary = wb.Worksheets.Add("Зведення");
            wsSummary.Style.Font.FontName = "Segoe UI";
            wsSummary.Style.Font.FontSize = 10;

            wsSummary.Cell(1, 1).Value = "Звіт аналітики проєкту";
            wsSummary.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF"));

            wsSummary.Cell(3, 1).Value = "Проєкт";
            wsSummary.Cell(3, 2).Value = dto.ProjectTitle;
            wsSummary.Cell(4, 1).Value = "Період (UTC)";
            wsSummary.Cell(4, 2).Value = $"{dto.FromUtc:yyyy-MM-dd} .. {dto.ToUtc:yyyy-MM-dd}";
            wsSummary.Cell(6, 1).Value = "Відпрацьовано годин";
            wsSummary.Cell(6, 2).Value = dto.WorkedHours;
            wsSummary.Cell(7, 1).Value = "Призначено задач";
            wsSummary.Cell(7, 2).Value = dto.TasksAssigned;
            wsSummary.Cell(8, 1).Value = "Виконано задач";
            wsSummary.Cell(8, 2).Value = dto.TasksCompleted;
            wsSummary.Cell(9, 1).Value = "Прострочено виконано";
            wsSummary.Cell(9, 2).Value = dto.OverdueCompleted;

            wsSummary.Cell(11, 1).Value = "Легенда кольорів (аркуш 'Щоденно')";
            wsSummary.Range(11, 1, 11, 2).Merge().Style.Font.SetBold();
            wsSummary.Cell(12, 1).Value = "Сині смуги";
            wsSummary.Cell(12, 2).Value = "Відпрацьовані години";
            wsSummary.Cell(13, 1).Value = "Сірі смуги";
            wsSummary.Cell(13, 2).Value = "Призначено задач";
            wsSummary.Cell(14, 1).Value = "Зелені смуги";
            wsSummary.Cell(14, 2).Value = "Виконано задач";
            wsSummary.Cell(15, 1).Value = "Червоні смуги";
            wsSummary.Cell(15, 2).Value = "Прострочені виконання";
            wsSummary.Cell(12, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));
            wsSummary.Cell(13, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E5E7EB"));
            wsSummary.Cell(14, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
            wsSummary.Cell(15, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEE2E2"));

            wsSummary.Range(3, 1, 15, 1).Style.Font.SetBold();
            wsSummary.Range(3, 2, 15, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            wsSummary.Columns().AdjustToContents();

            var wsDaily = wb.Worksheets.Add("Щоденно");
            wsDaily.Cell(1, 1).Value = "День (UTC)";
            wsDaily.Cell(1, 2).Value = "Відпрацьовані години";
            wsDaily.Cell(1, 3).Value = "Призначено задач";
            wsDaily.Cell(1, 4).Value = "Виконано задач";
            wsDaily.Cell(1, 5).Value = "Було прострочено";
            wsDaily.Range(1, 1, 1, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));
            wsDaily.Range(1, 1, 1, 5).Style.Font.FontColor = XLColor.Black;

            var row = 2;
            foreach (var day in dto.Days.OrderBy(x => x.DayUtc))
            {
                wsDaily.Cell(row, 1).Value = day.DayUtc.ToString("yyyy-MM-dd");
                wsDaily.Cell(row, 2).Value = day.WorkedHours;
                wsDaily.Cell(row, 3).Value = day.TasksAssigned;
                wsDaily.Cell(row, 4).Value = day.TasksCompleted;
                wsDaily.Cell(row, 5).Value = day.OverdueCompleted;
                row++;
            }

            if (row > 2)
            {
                var dailyTable = wsDaily.Range(1, 1, row - 1, 5).CreateTable("ЩоденнаАналітикаПроєкту");
                dailyTable.Theme = XLTableTheme.None;
                dailyTable.HeadersRow().Style.Font.SetBold();
                dailyTable.HeadersRow().Style.Font.FontColor = XLColor.Black;
                dailyTable.HeadersRow().Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EFF6FF"));

                wsDaily.Range(2, 2, row - 1, 2).Style.NumberFormat.Format = "0.00";
                wsDaily.Range(2, 3, row - 1, 5).Style.NumberFormat.Format = "0";

                wsDaily.Range(2, 2, row - 1, 2).AddConditionalFormat().DataBar(XLColor.FromHtml("#60A5FA"));
                wsDaily.Range(2, 3, row - 1, 3).AddConditionalFormat().DataBar(XLColor.FromHtml("#94A3B8"));
                wsDaily.Range(2, 4, row - 1, 4).AddConditionalFormat().DataBar(XLColor.FromHtml("#22C55E"));
                wsDaily.Range(2, 5, row - 1, 5).AddConditionalFormat().DataBar(XLColor.FromHtml("#EF4444"));
            }

            wsDaily.Columns().AdjustToContents();
            wsDaily.SheetView.FreezeRows(1);

            var wsTasks = wb.Worksheets.Add("Останні виконані задачі");
            wsTasks.Cell(1, 1).Value = "ID задачі";
            wsTasks.Cell(1, 2).Value = "Назва";
            wsTasks.Cell(1, 3).Value = "Виконавець";
            wsTasks.Cell(1, 4).Value = "Завершено (UTC)";
            wsTasks.Cell(1, 5).Value = "Дедлайн (UTC)";
            wsTasks.Cell(1, 6).Value = "Було прострочено";
            wsTasks.Cell(1, 7).Value = "Оцінка, год";
            wsTasks.Cell(1, 8).Value = "Факт, год";
            wsTasks.Cell(1, 9).Value = "Співвідношення факт/оцінка";
            wsTasks.Range(1, 1, 1, 9).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FFF7ED"));
            wsTasks.Range(1, 1, 1, 9).Style.Font.FontColor = XLColor.Black;

            row = 2;
            foreach (var task in dto.RecentCompletedTasks.OrderByDescending(x => x.CompletedAtUtc))
            {
                wsTasks.Cell(row, 1).Value = task.TaskId;
                wsTasks.Cell(row, 2).Value = task.Title;
                wsTasks.Cell(row, 3).Value = task.AssigneeName;
                wsTasks.Cell(row, 4).Value = task.CompletedAtUtc.ToString("O");
                wsTasks.Cell(row, 5).Value = task.DeadlineUtc?.ToString("O") ?? string.Empty;
                wsTasks.Cell(row, 6).Value = task.WasOverdue ? "так" : "ні";
                wsTasks.Cell(row, 7).Value = task.EstimatedHours;
                wsTasks.Cell(row, 8).Value = task.ActualHours;
                wsTasks.Cell(row, 9).Value = task.EstimatedHours <= 0 ? 0 : task.ActualHours / task.EstimatedHours;
                row++;
            }

            if (row > 2)
            {
                wsTasks.Range(2, 7, row - 1, 9).Style.NumberFormat.Format = "0.00";
                wsTasks.Range(2, 9, row - 1, 9).Style.NumberFormat.Format = "0.00x";

                var tasksTable = wsTasks.Range(1, 1, row - 1, 9).CreateTable("ОстанніВиконаніЗадачіПроєкту");
                tasksTable.Theme = XLTableTheme.None;
                tasksTable.HeadersRow().Style.Font.SetBold();
                tasksTable.HeadersRow().Style.Font.FontColor = XLColor.Black;
                tasksTable.HeadersRow().Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FFF7ED"));

                wsTasks.Range(2, 9, row - 1, 9).AddConditionalFormat().ColorScale()
                    .LowestValue(XLColor.FromHtml("#22C55E"))
                    .Midpoint(XLCFContentType.Number, "1", XLColor.FromHtml("#FACC15"))
                    .HighestValue(XLColor.FromHtml("#EF4444"));
            }

            wsTasks.Columns().AdjustToContents();
            wsTasks.SheetView.FreezeRows(1);

            wb.SaveAs(filePath);
        }
    }
}

