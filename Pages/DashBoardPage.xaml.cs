using Microcharts;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Timers;
using Twilio.Types;
using Twilio;
using Timer = System.Timers.Timer;
using Twilio.Rest.Api.V2010.Account;

namespace CommUnity_Hub
{
    public partial class DashBoardPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        private string documentsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
        public ObservableCollection<ActivityLog> ActivityLogs { get; set; } = new ObservableCollection<ActivityLog>();
        public ObservableCollection<PaymentDocument> RecentTransactions { get; set; } = new ObservableCollection<PaymentDocument>();
        public ObservableCollection<Document> RecentDocuments { get; set; } = new ObservableCollection<Document>();

        public int ResidentsCount { get; set; }
        public int DocumentsCount { get; set; }
        public string Blotter1 { get; set; }
        public string Blotter2 { get; set; }

        private Timer refreshTimer;

        public DashBoardPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadResidentCount();
            LoadDocumentsCount();
            LoadBlotters();
            LoadRecentActivityLogs();
            LoadResidentDemographics();
            LoadUpcomingEvents();
            LoadRecentTransactions();
            LoadRecentDocuments();
            DocumentsListView.ItemsSource = RecentDocuments;
            NavigationPage.SetHasBackButton(this, false);
            NavigationPage.SetHasNavigationBar(this, false);
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsEnabled = false });

            // Initialize and start the refresh timer
            refreshTimer = new Timer(5000); // Set to refresh every 5 seconds
            refreshTimer.Elapsed += OnTimedEvent;
            refreshTimer.AutoReset = true;
            refreshTimer.Enabled = true;
        }

        private void LoadRecentTransactions()
        {
            RecentTransactions.Clear();

            // Modify the query to retrieve the most recent paid document
            string query = "SELECT TOP 1 DocumentName, PaymentAmount, DueDate, Status " +
                           "FROM DocumentPayments " +
                           "WHERE Status = @Status " +
                           "ORDER BY DueDate DESC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add the 'Paid' status as a parameter to prevent SQL injection
                    command.Parameters.AddWithValue("@Status", PaymentStatus.Paid.ToString());

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Only read if there's data
                        {
                            // Add the most recent paid document
                            RecentTransactions.Clear(); // Clear any previous results
                            RecentTransactions.Add(new PaymentDocument
                            {
                                DocumentName = reader.GetString(0),
                                PaymentAmount = reader.GetDecimal(1),
                                DueDate = reader.GetDateTime(2),
                                Status = (PaymentStatus)Enum.Parse(typeof(PaymentStatus), reader.GetString(3))
                            });
                        }
                    }
                }
            }
        }

        private void LoadRecentDocuments()
        {
            RecentDocuments.Clear();

            // Specify the path of the folder containing documents
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");

            if (Directory.Exists(folderPath))
            {
                // Get all files in the folder and order them by the last modified date (most recent first)
                var recentFiles = Directory.GetFiles(folderPath)
                                           .Select(file => new FileInfo(file))
                                           .OrderByDescending(file => file.LastWriteTime)
                                           .Take(1);

                // Add the most recent documents to the ObservableCollection
                foreach (var file in recentFiles)
                {
                    RecentDocuments.Add(new Document
                    {
                        DocumentName = file.Name,
                        DocumentType = file.Extension,
                        DocumentDate = file.LastWriteTime
                    });
                }
            }
            else
            {
                // Handle the case where the folder doesn't exist (optional)
                Console.WriteLine("Folder not found: " + folderPath);
            }
        }

        private async void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            // Call the methods to refresh the dashboard data
            await LoadResidentCount();
            LoadDocumentsCount();
            await LoadBlotters();
            LoadRecentActivityLogs();
        }

        private async Task LoadResidentCount()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Residents";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    ResidentsCount = (int)await command.ExecuteScalarAsync();
                }
            }

            // Refresh the count displayed on the dashboard
            OnPropertyChanged(nameof(ResidentsCount));
        }

        private void LoadDocumentsCount()
        {
            try
            {
                // Check if the directory exists
                if (Directory.Exists(documentsDirectory))
                {
                    // Get all files in the directory
                    var files = Directory.GetFiles(documentsDirectory);

                    // Set the total document count
                    DocumentsCount = files.Length;
                }
                else
                {
                    DocumentsCount = 0;
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that may occur during file system access
                DisplayAlert("Error", $"An error occurred while counting documents: {ex.Message}", "OK");
            }

            // Update the label on the dashboard
            DocumentsCountLabel.Text = DocumentsCount.ToString();
        }

        private async Task LoadBlotters()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT TOP 2 CaseID, IncidentDetails FROM BlotterReports ORDER BY DateReported DESC", connection);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Blotter1 = $"{reader.GetString(0)}: {reader.GetString(1)}"; // CaseID and IncidentDetails
                        }
                        if (await reader.ReadAsync())
                        {
                            Blotter2 = $"{reader.GetString(0)}: {reader.GetString(1)}"; // CaseID and IncidentDetails
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle database connection or query errors
                    await DisplayAlert("Error", $"An error occurred while loading blotters: {ex.Message}", "OK");
                }
            }

            // Refresh the UI bindings
            OnPropertyChanged(nameof(Blotter1));
            OnPropertyChanged(nameof(Blotter2));
        }

        private async void LoadRecentActivityLogs()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 5 ActivityDescription, Timestamp FROM UserLogs ORDER BY Timestamp DESC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        await connection.OpenAsync();
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            ActivityLogs.Clear(); // Clear previous logs before loading new ones
                            int recordCount = 0;
                            while (await reader.ReadAsync())
                            {
                                ActivityLogs.Add(new ActivityLog
                                {
                                    ActivityDescription = reader.GetString(0),
                                    Timestamp = reader.GetDateTime(1),
                                });
                                recordCount++;
                            }
                            Console.WriteLine($"{recordCount} activity logs loaded.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"An error occurred while loading activity logs: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async Task LoadResidentDemographics()
        {
            int maleCount = 0;
            int femaleCount = 0;
            int ageGroup1 = 0; // 0-18
            int ageGroup2 = 0; // 19-35
            int ageGroup3 = 0; // 36-60
            int ageGroup4 = 0; // 61+

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT DateOfBirth, Gender FROM Residents";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        await connection.OpenAsync();
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime dob = reader.GetDateTime(0);
                                string gender = reader.GetString(1);

                                // Calculate age
                                int age = DateTime.Now.Year - dob.Year;
                                if (DateTime.Now.DayOfYear < dob.DayOfYear) age--;

                                // Count gender
                                if (gender.ToLower() == "male")
                                    maleCount++;
                                else if (gender.ToLower() == "female")
                                    femaleCount++;

                                // Count age groups
                                if (age <= 18) ageGroup1++;
                                else if (age <= 35) ageGroup2++;
                                else if (age <= 60) ageGroup3++;
                                else ageGroup4++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"An error occurred while loading resident demographics: {ex.Message}", "OK");
                    }
                }
            }

            // Update UI elements
            MaleCountLabel.Text = $"Males: {maleCount}";
            FemaleCountLabel.Text = $"Females: {femaleCount}";
            AgeGroupLabel.Text = $"Age Group Distribution: ( 0-18: {ageGroup1} ), ( 19-35: {ageGroup2} ), ( 36-60: {ageGroup3} ), ( 61+: {ageGroup4} )";

            // Display gender chart
            DisplayGenderChart(maleCount, femaleCount);

            // Display age group chart
            DisplayAgeGroupChart(ageGroup1, ageGroup2, ageGroup3, ageGroup4);
        }

        private void DisplayGenderChart(int maleCount, int femaleCount)
        {
            var genderEntries = new[]
            {
                new ChartEntry(maleCount) { Label = "Males", ValueLabel = maleCount.ToString(), Color = SkiaSharp.SKColor.Parse("#1B5E20") },
                new ChartEntry(femaleCount) { Label = "Females", ValueLabel = femaleCount.ToString(), Color = SkiaSharp.SKColor.Parse("#D32F2F") },
            };

            GenderChart.Chart = new BarChart { Entries = genderEntries, LabelOrientation = Orientation.Horizontal, ValueLabelOrientation = Orientation.Horizontal ,CornerRadius = 10};
        }

        private void DisplayAgeGroupChart(int ageGroup1, int ageGroup2, int ageGroup3, int ageGroup4)
        {
            var ageEntries = new[]
            {
                new ChartEntry(ageGroup1) { Label = "0-18", ValueLabel = ageGroup1.ToString(), Color = SkiaSharp.SKColor.Parse("#4CAF50") },
                new ChartEntry(ageGroup2) { Label = "19-35", ValueLabel = ageGroup2.ToString(), Color = SkiaSharp.SKColor.Parse("#2196F3") },
                new ChartEntry(ageGroup3) { Label = "36-60", ValueLabel = ageGroup3.ToString(), Color = SkiaSharp.SKColor.Parse("#FFC107") },
                new ChartEntry(ageGroup4) { Label = "61+", ValueLabel = ageGroup4.ToString(), Color = SkiaSharp.SKColor.Parse("#FF5722") },
            };

            AgeGroupChart.Chart = new BarChart { Entries = ageEntries, LabelOrientation = Orientation.Horizontal, ValueLabelOrientation = Orientation.Horizontal, CornerRadius = 10};
        }

        private async void LoadUpcomingEvents()
        {
            var upcomingEvents = new List<Event>();
            var currentDate = DateTime.Now;
            var oneWeekLater = currentDate.AddDays(7);

            using (var connection = new SqlConnection(connectionString))
            {
                // Query for events happening within the next week
                string query = "SELECT EventTitle, EventDescription, EventDate FROM Events WHERE EventDate > @Today AND EventDate <= @OneWeekLater ORDER BY EventDate ASC";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Today", currentDate);
                command.Parameters.AddWithValue("@OneWeekLater", oneWeekLater);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        upcomingEvents.Add(new Event
                        {
                            Title = reader["EventTitle"].ToString(),
                            Date = Convert.ToDateTime(reader["EventDate"]),
                            Description = reader["EventDescription"].ToString(),
                        });
                    }
                }
            }

            EventsListView.ItemsSource = upcomingEvents;

            // After loading events, delete passed events
            await DeletePastEvents();
        }

        public async Task DeletePastEvents()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                // Query to delete events that have already passed
                string query = "DELETE FROM Events WHERE EventDate < @Today";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Today", DateTime.Now);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        // Triggered when an event is selected from the list
        private async void OnEventSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                var selectedEvent = (Event)e.SelectedItem;
                EventsListView.SelectedItem = null;

                // Show event details with SMS option
                await DisplayEventDetailsPopup(selectedEvent);
            }
        }

        // Shows event details and prompts for SMS
        private async Task DisplayEventDetailsPopup(Event selectedEvent)
        {
            var eventDetailsMessage = $"Title: {selectedEvent.Title}\n\n" +
                                      $"Date: {selectedEvent.Date:MMMM dd, yyyy}\n\n" +
                                      $"Description:\n{selectedEvent.Description}";

            // Ask for confirmation to send SMS to residents
            bool sendSms = await DisplayAlert("Event Details", eventDetailsMessage, "Send SMS", "Cancel");

            // If 'Send SMS' is selected, initiate SMS to residents
            if (sendSms)
            {
                await SendSmsToResidents(selectedEvent);
            }
        }

        // Sends SMS notification to residents
        private async Task SendSmsToResidents(Event selectedEvent)
        {
            try
            {
                // Fetch residents’ contact numbers from the database (Assuming SQL connection setup)
                var contactNumbers = await GetResidentsContactNumbersAsync();

                // Message content
                string smsMessage = $"Reminder: {selectedEvent.Title} is scheduled on {selectedEvent.Date:MMMM dd, yyyy}. " +
                                    $"{selectedEvent.Description}";

                // Send SMS to each contact number without showing additional prompts
                foreach (var contact in contactNumbers)
                {
                    // Logic for sending SMS - Placeholder for actual SMS API integration
                    await SendSmsAsync(contact, smsMessage);
                }

                // Inform the user that all SMS notifications have been sent
                await DisplayAlert("Success", "SMS notifications sent to all residents.", "OK");
            }
            catch (Exception ex)
            {
                // Handle errors, such as network issues, invalid numbers, or authentication errors
                await DisplayAlert("Error", $"Failed to send SMS: {ex.Message}", "OK");
            }
        }

        // Retrieves contact numbers of all residents
        private async Task<List<string>> GetResidentsContactNumbersAsync()
        {
            var contactNumbers = new List<string>();

            // Assuming SQL connection and query setup
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT ContactNumber FROM Residents WHERE ContactNumber IS NOT NULL";
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        contactNumbers.Add(reader.GetString(0)); // Assuming ContactNumber is of type string
                    }
                }
            }

            return contactNumbers;
        }

        // Send SMS using Twilio (or any other SMS service)
        private async Task SendSmsAsync(string contactNumber, string message)
        {
            try
            {
                // Twilio credentials (replace these with your actual credentials)
                string accountSid = "your_account_sid";    // Your Twilio Account SID
                string authToken = "your_auth_token";      // Your Twilio Auth Token
                string fromPhoneNumber = "your_twilio_phone_number"; // Your Twilio phone number (e.g. "+1234567890")

                // Initialize Twilio client
                TwilioClient.Init(accountSid, authToken);

                // Send SMS to the contact number
                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(fromPhoneNumber),
                    to: new PhoneNumber(contactNumber)
                );

                // Optionally, log or handle the response if needed
                Console.WriteLine($"SMS sent to {contactNumber}: {messageResource.Sid}");
                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Sent an SMS Reminder to Residents.");
            }
            catch (Exception ex)
            {
                // Handle errors, such as network issues, invalid numbers, or authentication errors
                await DisplayAlert("Error", $"Failed to send SMS: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            refreshTimer?.Stop(); // Stop the timer when the page is not visible
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            refreshTimer?.Start(); // Start the timer when the page is visible
            LoadRecentDocuments();
            LoadRecentTransactions();
            LoadResidentDemographics();
            LoadUpcomingEvents();
        }
    }
}
