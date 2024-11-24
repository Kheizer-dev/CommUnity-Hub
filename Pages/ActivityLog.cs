using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public class ActivityLog
    {
        private static readonly string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public string? ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
        public string? LogType { get; set; }

        public static async Task LogActivity(int userId, string activityDescription)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO UserLogs (UserID, ActivityDescription, Timestamp) VALUES (@UserID, @ActivityDescription, @Timestamp)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ActivityDescription", activityDescription);
                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);

                try
                {
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    // Optionally, handle exceptions or log them
                    Console.WriteLine($"Error logging activity: {ex.Message}");
                }
            }
        }

        public static string GetUsername(int userId)
        {
            string username = string.Empty;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT Username FROM Users WHERE UserID = @UserID";
                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@UserID", userId);

                conn.Open();

                var result = command.ExecuteScalar();
                if (result!= null)
                {
                    username = result.ToString();
                }
            }

            return username;
        }
    }
}
