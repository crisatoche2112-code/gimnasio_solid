using GimnasioSolid.Models;
using GimnasioSolid.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace GimnasioSolid.Services
{
    public sealed class AuthenticationService
    {
        private readonly IUserRepository _userRepository;

        public AuthenticationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }

        public User? Authenticate(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            if (VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        public void CreateUser(string id, string username, string email, string password, Role role)
        {
            var existingUser = _userRepository.GetByUsername(username);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"Username '{username}' already exists.");
            }

            var passwordHash = HashPassword(password);
            var user = new User(id, username, email, passwordHash, role);
            _userRepository.Save(user);
        }
    }
}
