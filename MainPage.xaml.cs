using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Digests;
using System.Text;

namespace CommUnity_Hub
{
    public partial class MainPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private bool isPasswordVisible = false;
        public static UserViewModel UserViewModel { get; private set; } = new UserViewModel();
        public static int LoggedInUserId { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasBackButton(this, false);
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsEnabled = false });
        }

        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            passwordEntry.IsPassword = !isPasswordVisible;
            togglePasswordButton.Source = isPasswordVisible ? "eye_open.png" : "eye_closed.png";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                string username = usernameEntry.Text;
                string password = passwordEntry.Text;

                if (await ValidateUser(username, password))
                {
                    await DisplayAlert("Login", "Login Successful", "OK");

                    // Clear input fields
                    usernameEntry.Text = string.Empty;
                    passwordEntry.Text = string.Empty;

                    // Log the successful login activity
                    await ActivityLog.LogActivity(LoggedInUserId, $"{username} logged in successfully");

                    // Load user data for the logged-in user
                    if (LoggedInUserId > 0)
                    {
                        await UserViewModel.LoadUserData(LoggedInUserId);
                    }

                    // Navigate to the main dashboard page
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    await DisplayAlert("Login", "Invalid username or password.", "OK");
                    passwordEntry.Text = string.Empty;
                }
            }
            catch (Exception ex) 
            {
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ValidateUser(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string hashedPassword = HashPassword(password);

                string query = "SELECT UserID FROM Users WHERE Username = @Username AND Password = @Password";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", hashedPassword);

                try
                {
                    await connection.OpenAsync();
                    object? result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        LoggedInUserId = (int)result;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
                    return false;
                }
            }
        }

        private string HashPassword(string password)
        {
            var sha3 = new Sha3Digest(256);
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            sha3.BlockUpdate(inputBytes, 0, inputBytes.Length);

            byte[] hashBytes = new byte[sha3.GetDigestSize()];
            sha3.DoFinal(hashBytes, 0);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
