using GimnasioSolid.Models;
using GimnasioSolid.Services;

namespace GimnasioSolid.Controllers
{
    public sealed class LoginController
    {
        private readonly AuthenticationService _authService;

        public LoginController(AuthenticationService authService)
        {
            _authService = authService;
        }

        public User? Login(string username, string password)
        {
            return _authService.Authenticate(username, password);
        }

        public void Register(string id, string username, string email, string password, Role role)
        {
            _authService.CreateUser(id, username, email, password, role);
        }
    }
}
