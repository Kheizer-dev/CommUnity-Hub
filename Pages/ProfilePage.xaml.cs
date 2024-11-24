using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class ProfilePage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private ProfileViewModel _viewModel;

        public ProfilePage()
        {
            InitializeComponent();
            _viewModel = new ProfileViewModel();
            BindingContext = _viewModel; // Set the BindingContext to the ViewModel
            LoadUserData();
        }

        private async Task LoadUserData()
        {
            try
            {
                int loggedInUserId = MainPage.LoggedInUserId;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd = new SqlCommand("SELECT * FROM Users WHERE UserID = @UserID", conn);
                    cmd.Parameters.AddWithValue("@UserID", loggedInUserId);

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        // Populate the ViewModel properties
                        _viewModel.UserId = (int)reader["UserID"];
                        _viewModel.Name = reader["Name"].ToString();
                        _viewModel.Username = reader["Username"].ToString();
                        _viewModel.DateOfBirth = (DateTime)reader["DOB"];
                        _viewModel.Email = reader["Email"].ToString();
                        _viewModel.Address = reader["Address"].ToString();
                        _viewModel.Phone = reader["Phone"].ToString();
                        _viewModel.ProfileImage = reader["ProfileImage"] as byte[];
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnUploadImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Please select a profile image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using (var stream = await result.OpenReadAsync())
                    {
                        byte[] imageData;
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            imageData = memoryStream.ToArray();
                        }
                        _viewModel.ProfileImage = imageData; // Update ViewModel
                    }
                    await ActivityLog.LogActivity(_viewModel.UserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Uploaded a new profile image.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnSaveChangesClicked(object sender, EventArgs e)
        {
            try
            {
                // Save data to the database
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Users SET Name = @Name, Username = @Username, DOB = @DateOfBirth, Email = @Email, " +
                        "Address = @Address, Phone = @Phone, ProfileImage = @ProfileImage WHERE UserID = @UserID", conn);

                    cmd.Parameters.AddWithValue("@Name", _viewModel.Name);
                    cmd.Parameters.AddWithValue("@Username", _viewModel.Username);
                    cmd.Parameters.AddWithValue("@DateOfBirth", _viewModel.DateOfBirth);
                    cmd.Parameters.AddWithValue("@Email", _viewModel.Email);
                    cmd.Parameters.AddWithValue("@Address", _viewModel.Address);
                    cmd.Parameters.AddWithValue("@Phone", _viewModel.Phone);
                    cmd.Parameters.AddWithValue("@ProfileImage", _viewModel.ProfileImage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserID", _viewModel.UserId);

                    await cmd.ExecuteNonQueryAsync();
                    await DisplayAlert("Success", "Profile updated successfully.", "OK");

                    await ActivityLog.LogActivity(_viewModel.UserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Updated profile information.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Override OnAppearing to reload user data when the page appears
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserData(); // Reload data when the page is shown
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new AddUserPage());
        }
    }
}
