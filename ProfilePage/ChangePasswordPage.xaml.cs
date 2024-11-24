using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Digests;
using System.Text;

namespace CommUnity_Hub;

public partial class ChangePasswordPage : ContentPage
{
    private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

    public ChangePasswordPage()
    {
        InitializeComponent();
        LoadUsers();
    }

    private async void LoadUsers()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand("SELECT UserID, Username FROM Users", conn);
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    UserPicker.Items.Add($"{reader["UserID"]}: {reader["Username"]}");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        try
        {
            if (UserPicker.SelectedIndex == -1 || string.IsNullOrEmpty(NewPasswordEntry.Text) || string.IsNullOrEmpty(ConfirmPasswordEntry.Text))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            // Hash the new password
            string hashedPassword = HashPassword(NewPasswordEntry.Text);

            int selectedUserId = int.Parse(UserPicker.SelectedItem.ToString().Split(':')[0]);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand("UPDATE Users SET Password = @Password WHERE UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@Password", hashedPassword);
                cmd.Parameters.AddWithValue("@UserID", selectedUserId);

                await cmd.ExecuteNonQueryAsync();
                await DisplayAlert("Success", "Password changed successfully.", "OK");

                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} changed the password for UserID {selectedUserId}.");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
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
}