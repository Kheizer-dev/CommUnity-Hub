using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommUnity_Hub
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public byte[] ProfileImage { get; set; }

        public User()
        {

        }

        public User(int id, string name, byte[] profileImage = null)
        {
            Id = id;
            Name = name;
            ProfileImage = profileImage;
        }

        public User(int id, string name, string username, DateTime dateOfBirth, string email, string address, string phone, byte[] profileImage = null)
        {
            Id = id;
            Name = name;
            Username = username;
            DateOfBirth = dateOfBirth;
            Email = email;
            Address = address;
            Phone = phone;
            ProfileImage = profileImage;
        }
    }
}
