using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IMemberRepository
    {
        void Save(Member member);
        Member? FindByAccessKey(string accessKey);
        IEnumerable<Member> GetAll();
    }
}
