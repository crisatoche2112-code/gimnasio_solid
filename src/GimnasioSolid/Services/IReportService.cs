using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    public interface IReportService
    {
        string GenerateMembersCsv(IEnumerable<Member> members);
    }
}