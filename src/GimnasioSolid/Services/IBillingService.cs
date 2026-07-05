using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    
    // Abstracción del servicio de facturación. Depender de esta interfaz (y no de la
    // clase concreta BillingService) permite cumplir el Principio de Inversión de
    // Dependencias (DIP) y facilita crear mocks para pruebas unitarias.
    public interface IBillingService
    {
        // Calcula la cuota que le corresponde pagar al miembro hoy, incluyendo mora si aplica.
        decimal CalculateMonthlyFee(Member member);

        // Registra el pago del miembro con el método indicado (Efectivo, Tarjeta, Billetera digital),
        // renueva su membresía y devuelve el comprobante generado.
        PaymentRecord RegisterMonthlyPayment(Member member, string paymentMethod);
    }
}
