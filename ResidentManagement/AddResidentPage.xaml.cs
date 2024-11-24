using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class AddResidentPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public AddResidentPage()
        {
            InitializeComponent();
        }

        // Generate the next Resident ID
        private string GenerateResidentId()
        {
            int nextId = 1;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT MAX(CAST(SUBSTRING(Id, 4, LEN(Id) - 3) AS INT)) AS MaxId FROM Residents";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        nextId = Convert.ToInt32(result) + 1;
                    }
                }
            }

            return $"R-{nextId:D6}"; // Format as R-000001
        }

        private void OnNationalityPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (NationalityPicker.SelectedItem?.ToString() == "Others")
            {
                OtherNationalityEntry.IsVisible = true; // Show the Entry for entering nationality
            }
            else
            {
                OtherNationalityEntry.IsVisible = false; // Hide the Entry if "Others" is not selected
            }
        }

        // Add Resident to Database
        private async void OnAddClicked(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                GenderPicker.SelectedIndex == -1 ||
                CivilStatusPicker.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(AddressEntry.Text) ||
                string.IsNullOrWhiteSpace(ContactNumberEntry.Text) ||
                GenderPicker.SelectedIndex == -1 ||
                CivilStatusPicker.SelectedIndex == -1 ||
                EducationPicker.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(OccupationEntry.Text) ||
                NationalityPicker.SelectedIndex == -1)
            {
                await DisplayAlert("Error", "Please fill in all required fields.", "OK");
                return;
            }

            string residentId = GenerateResidentId(); // Generate the Resident ID
            string firstName = FirstNameEntry.Text.Trim();
            string middleName = MiddleNameEntry.Text?.Trim() ?? "";
            string lastName = LastNameEntry.Text.Trim();
            DateTime dateOfBirth = DateOfBirthPicker.Date;
            string? gender = GenderPicker.SelectedItem.ToString();
            string? civilStatus = CivilStatusPicker.SelectedItem.ToString();

            // Get nationality from OtherNationalityEntry if "Others" is selected
            string? nationality = NationalityPicker.SelectedItem?.ToString() == "Others"
                ? OtherNationalityEntry.Text?.Trim() ?? "Others"
                : NationalityPicker.SelectedItem.ToString();

            string religion = ReligionEntry.Text?.Trim() ?? "";
            string occupation = OccupationEntry.Text.Trim();
            string? educationalAttainment = EducationPicker.SelectedItem.ToString();
            string address = AddressEntry.Text.Trim();
            string contactNumber = ContactNumberEntry.Text.Trim();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Residents 
                                    (Id, FirstName, MiddleName, LastName, DateOfBirth, Gender, CivilStatus, Nationality, Religion, 
                                     Occupation, EducationalAttainment, Address, ContactNumber) 
                                     VALUES 
                                    (@Id, @FirstName, @MiddleName, @LastName, @DateOfBirth, @Gender, @CivilStatus, 
                                     @Nationality, @Religion, @Occupation, @EducationalAttainment, @Address, @ContactNumber)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", residentId); // Use the generated Resident ID
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@MiddleName", middleName);
                        command.Parameters.AddWithValue("@LastName", lastName);
                        command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                        command.Parameters.AddWithValue("@Gender", gender);
                        command.Parameters.AddWithValue("@CivilStatus", civilStatus);
                        command.Parameters.AddWithValue("@Nationality", nationality); // Use the selected or entered nationality
                        command.Parameters.AddWithValue("@Religion", string.IsNullOrEmpty(religion) ? (object)DBNull.Value : religion);
                        command.Parameters.AddWithValue("@Occupation", occupation);
                        command.Parameters.AddWithValue("@EducationalAttainment", educationalAttainment);
                        command.Parameters.AddWithValue("@Address", address);
                        command.Parameters.AddWithValue("@ContactNumber", contactNumber);

                        connection.Open();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"Resident {firstName} {lastName} (ID: {residentId}) added successfully.");
                await DisplayAlert("Success", "Resident added successfully.", "OK");
                await Navigation.PopAsync(); // Go back to the Resident Management Page
            }
            catch (Exception ex)
            {
                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"Error adding resident: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
