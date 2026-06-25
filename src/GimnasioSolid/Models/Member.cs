using System;
using GimnasioSolid.Memberships;

namespace GimnasioSolid.Models
{
    public sealed class Member
    {
        public Member(string id, string name, IMembershipPlan membershipPlan, string accessKey, string fingerprintSignature)
        {
            Id = id;
            Name = name;
            MembershipPlan = membershipPlan;
            AccessKey = accessKey;
            FingerprintSignature = fingerprintSignature;
            ExpirationDate = DateTime.Today.AddMonths(1);
        }

        public string Id { get; }
        public string Name { get; }
        public IMembershipPlan MembershipPlan { get; }
        public string AccessKey { get; }
        public string FingerprintSignature { get; }
        public DateTime ExpirationDate { get; }

        public bool IsValidAccessCredential(string credential)
        {
            return credential == AccessKey || credential == FingerprintSignature;
        }
    }
}
