
namespace CommUnity_Hub
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = MainPage.UserViewModel;
        }

        // This method is called when the page appears
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Call the LoadUserData method when the page appears
            await MainPage.UserViewModel.LoadUserData(MainPage.LoggedInUserId);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Ask for confirmation before logging out
            bool confirmLogout = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (confirmLogout)
            {
                await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Logged Out.");
                // Navigate back to the login page
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}
