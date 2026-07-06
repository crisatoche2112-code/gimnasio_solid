using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    public interface IAlertService
    {
        List<string> CheckMembershipExpirations(IEnumerable<Member> members);
    }
}