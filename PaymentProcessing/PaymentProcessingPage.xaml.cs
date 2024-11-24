using System.Collections.ObjectModel;
using PdfSharpCore.Pdf; // For PDF generation
using PdfSharpCore.Drawing; // For drawing on PDF
using System.Diagnostics;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using PdfPage = PdfSharpCore.Pdf.PdfPage;
using PdfDocument = PdfSharpCore.Pdf.PdfDocument;
using Microsoft.Data.SqlClient;

namespace CommUnity_Hub
{
    public partial class PaymentProcessingPage : ContentPage
    {
        public ObservableCollection<PaymentDocument> PaymentDocuments { get; set; }
        private readonly string documentsDirectory = @"C:\Users\jayja\Documents\CommUnityHub Documents"; // Directory to scan for documents
        private readonly string receiptsDirectory = @"C:\Users\jayja\Documents\CommUnityHub Invoice"; // Directory for receipts
        private readonly string connectionString = "Server=YRNAD21\\SQLEXPRESS;Database=CommUnityHub;Trusted_Connection=True;TrustServerCertificate=True;";

        public PaymentProcessingPage()
        {
            InitializeComponent();
            InitializePaymentDocuments();
            BindingContext = this;
            PaymentDocuments = new ObservableCollection<PaymentDocument>();
            LoadPaymentDocumentsFromDatabase();
            OnPropertyChanged(nameof(PaymentDocuments));

            // Ensure receipts directory exists
            if (!Directory.Exists(receiptsDirectory))
            {
                Directory.CreateDirectory(receiptsDirectory);
            }
        }

        private void LoadPaymentDocumentsFromDatabase()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT DocumentName, PaymentAmount, DueDate, Status FROM DocumentPayments";
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PaymentDocuments.Add(new PaymentDocument
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

        private void InitializePaymentDocuments()
        {
            // Initialize the collection of payment documents
            PaymentDocuments = new ObservableCollection<PaymentDocument>();

            // Scan the directory for documents
            ScanDocumentsDirectory();

            // Check which documents need to be paid
            CheckDocumentsForPayments();
        }

        private void ScanDocumentsDirectory()
        {
            // Get all PDF files from the specified directory
            var documents = Directory.GetFiles(documentsDirectory, "*.pdf", SearchOption.TopDirectoryOnly);

            foreach (var documentPath in documents)
            {
                var documentName = Path.GetFileName(documentPath);
                PaymentDocuments.Add(new PaymentDocument
                {
                    DocumentName = documentName,
                    DocumentPath = documentPath, // Store the full path for later use
                    PaymentAmount = 0, // Default to 0, will check payment requirements later
                    DueDate = DateTime.Now.AddDays(7), // Default due date, can be customized
                    Status = PaymentStatus.Pending // Initial status
                });
            }
        }

        private void CheckDocumentsForPayments()
        {
            var documentsRequiringPayment = new ObservableCollection<PaymentDocument>();

            foreach (var paymentDocument in PaymentDocuments)
            {
                try
                {
                    // Extract text from the PDF document
                    string content = ExtractTextFromPdf(paymentDocument.DocumentPath).ToLower(); // Convert to lowercase for case-insensitive search

                    // Define keywords and corresponding payment amounts
                    var paymentCriteria = new Dictionary<string, decimal>
                    {
                        { "barangay clearance", 500m },
                        { "business permit", 1000m },
                        // Add more criteria as needed
                    };

                    // Check if any of the keywords are present in the document
                    foreach (var criterion in paymentCriteria)
                    {
                        if (content.Contains(criterion.Key.ToLower()))
                        {
                            paymentDocument.PaymentAmount = criterion.Value;
                            // Optionally, set a specific due date based on the document type
                            paymentDocument.DueDate = DateTime.Now.AddDays(14);
                            paymentDocument.Status = PaymentStatus.Pending; // Set initial status
                            documentsRequiringPayment.Add(paymentDocument); // Add to the filtered list
                            break; // Stop checking after the first match
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., file access issues, corrupted documents)
                    Console.WriteLine($"Error processing {paymentDocument.DocumentName}: {ex.Message}");
                }
            }

            // Only keep documents that require payment
            PaymentDocuments = documentsRequiringPayment;
            OnPropertyChanged(nameof(PaymentDocuments)); // Notify UI of the updated collection
        }

        private string ExtractTextFromPdf(string pdfPath)
        {
            StringBuilder text = new StringBuilder();

            using (PdfReader pdfReader = new PdfReader(pdfPath))
            using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    // Extract text from the page using iText
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i));
                    text.Append(pageText);
                }
            }

            return text.ToString();
        }

        private async void OnDocumentSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedDocument = (PaymentDocument)DocumentsCollectionView.SelectedItem;

            if (selectedDocument != null && selectedDocument.PaymentAmount > 0 && selectedDocument.Status == PaymentStatus.Pending)
            {
                bool isConfirmed = await DisplayAlert("Process Payment", $"Do you want to process a payment of {selectedDocument.PaymentAmount:C} for {selectedDocument.DocumentName}?", "Yes", "No");

                if (isConfirmed)
                {
                    await Task.Delay(1000);
                    selectedDocument.Status = PaymentStatus.Paid;

                    await DisplayAlert("Payment Processed", $"Payment of {selectedDocument.PaymentAmount:C} for {selectedDocument.DocumentName} has been processed.", "OK");

                    UpdateDocumentPaymentStatus(selectedDocument);

                    await ActivityLog.LogActivity(MainPage.LoggedInUserId, $"{ActivityLog.GetUsername(MainPage.LoggedInUserId)} Processed Payment {selectedDocument.DocumentName}.");

                    GeneratePdfReceipt(selectedDocument);
                    DocumentsCollectionView.SelectedItem = null;
                }
                else
                {
                    DocumentsCollectionView.SelectedItem = null;
                }
            }
            else if (selectedDocument != null && selectedDocument.Status == PaymentStatus.Paid)
            {
                await DisplayAlert("Already Paid", $"The document {selectedDocument.DocumentName} has already been paid.", "OK");
                DocumentsCollectionView.SelectedItem = null; // Clear selection
            }
            else if (selectedDocument != null)
            {
                await DisplayAlert("No Payment Required", $"The document {selectedDocument.DocumentName} does not require a payment.", "OK");
                DocumentsCollectionView.SelectedItem = null; // Clear selection
            }
        }

        private void OnRefreshButtonClicked(object sender, EventArgs e)
        {
            // Clear existing documents
            PaymentDocuments.Clear();

            // Reload payment documents from the database
            LoadPaymentDocumentsFromDatabase();
        }

        private void UpdateDocumentPaymentStatus(PaymentDocument document)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                IF EXISTS (SELECT 1 FROM DocumentPayments WHERE DocumentName = @DocumentName)
                BEGIN
                    UPDATE DocumentPayments
                    SET Status = 'Paid'
                    WHERE DocumentName = @DocumentName
                END
                ELSE
                BEGIN
                    INSERT INTO DocumentPayments (DocumentName, PaymentAmount, DueDate, Status)
                    VALUES (@DocumentName, @PaymentAmount, @DueDate, 'Paid')
                END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentName", document.DocumentName);
                    command.Parameters.AddWithValue("@PaymentAmount", document.PaymentAmount);
                    command.Parameters.AddWithValue("@DueDate", document.DueDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Method to generate a receipt as a PDF
        private void GeneratePdfReceipt(PaymentDocument document)
        {
            string receiptPath = Path.Combine(receiptsDirectory, $"Receipt_{document.DocumentName}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            using (PdfDocument pdf = new PdfDocument())
            {
                // Add a page to the PDF document
                var page = pdf.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont fontTitle = new XFont("Arial", 18, XFontStyle.Bold);
                XFont fontBody = new XFont("Arial", 12);

                // Draw the title
                gfx.DrawString("Payment Invoice", fontTitle, XBrushes.Black, new XPoint(300, 50), XStringFormats.Center);

                // Add payment details
                gfx.DrawString($"Document: {document.DocumentName}", fontBody, XBrushes.Black, new XPoint(50, 100));
                gfx.DrawString($"Amount: {document.PaymentAmount:C}", fontBody, XBrushes.Black, new XPoint(50, 130));
                gfx.DrawString($"Payment Date: {DateTime.Now}", fontBody, XBrushes.Black, new XPoint(50, 160));
                gfx.DrawString($"Status: {document.Status}", fontBody, XBrushes.Black, new XPoint(50, 190));

                // Save the PDF
                pdf.Save(receiptPath);
            }

            // Open the receipt for viewing/printing
            Process.Start(new ProcessStartInfo(receiptPath) { UseShellExecute = true });
        }
    }
}
