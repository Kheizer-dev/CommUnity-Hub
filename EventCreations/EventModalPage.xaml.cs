namespace CommUnity_Hub;

public partial class EventModalPage : ContentPage
{
	public EventModalPage()
	{
		InitializeComponent();
        BindingContext = new EventCreationViewModel();
    }

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await Navigation.PopModalAsync();
	}
}