using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommUnity_Hub
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _username;
        private DateTime _dateOfBirth;
        private string _email;
        private string _address;
        private string _phone;
        private byte[] _profileImage;
        private bool _isAdmin;

        public int UserId { get; set; } // Assuming you have a UserId property to store the user's ID

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                SetProperty(ref _name, value);
            }
        }

        public string Username
        {
            get
            {
                return _username;
            }

            set
            {
                SetProperty(ref _username, value);
            }
        }

        public DateTime DateOfBirth
        {
            get
            {
                return _dateOfBirth;
            }

            set
            {
                SetProperty(ref _dateOfBirth, value);
            }
        }

        public string Email
        {
            get
            {
                return _email;
            }

            set
            {
                SetProperty(ref _email, value);
            }
        }

        public string Address
        {
            get
            {
                return _address;
            }

            set
            {
                SetProperty(ref _address, value);
            }
        }

        public string Phone
        {
            get
            {
                return _phone;
            }

            set
            {
                SetProperty(ref _phone, value);
            }
        }

        public byte[] ProfileImage
        {
            get
            {
                return _profileImage;
            }

            set
            {
                SetProperty(ref _profileImage, value);
            }
        }

        public bool IsAdmin
        {
            get
            {
                return _isAdmin;
            }

            set
            {
                _isAdmin = value;
                OnPropertyChanged(nameof(IsAdmin));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
