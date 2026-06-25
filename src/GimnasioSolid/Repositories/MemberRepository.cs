using System.Collections.Generic;
using System.Linq;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public sealed class MemberRepository : IMemberRepository
    {
        private readonly Dictionary<string, Member> _membersByAccessKey = new();

        public void Save(Member member)
        {
            if (member is null)
            {
                return;
            }

            _membersByAccessKey[member.AccessKey] = member;
            _membersByAccessKey[member.FingerprintSignature] = member;
        }

        public Member? FindByAccessKey(string accessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                return null;
            }

            _membersByAccessKey.TryGetValue(accessKey.Trim(), out var member);
            return member;
        }

        public IEnumerable<Member> GetAll() => _membersByAccessKey.Values.DistinctBy(member => member.Id);
    }
}
