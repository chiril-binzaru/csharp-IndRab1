using FastReport;
using FastReport.Export.Html;
using FastReport.Utils;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace IndRab1;

public static class ReportHelper
{
    private static float Cm(float value) => Units.Centimeters * value;

    // ===== REPORT 1: Participants by Event =====

    public static void ShowParticipantsByEventReport(string eventTitle, DataTable data)
    {
        var report = new Report();
        report.Dictionary.RegisterData(data, "Data", true);

        var page = CreatePage();
        report.Pages.Add(page);

        AddTitleBand(page,
            $"Participants — {eventTitle}",
            $"Generated: {DateTime.Now:dd.MM.yyyy HH:mm}");

        float[] widths  = { Cm(4.5f), Cm(4.5f), Cm(7f), Cm(3f) };
        string[] headers = { "First Name", "Last Name", "Email", "Status" };
        string[] fields  = { "FirstName", "LastName", "Email", "Status" };

        AddHeaderBand(page, headers, widths);
        AddDataBand(page, report, "Data", fields, widths);

        ExportAndOpen(report);
    }

    // ===== REPORT 2: Events by Period =====

    public static void ShowEventsByPeriodReport(DateTime from, DateTime to, DataTable data)
    {
        var report = new Report();
        report.Dictionary.RegisterData(data, "Data", true);

        var page = CreatePage();
        report.Pages.Add(page);

        AddTitleBand(page,
            $"Events {from:dd.MM.yyyy} — {to:dd.MM.yyyy}",
            $"Generated: {DateTime.Now:dd.MM.yyyy HH:mm}");

        float[] widths   = { Cm(6f), Cm(3f), Cm(4.5f), Cm(3f), Cm(2.5f) };
        string[] headers = { "Title", "Date", "Location", "Type", "Participants" };
        string[] fields  = { "Title", "EventDate", "Location", "EventType", "ParticipantCount" };

        AddHeaderBand(page, headers, widths);
        AddDataBand(page, report, "Data", fields, widths);

        ExportAndOpen(report);
    }

    // ===== HELPERS =====

    private static void ExportAndOpen(Report report)
    {
        report.Prepare();
        var path = Path.Combine(Path.GetTempPath(), $"report_{DateTime.Now:yyyyMMddHHmmss}.html");
        var export = new HTMLExport { SinglePage = true, Navigator = false };
        report.Export(export, path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static ReportPage CreatePage()
    {
        return new ReportPage
        {
            PaperWidth   = 210,
            PaperHeight  = 297,
            LeftMargin   = 10,
            RightMargin  = 10,
            TopMargin    = 15,
            BottomMargin = 15
        };
    }

    private static void AddTitleBand(ReportPage page, string title, string subtitle)
    {
        var band = new ReportTitleBand { Height = Cm(1.6f) };
        page.ReportTitle = band;

        var titleObj = new TextObject
        {
            Bounds    = new RectangleF(0, 0, Cm(19), Cm(1f)),
            Text      = title,
            Font      = new Font("Segoe UI", 14, FontStyle.Bold),
            HorzAlign = HorzAlign.Center,
            VertAlign = VertAlign.Center
        };
        band.Objects.Add(titleObj);

        var subtitleObj = new TextObject
        {
            Bounds    = new RectangleF(0, Cm(1.05f), Cm(19), Cm(0.45f)),
            Text      = subtitle,
            Font      = new Font("Segoe UI", 9),
            HorzAlign = HorzAlign.Center,
            TextColor = Color.Gray
        };
        band.Objects.Add(subtitleObj);
    }

    private static void AddHeaderBand(ReportPage page, string[] headers, float[] widths)
    {
        var band = new PageHeaderBand { Height = Cm(0.8f) };
        page.PageHeader = band;

        float x = 0;
        foreach (var (header, width) in headers.Zip(widths))
        {
            var cell = new TextObject
            {
                Bounds     = new RectangleF(x, 0, width, Cm(0.8f)),
                Text       = header,
                Font       = new Font("Segoe UI", 9, FontStyle.Bold),
                FillColor  = Color.FromArgb(44, 62, 80),
                TextColor  = Color.White,
                HorzAlign  = HorzAlign.Center,
                VertAlign  = VertAlign.Center
            };
            cell.Border.Lines = BorderLines.All;
            cell.Border.Color = Color.FromArgb(30, 45, 60);
            band.Objects.Add(cell);
            x += width;
        }
    }

    private static void AddDataBand(ReportPage page, Report report,
        string dataSourceName, string[] fields, float[] widths)
    {
        var band = new DataBand
        {
            DataSource = report.GetDataSource(dataSourceName),
            Height     = Cm(0.65f)
        };
        page.Bands.Add(band);

        float x = 0;
        foreach (var (field, width) in fields.Zip(widths))
        {
            var cell = new TextObject
            {
                Bounds    = new RectangleF(x, 0, width, Cm(0.65f)),
                Text      = $"[{dataSourceName}.{field}]",
                Font      = new Font("Segoe UI", 9),
                VertAlign = VertAlign.Center
            };
            cell.Border.Lines = BorderLines.All;
            cell.Border.Color = Color.FromArgb(213, 219, 219);
            cell.Padding = new Padding(3, 0, 0, 0);
            band.Objects.Add(cell);
            x += width;
        }
    }
}
