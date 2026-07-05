using System;

namespace GimnasioSolid.Models
{
    public sealed class PaymentRecord
    {
        public PaymentRecord(string memberId, string memberName, string planName, decimal baseAmount, decimal lateFee, DateTime date, string receiptNumber, string paymentMethod)
        {
            MemberId = memberId;
            MemberName = memberName;
            PlanName = planName;
            BaseAmount = baseAmount;
            LateFee = lateFee;
            Date = date;
            ReceiptNumber = receiptNumber;
            PaymentMethod = paymentMethod;
        }

        public string MemberId { get; }
        public string MemberName { get; }
        public string PlanName { get; }

        // Cuota base del plan, sin recargos
        public decimal BaseAmount { get; }

        // Recargo por mora (0 si el pago fue puntual)
        public decimal LateFee { get; }

        /// Total cobrado (BaseAmount + LateFee).
        public decimal Amount => BaseAmount + LateFee;

        // Total cobrado
        public DateTime Date { get; }

        // Numero de comprobante generado para este pago
        public string ReceiptNumber { get; }

        // Medio con el que se realizó el pago
        public string PaymentMethod { get; }
    }
}
