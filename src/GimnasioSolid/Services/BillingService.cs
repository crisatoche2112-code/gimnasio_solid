using System;
using GimnasioSolid.Models;
using GimnasioSolid.Repositories;

namespace GimnasioSolid.Services
{
    public sealed class BillingService : IBillingService
    {
        /// <summary>Recargo por mora: 10% de la cuota base cuando el miembro paga vencido.</summary>
        private const decimal LateFeePercentage = 0.10m;

        private readonly IPaymentRepository _paymentRepository;

        public BillingService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public decimal CalculateMonthlyFee(Member member)
        {
            if (member is null)
            {
                return 0m;
            }

            var baseAmount = member.MembershipPlan.CalculatePrice();
            return baseAmount + CalculateLateFee(member, baseAmount);
        }

        public PaymentRecord RegisterMonthlyPayment(Member member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var baseAmount = member.MembershipPlan.CalculatePrice();
            var lateFee = CalculateLateFee(member, baseAmount);
            var receiptNumber = GenerateReceiptNumber(member);

            var paymentRecord = new PaymentRecord(
                member.Id,
                member.Name,
                member.MembershipPlan.GetType().Name,
                baseAmount,
                lateFee,
                DateTime.Now,
                receiptNumber);

            _paymentRepository.Save(paymentRecord);

            // Al registrar el pago, la membresía se renueva automáticamente.
            member.RenewMembership();

            return paymentRecord;
        }

        private static decimal CalculateLateFee(Member member, decimal baseAmount)
        {
            return member.IsOverdue ? Math.Round(baseAmount * LateFeePercentage, 2) : 0m;
        }

        private static string GenerateReceiptNumber(Member member)
        {
            return $"RCB-{member.Id}-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}
