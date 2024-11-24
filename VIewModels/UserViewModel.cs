using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommUnity_Hub
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private string _name;
        private ImageSource _profileImage;

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public ImageSource ProfileImage
        {
            get
            {
                return _profileImage;
            }

            set
            {
                _profileImage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadUserData(int userId)
        {
            try
            {
                using (var connection = new SqlConnection("Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;"))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Name, ProfileImage FROM Users WHERE UserID = @UserID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Name = reader["Name"].ToString();

                                if (reader["ProfileImage"] != DBNull.Value)
                                {
                                    var imageBytes = (byte[])reader["ProfileImage"];
                                    ProfileImage = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                                }
                                else
                                {
                                    // Load default image if no profile image is available
                                    ProfileImage = ImageSource.FromFile("user_icon.png");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed (e.g., log error)
                Console.WriteLine($"Error loading user data: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
