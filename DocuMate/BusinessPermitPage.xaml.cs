using System.Diagnostics;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class BusinessPermitPage : ContentPage
    {
        public BusinessPermitPage()
        {
            InitializeComponent();
        }

        [Obsolete]
        private async void OnGenerateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BusinessNameEntry.Text) ||
                string.IsNullOrWhiteSpace(OwnerNameEntry.Text) ||
                string.IsNullOrWhiteSpace(BusinessTypeEntry.Text) ||
                string.IsNullOrWhiteSpace(BusinessAddressEntry.Text) ||
                string.IsNullOrWhiteSpace(TaxIDEntry.Text) ||
                string.IsNullOrWhiteSpace(ZoningEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please fill in all fields.", "OK");
                return;
            }

            string businessName = BusinessNameEntry.Text;
            string ownerName = OwnerNameEntry.Text;
            string businessType = BusinessTypeEntry.Text;
            string businessAddress = BusinessAddressEntry.Text;
            string taxId = TaxIDEntry.Text;
            string zoning = ZoningEntry.Text;
            string healthPermitInfo = HealthPermitEntry.Text;

            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics graphics = XGraphics.FromPdfPage(page);

            XFont headerFont = new XFont("Times New Roman", 16, XFontStyleEx.Bold);
            XFont subHeaderFont = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
            XFont contentFont = new XFont("Times New Roman", 12, XFontStyleEx.Regular);

            double yPosition = 20;

            graphics.DrawString("Republic of the Philippines", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
            yPosition += 25;
            graphics.DrawString("Province of Pangasinan", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
            yPosition += 25;
            graphics.DrawString("Municipality of Sual", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
            yPosition += 25;
            graphics.DrawString("Barangay Seselangen", headerFont, XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
            yPosition += 40;
            graphics.DrawString("Business Permit", new XFont("Times New Roman", 20, XFontStyleEx.Bold), XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
            yPosition += 40;

            graphics.DrawString("Business Name: " + businessName, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;
            graphics.DrawString("Owner Name: " + ownerName, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;
            graphics.DrawString("Business Type: " + businessType, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;
            graphics.DrawString("Business Address: " + businessAddress, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;
            graphics.DrawString("Tax ID: " + taxId, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;
            graphics.DrawString("Zoning: " + zoning, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            yPosition += 20;

            if (!string.IsNullOrWhiteSpace(healthPermitInfo))
            {
                graphics.DrawString("Health Permit Info: " + healthPermitInfo, contentFont, XBrushes.Black, new XRect(20, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
            }

            string fileName = $"BusinessPermit_{businessName}.pdf";
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
            string filePath = Path.Combine(folderPath, fileName);
            document.Save(filePath);

            byte[] pdfData = File.ReadAllBytes(filePath);

            string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Save in BusinessPermit table
                using (SqlCommand cmd = new SqlCommand("INSERT INTO BusinessPermit (BusinessName, PermitPDF) VALUES (@BusinessName, @PermitPDF)", connection))
                {
                    cmd.Parameters.AddWithValue("@BusinessName", businessName);
                    cmd.Parameters.AddWithValue("@PermitPDF", pdfData);
                    cmd.ExecuteNonQuery();
                }

                // Save in DocumentPayments table for payment tracking
                using (SqlCommand cmd = new SqlCommand("INSERT INTO DocumentPayments (DocumentName, PaymentAmount, DueDate, Status) VALUES (@DocumentName, @PaymentAmount, @DueDate, @Status)", connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentName", $"Business Permit - {businessName}");
                    cmd.Parameters.AddWithValue("@PaymentAmount", 1000.0); // Set payment amount as needed
                    cmd.Parameters.AddWithValue("@DueDate", DateTime.Now.AddDays(30)); // Set appropriate due date
                    cmd.Parameters.AddWithValue("@Status", "Pending");
                    cmd.ExecuteNonQuery();
                }
            }

            await DisplayAlert("Success", "Business Permit generated and saved to the database successfully!", "OK");
            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Created {fileName}.");
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            await Navigation.PopAsync();
        }
    }
}
