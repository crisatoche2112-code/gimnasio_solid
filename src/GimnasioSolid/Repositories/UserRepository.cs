using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public void Save(User user)
        {
            if (_users.Any(u => u.Id.Equals(user.Id, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"User with ID {user.Id} already exists.");
            }
            _users.Add(user);
        }

        public User? GetById(string id)
        {
            return _users.FirstOrDefault(u => u.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public User? GetByUsername(string username)
        {
            return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<User> GetAll()
        {
            return _users.AsReadOnly();
        }

        public void Update(User user)
        {
            var existingUser = GetById(user.Id);
            if (existingUser == null)
            {
                throw new InvalidOperationException($"User with ID {user.Id} not found.");
            }
            var index = _users.IndexOf(existingUser);
            _users[index] = user;
        }

        public void Delete(string id)
        {
            var user = GetById(id);
            if (user != null)
            {
                _users.Remove(user);
            }
        }
    }
}
