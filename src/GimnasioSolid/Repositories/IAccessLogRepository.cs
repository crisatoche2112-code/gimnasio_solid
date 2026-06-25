using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IAccessLogRepository
    {
        void Save(AccessLog accessLog);
        IEnumerable<AccessLog> GetAll();
    }
}
