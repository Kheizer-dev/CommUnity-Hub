using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class ResidentSelectionPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private List<Resident> _allResidents; // Store the original resident list

        public ResidentSelectionPage()
        {
            InitializeComponent();
            LoadResidents();
        }

        // Fetch residents from SQL database and bind them to CollectionView
        private async void LoadResidents()
        {
            try
            {
                _allResidents = await FetchResidentsFromDatabase();

                _allResidents = _allResidents.OrderBy(r => r.FirstName).ToList();

                ResidentCollectionView.ItemsSource = _allResidents; // Bind to the original list
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load residents: {ex.Message}", "OK");
            }
        }

        // Fetch residents from SQL database using Dapper
        private async Task<List<Resident>> FetchResidentsFromDatabase()
        {
            using (IDbConnection dbConnection = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT FirstName, MiddleName, LastName, Address, DateOfBirth, CivilStatus, Nationality, Occupation, EducationalAttainment FROM Residents";
                var residents = await dbConnection.QueryAsync<Resident>(sqlQuery);
                return residents.AsList();
            }
        }

        // Handle resident selection
        private async void OnResidentSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
            {
                Resident selectedResident = (Resident)e.CurrentSelection[0];

                // Check if resident has a blotter record
                bool hasBlotterRecord = await CheckForBlotterRecord(selectedResident);

                if (hasBlotterRecord)
                {
                    await DisplayAlert("Restriction", "This resident has a blotter record and cannot be issued a Certificate of Residency.", "OK");
                }
                else
                {
                    // Navigate to the CertificateOfResidencyPage, passing the selected resident
                    await Navigation.PushAsync(new CertificateOfResidencyPage(selectedResident));
                }

                // Deselect the item
                ResidentCollectionView.SelectedItem = null;
            }
        }

        // Method to check for a blotter record
        private async Task<bool> CheckForBlotterRecord(Resident resident)
        {
            using (IDbConnection dbConnection = new SqlConnection(connectionString))
            {
                string blotterQuery = @"
                    SELECT COUNT(*) 
                    FROM BlotterReports 
                    WHERE PartiesInvolved LIKE '%' + @FirstName + '%' 
                    AND PartiesInvolved LIKE '%' + @MiddleName + '%' 
                    AND PartiesInvolved LIKE '%' + @LastName + '%'";

                int count = await dbConnection.ExecuteScalarAsync<int>(blotterQuery, new
                {
                    resident.FirstName,
                    resident.MiddleName,
                    resident.LastName
                });

                return count > 0;
            }
        }


        // Filter residents based on search input
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string? searchText = e.NewTextValue?.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all residents if search box is empty
                ResidentCollectionView.ItemsSource = _allResidents;
            }
            else
            {
                // Filter residents
                var filteredResidents = _allResidents.Where(r =>
                    r.FirstName.ToLower().Contains(searchText) ||
                    r.LastName.ToLower().Contains(searchText) ||
                    r.Address.ToLower().Contains(searchText)).ToList();

                ResidentCollectionView.ItemsSource = filteredResidents;
            }
        }
    }
}
