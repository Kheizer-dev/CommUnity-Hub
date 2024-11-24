using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class BlotterReportsPage : ContentPage
    {
        private ObservableCollection<BlotterReport> BlotterReports { get; set; }
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public BlotterReportsPage()
        {
            InitializeComponent();
            BlotterReports = new ObservableCollection<BlotterReport>();
            LoadBlotterReports();
            BlotterListView.ItemsSource = BlotterReports;
        }

        // Load blotter reports from the SQL Server database
        private void LoadBlotterReports()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT CaseID, IncidentDetails, DateReported, Location, PartiesInvolved, Evidence FROM BlotterReports", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        BlotterReports.Add(new BlotterReport
                        {
                            CaseID = reader.GetString(0),
                            IncidentDetails = reader.GetString(1),
                            DateReported = reader.GetDateTime(2),
                            Location = reader.GetString(3),
                            PartiesInvolved = reader.GetString(4),
                            Evidence = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = BlotterSearchBar.Text.ToLower();
            var filteredReports = new ObservableCollection<BlotterReport>();

            foreach (var report in BlotterReports)
            {
                if (report.CaseID.ToLower().Contains(searchText) ||
                    report.IncidentDetails.ToLower().Contains(searchText) ||
                    report.Location.ToLower().Contains(searchText) ||
                    report.PartiesInvolved.ToLower().Contains(searchText))
                {
                    filteredReports.Add(report);
                }
            }

            // Update the CollectionView's ItemsSource with the filtered results
            BlotterListView.ItemsSource = filteredReports;
        }

        // Add new blotter report
        private async void OnAddNewBlotterReportClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new AddBlotterReportPage(BlotterReports));
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            // Clear the existing reports
            BlotterReports.Clear();
            // Load the reports again
            LoadBlotterReports();
            // Update the ItemsSource to reflect the changes 
            BlotterListView.ItemsSource = BlotterReports;
        }

        // Handle report selection with print and delete option
        [Obsolete]
        private async void OnReportSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
            {
                var selectedReport = (BlotterReport)e.CurrentSelection[0];
                string action = await DisplayActionSheet("Choose an action", "Cancel", null, "Print", "Delete");

                if (action == "Print")
                {
                    BlotterReport.GenerateBlotterReportPdf(selectedReport);
                    await DisplayAlert("Success", "Blotter report printed successfully.", "OK");
                    await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Printed {selectedReport.CaseID}.");
                }
                else if (action == "Delete")
                {
                    bool confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete report: {selectedReport.CaseID}?", "Yes", "No");
                    if (confirm)
                    {
                        BlotterReports.Remove(selectedReport);
                        DeleteReportFromDatabase(selectedReport.CaseID); // Remove from database
                        DeleteReportFile(selectedReport.CaseID);         // Delete PDF file

                        await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Deleted {selectedReport.CaseID}.");
                    }
                }

                // Clear selection
                BlotterListView.SelectedItem = null;
            }
        }

        // Delete blotter report from the database
        private void DeleteReportFromDatabase(string caseId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM BlotterReports WHERE CaseID = @caseID", connection);
                    command.Parameters.AddWithValue("@caseID", caseId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"An error occurred while deleting the report: {ex.Message}", "OK");
            }
        }

        // Delete the report PDF file from the system
        private void DeleteReportFile(string caseId)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Blotter Reports");
            string filePath = Path.Combine(folderPath, $"BlotterReport_{caseId}.pdf");

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    DisplayAlert("Error", $"An error occurred while deleting the file: {ex.Message}", "OK");
                }
            }
        }

    }
}
