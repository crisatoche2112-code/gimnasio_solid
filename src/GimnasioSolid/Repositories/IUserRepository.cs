using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IUserRepository
    {
        void Save(User user);
        User? GetById(string id);
        User? GetByUsername(string username);
        IEnumerable<User> GetAll();
        void Update(User user);
        void Delete(string id);
    }
}
