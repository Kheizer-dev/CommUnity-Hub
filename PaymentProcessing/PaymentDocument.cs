using System.ComponentModel;
using System.Globalization;

namespace CommUnity_Hub
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Overdue
    }

    public class PaymentDocument : INotifyPropertyChanged, IValueConverter
    {
        private PaymentStatus status;

        public string? DocumentName { get; set; }
        public string? DocumentPath { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime DueDate { get; set; }

        public PaymentStatus Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentStatus status)
            {
                return status switch
                {
                    PaymentStatus.Pending => Colors.Orange,
                    PaymentStatus.Paid => Colors.Green,
                    PaymentStatus.Overdue => Colors.Red,
                    _ => Colors.Black,
                };
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
