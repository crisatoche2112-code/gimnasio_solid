using System;
using GimnasioSolid.Models;
using GimnasioSolid.Repositories;

namespace GimnasioSolid.Services
{
    public sealed class BillingService
    {
        private readonly IPaymentRepository _paymentRepository;

        public BillingService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public PaymentRecord RegisterMonthlyPayment(Member member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var amount = member.MembershipPlan.CalculatePrice();
            var paymentRecord = new PaymentRecord(member.Id, member.Name, member.MembershipPlan.GetType().Name, amount, DateTime.Now);
            _paymentRepository.Save(paymentRecord);
            return paymentRecord;
        }

        public decimal CalculateMonthlyFee(Member member)
        {
            return member?.MembershipPlan.CalculatePrice() ?? 0m;
        }
    }
}
