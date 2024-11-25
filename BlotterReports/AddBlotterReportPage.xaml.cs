using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Storage;

namespace CommUnity_Hub
{
    public partial class AddBlotterReportPage : ContentPage
    {
        public List<string> IncidentTypes { get; set; }
        BlotterReport report = new BlotterReport();
        private ObservableCollection<BlotterReport> _blotterReports;
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public AddBlotterReportPage(ObservableCollection<BlotterReport> blotterReports)
        {
            InitializeComponent();
            InitializeIncidentTypes();
            BindingContext = this;

            // Set the DatePicker to today's date
            IncidentDatePicker.Date = DateTime.Now;
            _blotterReports = blotterReports;
        }

        private string GenerateCaseID()
        {
            string newCaseID = "BR-001"; // Default case ID

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT TOP 1 CaseID FROM BlotterReports ORDER BY CaseID DESC", connection);
                var result = command.ExecuteScalar();

                // Check if result is null or DBNull.Value
                if (result != null && result != DBNull.Value)
                {
                    string lastCaseID = (string)result;
                    int lastID = int.Parse(lastCaseID.Split('-')[1]);
                    newCaseID = $"BR-{lastID + 1:D3}";
                }
                // If no reports exist, the default "BR-001" will be used
            }

            return newCaseID;
        }


        private void InitializeIncidentTypes()
        {
            // Example incident types (replace with actual data)
            IncidentTypes = new List<string>
            {
                "Theft",
                "Vandalism",
                "Assault",
                "Disturbance",
                "Missing Person"
            };
        }
        
        private async void OnIncidentTypeSelected(object sender, EventArgs e)
        {
            // Handle selection change if needed
        }

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select an Image or Video",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>()
                {
                    { DevicePlatform.iOS, new[] { "public.image", "public.video" } }, // iOS specific types
                    { DevicePlatform.Android, new[] { "image/*", "video/*" } },         // Android specific types
                    { DevicePlatform.WinUI, new[] { ".jpg", ".png", ".jpeg", ".mp4", ".avi", ".mov" } }  // WinUI specific types
                })
            });

            if (result != null)
            {
                SelectedFileLabel.Text = result.FileName;

                // Handling image file selection
                if (result.FileName.EndsWith(".jpg") || result.FileName.EndsWith(".png") || result.FileName.EndsWith(".jpeg"))
                {
                    report.MediaType = "Image";
                    report.MediaData = await File.ReadAllBytesAsync(result.FullPath);  // Store image as binary data
                    report.MediaFilePath = null;  // No file path needed for images
                }
                // Handling video file selection
                else if (result.FileName.EndsWith(".mp4") || result.FileName.EndsWith(".avi") || result.FileName.EndsWith(".mov"))
                {
                    report.MediaType = "Video";
                    report.MediaData = null;  // No binary data needed for videos
                    report.MediaFilePath = result.FullPath;  // Store the file path for video
                }
            }
        }

        private async void OnSubmitReportClicked(object sender, EventArgs e)
        {
            // Gather report details
            var incidentDate = IncidentDatePicker.Date;
            var selectedIncidentType = IncidentTypePicker.SelectedItem?.ToString();
            var description = DescriptionEditor.Text;
            var location = LocationEntry.Text;
            var partiesInvolved = PartiesEditor.Text;
            var evidence = EvidenceEditor.Text;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(selectedIncidentType) ||
                string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(location))
            {
                await DisplayAlert("Error", "Please fill in all required fields.", "OK");
                return;
            }

            // Create a new blotter report
            var newReport = new BlotterReport
            {
                CaseID = GenerateCaseID(),
                IncidentDetails = description,
                DateReported = incidentDate,
                Location = location,
                PartiesInvolved = partiesInvolved,
                Evidence = evidence,
                MediaType = report.MediaType,
                MediaData = report.MediaData,
                MediaFilePath = report.MediaFilePath,
                DateCreated = DateTime.Now
            };

            // Add to local collection and database
            _blotterReports.Add(newReport);
            InsertReportIntoDatabase(newReport);

            await DisplayAlert("Success", "Blotter report submitted successfully.", "OK");

            // Optionally clear the form or navigate back
            IncidentTypePicker.SelectedIndex = -1;
            DescriptionEditor.Text = string.Empty;
            LocationEntry.Text = string.Empty;
            PartiesEditor.Text = string.Empty;
            EvidenceEditor.Text = string.Empty;
            IncidentDatePicker.Date = DateTime.Now; // Reset the date to today
            await Navigation.PopModalAsync();
        }


        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        // Insert new blotter report into the SQL database
        private void InsertReportIntoDatabase(BlotterReport report)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO BlotterReports (CaseID, IncidentDetails, DateReported, Location, PartiesInvolved, Evidence, MediaType, MediaData, MediaFilePath, DateCreated) " +
                    "VALUES (@caseID, @incidentDetails, @dateReported, @location, @partiesInvolved, @evidence, @mediaType, @mediaData, @mediaFilePath, @dateCreated)",
                    connection);

                command.Parameters.AddWithValue("@caseID", report.CaseID);
                command.Parameters.AddWithValue("@incidentDetails", report.IncidentDetails);
                command.Parameters.AddWithValue("@dateReported", report.DateReported);
                command.Parameters.AddWithValue("@location", report.Location);
                command.Parameters.AddWithValue("@partiesInvolved", report.PartiesInvolved);
                command.Parameters.AddWithValue("@evidence", (object)report.Evidence ?? DBNull.Value);
                command.Parameters.AddWithValue("@mediaType", (object)report.MediaType ?? DBNull.Value);
                command.Parameters.AddWithValue("@mediaData", (object)report.MediaData ?? DBNull.Value);
                command.Parameters.AddWithValue("@mediaFilePath", (object)report.MediaFilePath ?? DBNull.Value);
                command.Parameters.AddWithValue("@dateCreated", report.DateCreated);

                command.ExecuteNonQuery();
            }
        }
    }
}
