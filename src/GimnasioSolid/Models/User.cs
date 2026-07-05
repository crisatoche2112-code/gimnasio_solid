namespace GimnasioSolid.Models
{
    public sealed class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public Role Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public User(string id, string username, string email, string passwordHash, Role role)
        {
            Id = id;
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }
}
