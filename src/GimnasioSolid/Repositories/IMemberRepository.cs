using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IMemberRepository
    {
        void Save(Member member);
        Member? GetById(string id);
        Member? FindByAccessKey(string accessKey);
        IEnumerable<Member> GetAll();
        void Update(Member member);
        void Delete(string id);
    }
}
