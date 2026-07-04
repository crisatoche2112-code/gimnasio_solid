using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    /// <summary>
    /// Abstracción del servicio de facturación. Depender de esta interfaz (y no de la
    /// clase concreta BillingService) permite cumplir el Principio de Inversión de
    /// Dependencias (DIP) y facilita crear mocks para pruebas unitarias.
    /// </summary>
    public interface IBillingService
    {
        /// <summary>Calcula la cuota que le corresponde pagar al miembro hoy, incluyendo mora si aplica.</summary>
        decimal CalculateMonthlyFee(Member member);

        /// <summary>Registra el pago del miembro, renueva su membresía y devuelve el comprobante generado.</summary>
        PaymentRecord RegisterMonthlyPayment(Member member);
    }
}
