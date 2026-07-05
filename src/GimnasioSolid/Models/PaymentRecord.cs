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

        /// <summary>Cuota base del plan, sin recargos.</summary>
        public decimal BaseAmount { get; }

        /// <summary>Recargo por mora (0 si el pago fue puntual).</summary>
        public decimal LateFee { get; }

        /// <summary>Total cobrado (BaseAmount + LateFee).</summary>
        public decimal Amount => BaseAmount + LateFee;

        public DateTime Date { get; }

        /// <summary>Numero de comprobante/recibo generado para este pago.</summary>
        public string ReceiptNumber { get; }

        /// <summary>Medio con el que se realizó el pago (Efectivo, Tarjeta, Billetera digital).</summary>
        public string PaymentMethod { get; }
    }
}
