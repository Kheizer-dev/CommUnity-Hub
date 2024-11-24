using CommUnity_Hub.EmergencyHotlines;
using Microsoft.Maui.Controls;

namespace CommUnity_Hub
{
    public partial class EmergencyHotlinesPage : ContentPage
    {
        public EmergencyHotlinesPage()
        {
            InitializeComponent();
            InitializeHotlines();
        }

        private void InitializeHotlines()
        {
            var hotlines = new List<Hotline>
            {
                new Hotline { Name = "Bureau of Fire Protection (BFP)", PhoneNumber = "0917-155-1611" },
                new Hotline { Name = "Philippine National Police (PNP)", PhoneNumber = "0906-465-7881" },
                new Hotline { Name = "Rural Health Unit (RHU)", PhoneNumber = "0917-548-0334" },
                new Hotline { Name = "PCF - Infirmary", PhoneNumber = "0950-953-2476" },
                new Hotline { Name = "MDRRMO", PhoneNumber = "0998-998-1918" }
            };

            hotlineCollectionView.ItemsSource = hotlines;
        }

        private async void OnHotlineSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedHotline = e.CurrentSelection.FirstOrDefault() as Hotline;
            if (selectedHotline != null)
            {
                // Prompt the user to enter a custom message
                string message = await DisplayPromptAsync("Compose Message", $"Enter message for {selectedHotline.Name}:");

                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        // Attempt to send the SMS with the user-composed message
                        await Sms.ComposeAsync(new SmsMessage(message, new[] { selectedHotline.PhoneNumber }));

                        await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Sent a Message to {selectedHotline.Name}.");
                    }
                    catch (FeatureNotSupportedException)
                    {
                        await DisplayAlert("Error", "SMS functionality is not available on this device.", "OK");
                    }
                    catch (Exception)
                    {
                        await DisplayAlert("Error", "An error occurred while trying to send the message.", "OK");
                    }
                }

                // Clear the selection (optional)
                hotlineCollectionView.SelectedItem = null;
            }
        }
    }
}
