using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class EventCreationPage : ContentPage
    {
        private string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
        public EventCreationPage()
        {
            InitializeComponent();
        }

        private async void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                var selectedEvent = e.CurrentSelection[0] as Event; // Assuming Event is your data model

                var answer = await DisplayAlert("Delete Event",
                    $"Are you sure you want to delete the event: {selectedEvent.Title}?",
                    "Yes", "No");

                if (answer)
                {
                    await DeleteEvent(selectedEvent);
                    // Refresh the events list after deletion
                    (BindingContext as EventCreationViewModel)?.LoadEvents();
                    await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Deleted {selectedEvent.Title}.");
                }

                // Deselect the item to prevent it from staying selected
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async Task DeleteEvent(Event selectedEvent)
        {
            using var connection = new SqlConnection(connectionString);
            string query = "DELETE FROM Events WHERE EventTitle = @EventTitle AND EventDate = @EventDate";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EventTitle", selectedEvent.Title);
            command.Parameters.AddWithValue("@EventDate", selectedEvent.Date);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
