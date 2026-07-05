using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    // Genera el comprobante de pago en formato PDF a partir de un PaymentRecord ya registrado
    public interface IReceiptPdfGenerator
    {
        byte[] Generate(PaymentRecord payment);
    }
}
