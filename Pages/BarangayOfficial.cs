using System.Collections.ObjectModel;

namespace CommUnity_Hub
{
    public class BarangayOfficial
    {
        public string? ImageSource { get; set; }
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
    }

    public static class BarangayOfficialsData
    {
        // Sample data collection
        public static ObservableCollection<BarangayOfficial> BarangayOfficialsCollection { get; } = new ObservableCollection<BarangayOfficial>
    {
        new BarangayOfficial { ImageSource = "barangay_captain.jpg", Name = "Clifford L. Ibañez", Position = "Barangay Captain", Address = "Riverside", ContactNumber = "123-456-7890" },
        new BarangayOfficial { ImageSource = "manuel_bombasi.jpg", Name = "Manuel R. Bombasi", Position = "Barangay Kagawad", Address = "Laud", ContactNumber = "098-765-4321" },
        new BarangayOfficial { ImageSource = "user_icon.png", Name = "Lachmanny T. Ramiscal", Position = "Barangay Kagawad", Address = "Riverside", ContactNumber = "321-654-0987" },
        new BarangayOfficial { ImageSource = "emmanual_millan.jpg", Name = "Emmanuel T. Millan", Position = "Barangay Kagawad", Address = "Laoac", ContactNumber = "456-789-1234" },
        new BarangayOfficial { ImageSource = "user_icon.png", Name = "Felicidad D. Gille", Position = "Barangay Kagawad", Address = "Laoac", ContactNumber = "789-123-4560" },
        new BarangayOfficial { ImageSource = "lani_caras.jpg", Name = "Lani C. Caras", Position = "Barangay Kagawad", Address = "Centro", ContactNumber = "654-321-0987" },
        new BarangayOfficial { ImageSource = "user_icon.png", Name = "Isagani M. Ancheta", Position = "Barangay Kagawad", Address = "Riverside", ContactNumber = "987-654-3210" },
        new BarangayOfficial { ImageSource = "user_icon.png", Name = "Edilarosa P. Ramiscal", Position = "Barangay Kagawad", Address = "RIverside", ContactNumber = "345-678-9012" },
        new BarangayOfficial { ImageSource = "danna_delacruz.jpg", Name = "Danna Mae A. Dela Cruz", Position = "SK Chairperson", Address = "Laoac", ContactNumber = "210-987-6543" },
        new BarangayOfficial { ImageSource = "cristina_russiana.jpg", Name = "Cristina A. Russiana", Position = "Barangay Secretary", Address = "Laoac", ContactNumber = "678-901-2345" },
        new BarangayOfficial { ImageSource = "amy_bembo.jpg", Name = "Amy F. Bembo", Position = "Barangay Treasurer", Address = "Riverside", ContactNumber = "432-109-8765" }
    };
    }

}
