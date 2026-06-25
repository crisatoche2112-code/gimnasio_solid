using GimnasioSolid.Models;
using GimnasioSolid.Scanners;

namespace GimnasioSolid.Controllers
{
    public sealed class AccessControl
    {
        private IAccessScanner _scanner;

        public AccessControl(IAccessScanner scanner)
        {
            _scanner = scanner;
        }

        public void SetScanner(IAccessScanner scanner)
        {
            _scanner = scanner;
        }

        public bool CanOpenDoor(Member? member, string presentedData)
        {
            if (member is null)
            {
                return false;
            }

            var scannedCredential = _scanner.Scan(presentedData);
            return member.IsValidAccessCredential(scannedCredential);
        }
    }
}
