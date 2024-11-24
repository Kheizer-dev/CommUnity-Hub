using System.Diagnostics;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class CertificateOfResidencyPage : ContentPage
    {
        private Resident _resident;

        public CertificateOfResidencyPage(Resident resident)
        {
            InitializeComponent();
            _resident = resident;
            BindResidentData();
        }

        private void BindResidentData()
        {
            ResidentNameEntry.Text = _resident.GetFullName();
            CivilStatusEntry.Text = _resident.CivilStatus;
            NationalityEntry.Text = _resident.Nationality;
            AddressEntry.Text = _resident.Address;
        }

        [Obsolete]
        private async void OnGenerateClicked(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(ResidentNameEntry.Text) ||
                string.IsNullOrWhiteSpace(CivilStatusEntry.Text) ||
                string.IsNullOrWhiteSpace(NationalityEntry.Text) ||
                string.IsNullOrWhiteSpace(AddressEntry.Text) ||
                string.IsNullOrWhiteSpace(DurationEntry.Text) ||
                string.IsNullOrWhiteSpace(PurposeEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please fill in all fields.", "OK");
                return;
            }

            try
            {
                // Gather data from the form fields
                string residentName = ResidentNameEntry.Text;
                string civilStatus = CivilStatusEntry.Text;
                string nationality = NationalityEntry.Text;
                string address = AddressEntry.Text;
                string duration = DurationEntry.Text;
                string purpose = PurposeEntry.Text;

                // Create a new PDF document
                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                XGraphics graphics = XGraphics.FromPdfPage(page);

                // Define fonts
                XFont headerFont = new XFont("Times New Roman", 16, XFontStyleEx.Bold);
                XFont contentFont = new XFont("Times New Roman", 12, XFontStyleEx.Regular);

                // Define positions
                double yPosition = 20;

                // Draw Header
                graphics.DrawString("Republic of the Philippines", headerFont, XBrushes.Black,
                    new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                yPosition += 25;

                graphics.DrawString("Province of Pangasinan", headerFont, XBrushes.Black,
                    new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                yPosition += 20;

                graphics.DrawString("Municipality of Sual", headerFont, XBrushes.Black,
                    new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                yPosition += 20;

                graphics.DrawString("Barangay Seselangen", headerFont, XBrushes.Black,
                    new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                yPosition += 40;

                graphics.DrawString("Certificate of Residency", new XFont("Times New Roman", 18, XFontStyleEx.Bold),
                    XBrushes.Black, new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                yPosition += 40;

                // Certificate details - Centered
                string[] details =
                {
                    $"This is to certify that {residentName},",
                    $"a {civilStatus} Filipino citizen, residing at {address},",
                    $"has been a resident of this barangay for {duration}.",
                    $"This certification is issued upon the request of {residentName} for",
                    $"the purpose of {purpose}."
                };

                foreach (var detail in details)
                {
                    graphics.DrawString(detail, contentFont, XBrushes.Black,
                        new XRect(0, yPosition, page.Width, page.Height), XStringFormats.TopCenter);
                    yPosition += 20;
                }

                yPosition += 20; // Add extra space before the footer

                // Footer with date and signature - Centered
                graphics.DrawString($"Issued on {DateTime.Now:MMMM dd, yyyy}.", contentFont, XBrushes.Black,
                    new XRect(60, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 60;

                graphics.DrawString("________________________", contentFont, XBrushes.Black,
                    new XRect(page.Width - 200, yPosition, 160, 0), XStringFormats.TopCenter);
                yPosition += 20;

                graphics.DrawString("Barangay Captain", contentFont, XBrushes.Black,
                    new XRect(page.Width - 200, yPosition, 160, 0), XStringFormats.TopCenter);

                // Save the document
                string fileName = $"CertificateOfResidency_{residentName}.pdf";
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
                string filePath = Path.Combine(folderPath, fileName);
                document.Save(filePath);

                byte[] pdfBytes = File.ReadAllBytes(filePath);

                // Save the PDF byte array to SQL database
                string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("INSERT INTO CertificateOfResidency (ResidentName, ResidencyPDF) VALUES (@ResidentName, @ResidencyPDF)", connection))
                    {
                        command.Parameters.AddWithValue("@ResidentName", residentName);
                        command.Parameters.AddWithValue("@ResidencyPDF", pdfBytes);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await DisplayAlert("Success", "Certificate of Residency generated and saved to the database!", "OK");
                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Created {fileName}.");
                // Optionally, open the PDF document
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });

                await Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
