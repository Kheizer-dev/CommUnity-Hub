using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;

namespace CommUnity_Hub
{
    public class BlotterReport
    {
        public string? CaseID { get; set; }
        public string? IncidentDetails { get; set; }
        public DateTime DateReported { get; set; }
        public string? Location { get; set; }       // Added Location
        public string? PartiesInvolved { get; set; } // Added Parties Involved
        public string? Evidence { get; set; }       // Added Evidence

        // Generate the PDF for a blotter report
        [Obsolete]
        public static void GenerateBlotterReportPdf(BlotterReport report)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Define fonts and layout
            var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
            var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var regularFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            // Title and header section
            gfx.DrawString("Blotter Report", titleFont, XBrushes.Black, new XRect(0, 20, page.Width, 40), XStringFormats.TopCenter);
            gfx.DrawString($"Case ID: {report.CaseID}", headerFont, XBrushes.Black, new XRect(40, 60, page.Width - 80, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Date Reported: {report.DateReported:MMM dd, yyyy}", headerFont, XBrushes.Black, new XRect(40, 80, page.Width - 80, 20), XStringFormats.TopLeft);

            // Details section
            gfx.DrawString("Incident Details:", headerFont, XBrushes.Black, new XRect(40, 120, page.Width - 80, 20), XStringFormats.TopLeft);
            gfx.DrawString(report.IncidentDetails, regularFont, XBrushes.Black, new XRect(40, 140, page.Width - 80, page.Height - 200), XStringFormats.TopLeft);

            gfx.DrawString("Location:", headerFont, XBrushes.Black, new XRect(40, 240, page.Width - 80, 20), XStringFormats.TopLeft);
            gfx.DrawString(report.Location, regularFont, XBrushes.Black, new XRect(40, 260, page.Width - 80, 20), XStringFormats.TopLeft);

            gfx.DrawString("Parties Involved:", headerFont, XBrushes.Black, new XRect(40, 300, page.Width - 80, 20), XStringFormats.TopLeft);
            gfx.DrawString(report.PartiesInvolved, regularFont, XBrushes.Black, new XRect(40, 320, page.Width - 80, 20), XStringFormats.TopLeft);

            if (!string.IsNullOrEmpty(report.Evidence))
            {
                gfx.DrawString("Evidence:", headerFont, XBrushes.Black, new XRect(40, 360, page.Width - 80, 20), XStringFormats.TopLeft);
                gfx.DrawString(report.Evidence, regularFont, XBrushes.Black, new XRect(40, 380, page.Width - 80, 20), XStringFormats.TopLeft);
            }

            // Save PDF in Documents\CommUnityHub Blotter Reports
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Blotter Reports");
            Directory.CreateDirectory(folderPath); // Ensure directory exists
            string filePath = Path.Combine(folderPath, $"BlotterReport_{report.CaseID}.pdf");

            document.Save(filePath);
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            document.Close();
        }
    }
}
