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
        public DateTime ExpirationDate { get; private set; }

        // Indica si la membresía sigue vigente a la fecha de hoy
        public bool IsActive => ExpirationDate.Date >= DateTime.Today;

        // Indica si el pago del miembro está atrasado (la fecha de expiración ya pasó)
        public bool IsOverdue => ExpirationDate.Date < DateTime.Today;

        public bool IsValidAccessCredential(string credential)
        {
            return credential == AccessKey || credential == FingerprintSignature;
        }

        // Renueva la membresía a partir de la fecha de expiración vigente (o de hoy, si ya venció),
        // para no restarle días al miembro que paga por adelantado.
        public void RenewMembership(int months = 1)
        {
            var renewalStart = ExpirationDate.Date > DateTime.Today ? ExpirationDate.Date : DateTime.Today;
            ExpirationDate = renewalStart.AddMonths(months);
        }
    }
}
