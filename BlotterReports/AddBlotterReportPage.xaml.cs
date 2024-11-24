using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class AddBlotterReportPage : ContentPage
    {
        public List<string> IncidentTypes { get; set; }

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

                if (result != DBNull.Value)
                {
                    string lastCaseID = (string)result;
                    int lastID = int.Parse(lastCaseID.Split('-')[1]);
                    newCaseID = $"BR-{lastID + 1:D3}";
                }
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
                Evidence = evidence
            };

            // Add to local collection and database
            _blotterReports.Add(newReport);
            InsertReportIntoDatabase(newReport);

            await DisplayAlert("Success", "Blotter report submitted successfully.", "OK");

            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Added Blotter {newReport.CaseID}");

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
                var command = new SqlCommand("INSERT INTO BlotterReports (CaseID, IncidentDetails, DateReported, Location, PartiesInvolved, Evidence) VALUES (@caseID, @incidentDetails, @dateReported, @location, @partiesInvolved, @evidence)", connection);

                command.Parameters.AddWithValue("@caseID", report.CaseID);
                command.Parameters.AddWithValue("@incidentDetails", report.IncidentDetails);
                command.Parameters.AddWithValue("@dateReported", report.DateReported);
                command.Parameters.AddWithValue("@location", report.Location);
                command.Parameters.AddWithValue("@partiesInvolved", report.PartiesInvolved);
                command.Parameters.AddWithValue("@evidence", (object)report.Evidence ?? DBNull.Value); // Handle potential null value

                command.ExecuteNonQuery();
            }
        }
    }
}
