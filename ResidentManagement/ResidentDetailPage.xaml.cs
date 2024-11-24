using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class ResidentDetailPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private Resident _resident;

        // Define a list of common nationalities
        private readonly List<string> _nationalities = new List<string>
        {
            "Filipino", "American", "Japanese", "Chinese", "Australian", "Canadian", "British", "French"
            // Add more as needed
        };

        public ResidentDetailPage(Resident resident)
        {
            InitializeComponent();
            _resident = resident;
            BindingContext = resident;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // Check if Nationality matches a predefined nationality, else prompt the user
            if (!_nationalities.Contains(_resident.Nationality))
            {
                bool confirm = await DisplayAlert("Confirm Nationality", $"The nationality '{_resident.Nationality}' is not in the common list. Do you want to save it anyway?", "Yes", "No");
                if (!confirm)
                {
                    return; // Stop save if user does not confirm
                }
            }

            // Save resident details to database as in your existing code
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Residents SET FirstName = @FirstName, MiddleName = @MiddleName, LastName = @LastName, 
                             Address = @Address, ContactNumber = @ContactNumber, DateOfBirth = @DateOfBirth, 
                             Gender = @Gender, CivilStatus = @CivilStatus, Nationality = @Nationality, 
                             Occupation = @Occupation, EducationalAttainment = @EducationalAttainment
                             WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", _resident.FirstName ?? string.Empty);
                        command.Parameters.AddWithValue("@MiddleName", _resident.MiddleName ?? string.Empty);
                        command.Parameters.AddWithValue("@LastName", _resident.LastName ?? string.Empty);
                        command.Parameters.AddWithValue("@Address", _resident.Address ?? string.Empty);
                        command.Parameters.AddWithValue("@ContactNumber", _resident.ContactNumber ?? string.Empty);
                        command.Parameters.AddWithValue("@DateOfBirth", _resident.DateOfBirth);
                        command.Parameters.AddWithValue("@Gender", _resident.Gender ?? string.Empty);
                        command.Parameters.AddWithValue("@CivilStatus", _resident.CivilStatus ?? string.Empty);
                        command.Parameters.AddWithValue("@Nationality", _resident.Nationality ?? string.Empty);
                        command.Parameters.AddWithValue("@Occupation", _resident.Occupation ?? string.Empty);
                        command.Parameters.AddWithValue("@EducationalAttainment", _resident.EducationalAttainment ?? string.Empty);
                        command.Parameters.AddWithValue("@Id", _resident.Id);

                        connection.Open();
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            await DisplayAlert("Success", "Resident details updated successfully.", "OK");

                            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Updated details for resident: {_resident.FirstName} {_resident.LastName}");

                            await Navigation.PopAsync();
                        }
                        else
                        {
                            await DisplayAlert("Error", "Failed to update resident details.", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while updating resident: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to delete this resident?", "Yes", "No");
            if (confirm)
            {
                // Delete resident from the database
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Residents WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", _resident.Id);

                            connection.Open();
                            int rowsAffected = await command.ExecuteNonQueryAsync();

                            if (rowsAffected > 0)
                            {
                                await DisplayAlert("Success", "Resident deleted successfully.", "OK");

                                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Deleted resident: {_resident.FirstName} {_resident.LastName}");

                                await Navigation.PopAsync();
                            }
                            else
                            {
                                await DisplayAlert("Error", "Failed to delete resident.", "OK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An error occurred while deleting resident: {ex.Message}", "OK");
                }
            }
        }
    }
}
