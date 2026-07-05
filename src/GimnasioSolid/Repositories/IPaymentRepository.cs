using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IPaymentRepository
    {
        void Save(PaymentRecord paymentRecord);
        IEnumerable<PaymentRecord> GetAll();

        // Busca un pago por su número de comprobante (usado para regenerar el PDF)
        PaymentRecord? GetByReceiptNumber(string receiptNumber);
    }
}
