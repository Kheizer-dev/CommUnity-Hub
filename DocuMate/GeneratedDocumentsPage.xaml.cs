using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace CommUnity_Hub
{
    public partial class GeneratedDocumentsPage : ContentPage, INotifyPropertyChanged
    {
        private const int ItemsPerPage = 10;

        public ObservableCollection<Document> MyDocuments { get; set; }
        private ObservableCollection<Document> _filteredDocuments;
        private string _searchText;
        private int _currentPage;

        public GeneratedDocumentsPage()
        {
            InitializeComponent();
            MyDocuments = new ObservableCollection<Document>();
            FilteredDocuments = new ObservableCollection<Document>(MyDocuments);
            LoadDocumentsFromDirectory();
            BindingContext = this;
        }

        private void LoadDocumentsFromDirectory()
        {
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
            var pdfFiles = Directory.GetFiles(documentsPath, "*.pdf");

            var sortedFiles = pdfFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                .ToList();

            foreach (var fileInfo in sortedFiles)
            {
                string documentType = GetDocumentTypeFromFileName(fileInfo.Name);

                MyDocuments.Add(new Document
                {
                    DocumentName = fileInfo.Name,
                    DocumentType = documentType,
                    DocumentDate = fileInfo.LastWriteTime
                });
            }
            FilterDocuments(); // Initial filtering
        }

        private string GetDocumentTypeFromFileName(string fileName)
        {
            if (fileName.Contains("BarangayClearance", StringComparison.OrdinalIgnoreCase))
            {
                return "Barangay Clearance";
            }
            else if (fileName.Contains("BusinessPermit", StringComparison.OrdinalIgnoreCase))
            {
                return "Business Permit";
            }
            else if (fileName.Contains("CertificateOfResidency", StringComparison.OrdinalIgnoreCase))
            {
                return "Certificate of Residency";
            }
            else
            {
                return "PDF Document"; // Default type if none of the keywords match
            }
        }


        // Filter documents based on the search text
        private void FilterDocuments()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredDocuments = new ObservableCollection<Document>(MyDocuments);
            }
            else
            {
                var lowerSearchText = SearchText.ToLower();
                FilteredDocuments = new ObservableCollection<Document>(
                    MyDocuments.Where(doc => doc.DocumentName.ToLower().Contains(lowerSearchText) ||
                                             doc.DocumentType.ToLower().Contains(lowerSearchText)));
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = e.NewTextValue;
        }

        [Obsolete]
        private async void OnDocumentSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
            {
                var selectedDocument = (Document)e.CurrentSelection[0];
                if (selectedDocument != null)
                {
                    string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CommUnityHub Documents");
                    string docPath = Path.Combine(documentsPath, selectedDocument.DocumentName);

                    string action = await DisplayActionSheet("Choose Action", "Cancel", null, "Open", "Delete");

                    if (action == "Open")
                    {
                        if (File.Exists(docPath))
                        {
                            using (PdfDocument document = PdfReader.Open(docPath, PdfDocumentOpenMode.ReadOnly))
                            {
                                int pageCount = document.PageCount;
                                Console.WriteLine($"The selected PDF has {pageCount} pages.");
                            }

                            await Launcher.OpenAsync(new OpenFileRequest
                            {
                                File = new ReadOnlyFile(docPath)
                            });

                            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Opened {selectedDocument.DocumentName}.");

                        }
                        else
                        {
                            await DisplayAlert("Error", "Document not found.", "OK");
                        }
                    }
                    else if (action == "Delete")
                    {
                        bool confirm = await DisplayAlert("Delete Document", "Are you sure you want to delete this document?", "Yes", "No");
                        if (confirm)
                        {
                            try
                            {
                                // Delete the file from local storage
                                if (File.Exists(docPath))
                                {
                                    File.Delete(docPath);
                                }
                                else
                                {
                                    await DisplayAlert("Error", "File not found locally.", "OK");
                                }

                                // Determine the document type and delete the corresponding entry from the SQL database
                                string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";
                                // Adjusted deletion code
                                string residentName = GetResidentNameFromFileName(selectedDocument.DocumentName);
                                string deleteQuery = "";

                                if (selectedDocument.DocumentType == "Barangay Clearance")
                                {
                                    deleteQuery = "DELETE FROM BarangayClearance WHERE ResidentName = @ResidentName";
                                }
                                else if (selectedDocument.DocumentType == "Business Permit")
                                {
                                    deleteQuery = "DELETE FROM BusinessPermit WHERE BusinessName = @ResidentName";
                                }
                                else if (selectedDocument.DocumentType == "Certificate of Residency")
                                {
                                    deleteQuery = "DELETE FROM CertificateOfResidency WHERE ResidentName = @ResidentName";
                                }
                                else
                                {
                                    await DisplayAlert("Error", "Unknown document type.", "OK");
                                    return;
                                }

                                // Execute SQL delete operation
                                using (SqlConnection connection = new SqlConnection(connectionString))
                                {
                                    connection.Open();
                                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                                    {
                                        command.Parameters.AddWithValue("@ResidentName", residentName);
                                        int rowsAffected = command.ExecuteNonQuery();

                                        if (rowsAffected > 0)
                                        {
                                            MyDocuments.Remove(selectedDocument);
                                            FilterDocuments();
                                            await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Deleted {selectedDocument.DocumentName}.");
                                            await DisplayAlert("Deleted", "The document has been deleted from the database and local storage.", "OK");
                                        }
                                        else
                                        {
                                            await DisplayAlert("Error", "Document not found in the database.", "OK");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await DisplayAlert("Error", $"Failed to delete the document: {ex.Message}", "OK");
                            }
                        }
                    }
                    ((CollectionView)sender).SelectedItem = null;
                }
            }
        }

        // Common method to extract resident name
        private string GetResidentNameFromFileName(string fileName)
        {
            // Assuming the file names follow a similar pattern like "DocumentType_ResidentName.pdf"
            string namePart = fileName.Substring(fileName.IndexOf('_') + 1).Replace(".pdf", "").Trim();
            return namePart;
        }

        // Property change notification for data binding
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<Document> FilteredDocuments
        {
            get
            {
                return _filteredDocuments;
            }

            set
            {
                _filteredDocuments = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get
            {
                return _searchText;
            }

            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterDocuments(); // Filter the documents whenever search text changes
            }
        }
    }
}
