using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class ResidentManagementPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private ObservableCollection<Resident> allResidents = new ObservableCollection<Resident>();
        private ObservableCollection<Resident> filteredResidents = new ObservableCollection<Resident>();

        private int currentPage = 1; // Current page
        private int entriesPerPage = 5; // Default entries per page

        public ResidentManagementPage()
        {
            InitializeComponent();
            EntriesPerPagePicker.SelectedIndex = 1; // Default to 10 entries per page
            Task task = LoadResidents(); // Initial load of residents
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadResidents(); // Refresh residents list when the page appears
        }

        // Load residents from the database
        private async Task LoadResidents()
        {
            List<Resident> residents = new List<Resident>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT Id, FirstName, MiddleName, LastName, Address, ContactNumber, DateOfBirth, Gender, CivilStatus, Nationality, Religion, Occupation, EducationalAttainment FROM Residents";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var resident = new Resident
                                {
                                    Id = reader["Id"]?.ToString() ?? string.Empty,
                                    FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                    MiddleName = reader["MiddleName"]?.ToString() ?? string.Empty,
                                    LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                    Address = reader["Address"]?.ToString() ?? string.Empty,
                                    ContactNumber = reader["ContactNumber"]?.ToString() ?? string.Empty,
                                    DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]) : DateTime.MinValue,
                                    Gender = reader["Gender"]?.ToString() ?? string.Empty,
                                    CivilStatus = reader["CivilStatus"]?.ToString() ?? string.Empty,
                                    Nationality = reader["Nationality"]?.ToString() ?? string.Empty,
                                    Religion = reader["Religion"]?.ToString() ?? string.Empty,
                                    Occupation = reader["Occupation"]?.ToString() ?? string.Empty,
                                    EducationalAttainment = reader["EducationalAttainment"]?.ToString() ?? string.Empty
                                };

                                // Check if the resident has a blotter record using the full name
                                resident.HasBlotter = await CheckBlotterRecord(resident.FirstName, resident.MiddleName, resident.LastName);
                                residents.Add(resident);
                            }
                        }
                    }
                }

                allResidents = new ObservableCollection<Resident>(residents);
                currentPage = 1; // Reset to the first page
                UpdatePage(); // Display the first page of residents
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while loading residents: {ex.Message}", "OK");
            }
        }

        // Helper method to check if a resident has a blotter record
        private async Task<bool> CheckBlotterRecord(string firstName, string middleName, string lastName)
        {
            string blotterQuery = "SELECT COUNT(*) FROM BlotterReports WHERE PartiesInvolved LIKE @FullName";

            // Construct the full name to search
            string fullName = $"{firstName} {middleName} {lastName}".Trim();

            using (SqlConnection blotterConnection = new SqlConnection(connectionString))
            {
                await blotterConnection.OpenAsync();

                using (SqlCommand blotterCommand = new SqlCommand(blotterQuery, blotterConnection))
                {
                    blotterCommand.Parameters.AddWithValue("@FullName", "%" + fullName + "%");
                    int count = (int)await blotterCommand.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }




        // Update the residents displayed based on the current page and entries per page
        private void UpdatePage()
        {
            int start = (currentPage - 1) * entriesPerPage;
            int end = start + entriesPerPage;

            var paginatedResidents = allResidents.Skip(start).Take(entriesPerPage);
            filteredResidents = new ObservableCollection<Resident>(paginatedResidents);
            ResidentsCollectionView.ItemsSource = filteredResidents;

            PageLabel.Text = $"Page {currentPage}";
        }

        // Event handler for entries per page change
        private void OnEntriesPerPageChanged(object sender, EventArgs e)
        {
            entriesPerPage = int.Parse(EntriesPerPagePicker.SelectedItem.ToString());
            currentPage = 1; // Reset to the first page
            UpdatePage();
        }

        // Next page button clicked
        private void OnNextPageClicked(object sender, EventArgs e)
        {
            if ((currentPage * entriesPerPage) < allResidents.Count)
            {
                currentPage++;
                UpdatePage();
            }
        }

        // Previous page button clicked
        private void OnPreviousPageClicked(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                UpdatePage();
            }
        }

        private void OnSortOrderChanged(object sender, EventArgs e)
        {
            if (SortOrderPicker.SelectedIndex == -1)
                return;

            // Sort based on selected criteria
            switch (SortOrderPicker.SelectedItem.ToString())
            {
                case "Last Name (A-Z)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderBy(r => r.LastName));
                    break;
                case "Last Name (Z-A)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderByDescending(r => r.LastName));
                    break;
                case "First Name (A-Z)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderBy(r => r.FirstName));
                    break;
                case "First Name (Z-A)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderByDescending(r => r.FirstName));
                    break;
                case "Resident ID (Ascending)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderBy(r => r.Id));
                    break;
                case "Resident ID (Descending)":
                    allResidents = new ObservableCollection<Resident>(allResidents.OrderByDescending(r => r.Id));
                    break;
            }

            // Reset to page 1 and refresh the display
            currentPage = 1;
            UpdatePage();
        }


        // Navigate to AddResidentPage
        private async void OnAddResidentClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddResidentPage());
        }

        // Refresh Button Clicked
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadResidents(); // Reload residents from database
        }

        // SearchBar TextChanged event handler
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredResidents = new ObservableCollection<Resident>(allResidents);
            }
            else
            {
                filteredResidents = new ObservableCollection<Resident>(
                    allResidents.Where(r =>
                        (r.FirstName?.ToLower().Contains(searchText) ?? false) ||
                        (r.MiddleName?.ToLower().Contains(searchText) ?? false) ||
                        (r.LastName?.ToLower().Contains(searchText) ?? false) ||
                        (r.Address?.ToLower().Contains(searchText) ?? false) ||
                        (r.ContactNumber?.ToLower().Contains(searchText) ?? false) ||
                        (r.DateOfBirth.ToString("MM/dd/yyyy").Contains(searchText)) ||
                        (r.Id?.ToLower().Contains(searchText) ?? false)
                    )
                );
            }

            ResidentsCollectionView.ItemsSource = filteredResidents;
        }

        // Resident selected event handler
        private async void OnResidentSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
            {
                Resident selectedResident = (Resident)e.CurrentSelection[0];
                await Navigation.PushAsync(new ResidentDetailPage(selectedResident));
                ResidentsCollectionView.SelectedItem = null;
            }
        }
    }
}
