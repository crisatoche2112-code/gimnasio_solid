using System.Collections.Generic;
using System.Linq;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public sealed class MemberRepository : IMemberRepository
    {
        //private readonly Dictionary<string, Member> _membersByAccessKey = new();
        private readonly Dictionary<string, Member> _membersById = new();
        private readonly Dictionary<string, string> _credentialToId = new();

        public void Save(Member member)
        {
            if (member is null)
            {
                return;
            }

            if ( _membersById.TryGetValue(member.Id, out var existing))
            {
                _credentialToId.Remove(existing.AccessKey);
                _credentialToId.Remove(existing.FingerprintSignature);
            }

            _membersById[member.Id] = member;
            _credentialToId[member.AccessKey] = member.Id;
            _credentialToId[member.FingerprintSignature] = member.Id;
        }

        public Member? FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            _membersById.TryGetValue(id.Trim(), out var member);
            return member;
        }

        public Member? FindByAccessKey(string accessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                return null;
            }

            if (!_credentialToId.TryGetValue(accessKey.Trim(), out var memberId))
            {
                return null;
            }

            _membersById.TryGetValue(memberId, out var member);
            return member;
        }

        public IEnumerable<Member> GetAll() => _membersById.Values;

        public void Delete(String id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if ( _membersById.Remove(id.Trim(), out var existing))
            {
                _credentialToId.Remove(existing.AccessKey);
                _credentialToId.Remove(existing.FingerprintSignature);
            }
        }
    }
}
