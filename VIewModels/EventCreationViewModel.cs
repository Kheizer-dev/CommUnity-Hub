using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;

namespace CommUnity_Hub
{
    public class EventCreationViewModel : INotifyPropertyChanged
    {
        private string _eventTitle;
        private string _eventDescription;
        private DateTime _selectedDate;
        private readonly string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public ObservableCollection<Event> Events { get; }
        public ICommand AddEventCommand { get; }
        public ICommand OpenAddEventModalCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public EventCreationViewModel()
        {
            AddEventCommand = new Command(async () => await OnAddEventClicked());
            OpenAddEventModalCommand = new Command(async () => await OpenAddEventModal());
            RefreshCommand = new Command(async () => await LoadEvents());
            SelectedDate = DateTime.Now;
            Events = new ObservableCollection<Event>();
            LoadEvents();
        }

        public string EventTitle
        {
            get
            {
                return _eventTitle;
            }

            set { _eventTitle = value; OnPropertyChanged(nameof(EventTitle)); }
        }

        public string EventDescription
        {
            get
            {
                return _eventDescription;
            }

            set { _eventDescription = value; OnPropertyChanged(nameof(EventDescription)); }
        }

        public DateTime SelectedDate
        {
            get
            {
                return _selectedDate;
            }

            set { _selectedDate = value; OnPropertyChanged(nameof(SelectedDate)); }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OnAddEventClicked()
        {
            if (SelectedDate == DateTime.MinValue || string.IsNullOrEmpty(EventTitle))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please provide a date and title.", "OK");
                return;
            }

            await AddEventToDatabase(EventTitle, EventDescription, SelectedDate);
            Events.Add(new Event { Title = EventTitle, Description = EventDescription, Date = SelectedDate });
            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Addded an Event {EventTitle}.");
            await Application.Current.MainPage.Navigation.PopModalAsync(); // Close modal after saving
            EventTitle = string.Empty;
            EventDescription = string.Empty;
        }

        private async Task OpenAddEventModal()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(new EventModalPage());
        }

        private async Task AddEventToDatabase(string title, string description, DateTime eventDate)
        {
            using var connection = new SqlConnection(connectionString);
            string query = "INSERT INTO Events (EventTitle, EventDescription, EventDate) VALUES (@Title, @Description, @EventDate)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Description", description);
            command.Parameters.AddWithValue("@EventDate", eventDate);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task LoadEvents()
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT EventTitle, EventDescription, EventDate FROM Events ORDER BY EventDate ASC"; // Order by EventDate
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            Events.Clear(); // Clear existing events to avoid duplicate loading
            while (await reader.ReadAsync())
            {
                Events.Add(new Event
                {
                    Title = reader.GetString(0),
                    Description = reader.GetString(1),
                    Date = reader.GetDateTime(2)
                });
            }
        }
    }
}
