using System;

namespace GimnasioSolid.Models
{
    public sealed class PaymentRecord
    {
        public PaymentRecord(string memberId, string memberName, string planName, decimal amount, DateTime date)
        {
            MemberId = memberId;
            MemberName = memberName;
            PlanName = planName;
            Amount = amount;
            Date = date;
        }

        public string MemberId { get; }
        public string MemberName { get; }
        public string PlanName { get; }
        public decimal Amount { get; }
        public DateTime Date { get; }
    }
}
