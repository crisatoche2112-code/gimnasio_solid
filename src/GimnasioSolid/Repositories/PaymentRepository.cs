using System;
using System.Collections.Generic;
using System.Linq;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly List<PaymentRecord> _payments = new();

        public void Save(PaymentRecord paymentRecord)
        {
            if (paymentRecord is null)
            {
                return;
            }

            _payments.Add(paymentRecord);
        }

        public IEnumerable<PaymentRecord> GetAll() => _payments.AsReadOnly();

        public PaymentRecord? GetByReceiptNumber(string receiptNumber)
        {
            return _payments.FirstOrDefault(p => p.ReceiptNumber.Equals(receiptNumber, StringComparison.OrdinalIgnoreCase));
        }
    }
}
