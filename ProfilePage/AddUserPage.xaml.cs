using System.Text;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Digests;

namespace CommUnity_Hub;
public partial class AddUserPage : ContentPage
{
    public AddUserPage()
    {
        InitializeComponent();
    }

    private async void OnAddButtonClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text;
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;
        string email = EmailEntry.Text;
        string address = AddressEntry.Text;
        string phone = PhoneEntry.Text;
        DateTime dob = DOBPicker.Date;

        // Hash the password using SHA-3
        string hashedPassword = HashPassword(password);

        string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        string insertQuery = @"
        INSERT INTO Users (Name, Username, Password, Email, Address, Phone, DOB) 
        VALUES (@Name, @Username, @Password, @Email, @Address, @Phone, @DOB)";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword); // Use hashed password
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@DOB", dob);

                    await command.ExecuteNonQueryAsync();
                }
            }

            await DisplayAlert("Success", "User added successfully!", "OK");

            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Added a new user: {username}");

            await Navigation.PopModalAsync();
        }
        catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
        {
            await DisplayAlert("Error", "The username already exists. Please choose another.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add user: {ex.Message}", "OK");
        }
    }

    private string HashPassword(string password)
    {
        // Create a new SHA3-256 instance
        var sha3 = new Sha3Digest(256); // For SHA3-256
        byte[] inputBytes = Encoding.UTF8.GetBytes(password);
        sha3.BlockUpdate(inputBytes, 0, inputBytes.Length);

        byte[] hashBytes = new byte[sha3.GetDigestSize()];
        sha3.DoFinal(hashBytes, 0);

        // Convert the byte array to a hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async void OnCancelButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
