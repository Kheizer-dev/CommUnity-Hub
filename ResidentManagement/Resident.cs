
namespace CommUnity_Hub
{
    public class Resident
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; } // Full middle name
        public string? MiddleInitial
        {
            get
            {
                // Return the first letter of the middle name, or an empty string if middle name is null/empty
                return !string.IsNullOrWhiteSpace(MiddleName) ? MiddleName.Substring(0, 1).ToUpper() : string.Empty;
            }
        }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? CivilStatus { get; set; }
        public string? Nationality { get; set; }
        public string? Religion { get; set; }
        public string? Occupation { get; set; }
        public string? EducationalAttainment { get; set; }
        public bool HasBlotter { get; set; }

        // Method to get full name with middle initial
        public string GetFullName()
        {
            // Combine First Name, Middle Initial, and Last Name
            return $"{FirstName} {MiddleInitial}. {LastName}";
        }

        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public string DisplayName
        {
            get
            {
                return $"{FirstName}, Age: {Age}";
            }
        }
    }
}
