using GimnasioSolid.Models;
using GimnasioSolid.Repositories;
using GimnasioSolid.Scanners;

namespace GimnasioSolid.Controllers
{
    public sealed class TurnstileController
    {
        private readonly IMemberRepository _repository;
        private readonly AccessControl _accessControl;

        public TurnstileController(IMemberRepository repository, AccessControl accessControl)
        {
            _repository = repository;
            _accessControl = accessControl;
        }

        public bool ValidateEntry(string presentedData)
        {
            var member = _repository.FindByAccessKey(presentedData);
            return _accessControl.CanOpenDoor(member, presentedData);
        }

        public void SetScanner(IAccessScanner scanner)
        {
            _accessControl.SetScanner(scanner);
        }
    }
}
