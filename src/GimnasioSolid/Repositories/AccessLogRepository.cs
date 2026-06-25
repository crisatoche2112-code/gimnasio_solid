using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public sealed class AccessLogRepository : IAccessLogRepository
    {
        private readonly List<AccessLog> _logs = new();

        public void Save(AccessLog accessLog)
        {
            if (accessLog is null)
            {
                return;
            }

            _logs.Add(accessLog);
        }

        public IEnumerable<AccessLog> GetAll() => _logs.AsReadOnly();
    }
}
