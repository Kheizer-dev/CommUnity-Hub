using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Dapper;
namespace CommUnity_Hub
{
    public partial class BarangayClearancePage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        public BarangayClearancePage()
        {
            InitializeComponent();
        }
        [Obsolete]
        private async void OnGenerateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResidentNameEntry.Text) ||
                string.IsNullOrWhiteSpace(ResidentAddressEntry.Text) ||
                string.IsNullOrWhiteSpace(PurposeEntry.Text) ||
                string.IsNullOrWhiteSpace(ClearanceNumberEntry.Text) ||
                string.IsNullOrWhiteSpace(IssuedByEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please fill in all fields.", "OK");
                return;
            }
            try
            {
                bool hasBlotterRecord = await CheckForBlotterRecord(ResidentNameEntry.Text);
                if (hasBlotterRecord)
                {
                    await DisplayAlert("Restriction", "This resident has a blotter record and cannot be issued a Barangay Clearance.", "OK");
                    return;
                }
                PdfDocument document = new PdfDocument();
                document.Info.Title = $"Barangay Clearance for {ResidentNameEntry.Text}";
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont headerFont = new XFont("Times New Roman", 16, XFontStyleEx.Bold);
                XFont subHeaderFont = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
                XFont contentFont = new XFont("Times New Roman", 12, XFontStyleEx.Regular);
                double yPosition = 40;
                gfx.DrawString("Republic of the Philippines", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, 0), XStringFormats.TopCenter);
                yPosition += 25;
                gfx.DrawString("Province of Pangasinan", subHeaderFont, XBrushes.Black, new XRect(0, yPosition, page.Width, 0), XStringFormats.TopCenter);
                yPosition += 20;
                gfx.DrawString("Municipality of Sual", subHeaderFont, XBrushes.Black, new XRect(0, yPosition, page.Width, 0), XStringFormats.TopCenter);
                yPosition += 20;
                gfx.DrawString("Barangay Seselangen", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, 0), XStringFormats.TopCenter);
                yPosition += 40;
                gfx.DrawString("Barangay Clearance", new XFont("Times New Roman", 18, XFontStyleEx.Bold), XBrushes.Black, new XRect(0, yPosition, page.Width, 0), XStringFormats.TopCenter);
                yPosition += 40;
                gfx.DrawString($"Clearance Number: {ClearanceNumberEntry.Text}", contentFont, XBrushes.Black, new XRect(40, yPosition, page.Width, 0), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString($"Date Issued: {IssuedDatePicker.Date:MM/dd/yyyy}", contentFont, XBrushes.Black, new XRect(40, yPosition, page.Width, 0), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString("To Whom It May Concern:", contentFont, XBrushes.Black, new XRect(40, yPosition, page.Width, 0), XStringFormats.TopLeft);
                yPosition += 40;
                string clearanceText = $"This is to certify that {ResidentNameEntry.Text}, a resident of {ResidentAddressEntry.Text}, is known to be a person of good moral character and has no derogatory record on file. This clearance is issued upon the request of {ResidentNameEntry.Text} for the purpose of {PurposeEntry.Text}.";
                XRect textArea = new XRect(40, yPosition, page.Width - 80, page.Height - 100);
                DrawStringWithWordWrap(gfx, clearanceText, contentFont, XBrushes.Black, textArea, 15);
                yPosition += 120;
                gfx.DrawString($"Issued by: {IssuedByEntry.Text}", contentFont, XBrushes.Black, new XRect(40, yPosition, page.Width, 0), XStringFormats.TopLeft);
                yPosition += 40;
                gfx.DrawString("______________________", contentFont, XBrushes.Black, new XRect(page.Width - 200, yPosition, 160, 0), XStringFormats.TopCenter);
                yPosition += 20;
                gfx.DrawString("Signature over Printed Name", contentFont, XBrushes.Black, new XRect(page.Width - 200, yPosition, 160, 0), XStringFormats.TopCenter);
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
                string fileName = $"BarangayClearance_{ResidentNameEntry.Text}.pdf";
                string pdfPath = Path.Combine(folderPath, fileName);
                document.Save(pdfPath);
                byte[] pdfData = File.ReadAllBytes(pdfPath);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Save in BarangayClearance table
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO BarangayClearance (ResidentName, ClearancePDF) VALUES (@ResidentName, @ClearancePDF)", connection))
                    {
                        cmd.Parameters.AddWithValue("@ResidentName", ResidentNameEntry.Text);
                        cmd.Parameters.AddWithValue("@ClearancePDF", pdfData);
                        cmd.ExecuteNonQuery();
                    }
                    // Save in DocumentPayments table for payment processing
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO DocumentPayments (DocumentName, PaymentAmount, DueDate, Status) VALUES (@DocumentName, @PaymentAmount, @DueDate, @Status)", connection))
                    {
                        cmd.Parameters.AddWithValue("@DocumentName", $"Barangay Clearance - {ResidentNameEntry.Text}");
                        cmd.Parameters.AddWithValue("@PaymentAmount", 500.0); // Set appropriate payment amount if needed
                        cmd.Parameters.AddWithValue("@DueDate", DateTime.Now.AddDays(30)); // Set appropriate due date if needed
                        cmd.Parameters.AddWithValue("@Status", "Pending");
                        cmd.ExecuteNonQuery();
                    }
                }
                await DisplayAlert("Success", "Barangay Clearance PDF generated and saved to the database successfully!", "OK");
                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Created {fileName}.");
                Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                await Navigation.PopToRootAsync();
            }
        }
        private async Task<bool> CheckForBlotterRecord(string residentName)
        {
            using (SqlConnection dbConnection = new SqlConnection(connectionString))
            {
                string blotterQuery = @"SELECT COUNT(*) FROM BlotterReports WHERE LOWER(CAST(PartiesInvolved AS VARCHAR(MAX))) LIKE '%' + LOWER(@ResidentName) + '%'";
                int count = await dbConnection.ExecuteScalarAsync<int>(blotterQuery, new { ResidentName = residentName });
                return count > 0;
            }
        }
        private void DrawStringWithWordWrap(XGraphics gfx, string text, XFont font, XBrush brush, XRect rect, double lineHeight)
        {
            var words = text.Split(' ');
            var currentLine = string.Empty;
            double yOffset = rect.Top;
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testSize = gfx.MeasureString(testLine, font);
                if (testSize.Width > rect.Width)
                {
                    gfx.DrawString(currentLine, font, brush, new XRect(rect.Left, yOffset, rect.Width, lineHeight), XStringFormats.TopLeft);
                    currentLine = word;
                    yOffset += lineHeight;
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
            {
                gfx.DrawString(currentLine, font, brush, new XRect(rect.Left, yOffset, rect.Width, lineHeight), XStringFormats.TopLeft);
            }
        }
    }
}