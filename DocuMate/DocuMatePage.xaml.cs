using System;
using Microsoft.Maui.Controls;

namespace CommUnity_Hub
{
    public partial class DocuMatePage : ContentPage
    {
        public DocuMatePage()
        {
            InitializeComponent();
        }

        // Handle create button click based on selected document type
        private async void OnCreateButtonClicked(object sender, EventArgs e)
        {
            if (DocumentTypePicker.SelectedIndex == -1)
            {
                await DisplayAlert("Error", "Please select a document type before proceeding.", "OK");
                return;
            }

            string selectedDocumentType = DocumentTypePicker.SelectedItem.ToString();

            switch (selectedDocumentType)
            {
                case "Barangay Clearance":
                    await Navigation.PushAsync(new BarangayClearancePage());
                    break;

                case "Certificate of Residency":
                    // Navigate to the ResidentSelectionPage
                    await Navigation.PushAsync(new ResidentSelectionPage());
                    break;

                case "Business Permit":
                    await Navigation.PushAsync(new BusinessPermitPage());
                    break;

                default:
                    await DisplayAlert("Error", "Unknown document type selected.", "OK");
                    break;
            }
        }

        // Handle view generated documents button click
        private async void OnViewGeneratedDocumentsClicked(object sender, EventArgs e)
        {
            // Navigate to the page that shows stored documents
            await Navigation.PushAsync(new GeneratedDocumentsPage());
        }
    }
}
