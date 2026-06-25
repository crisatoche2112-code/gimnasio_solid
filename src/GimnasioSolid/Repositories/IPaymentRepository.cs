using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Repositories
{
    public interface IPaymentRepository
    {
        void Save(PaymentRecord paymentRecord);
        IEnumerable<PaymentRecord> GetAll();
    }
}
