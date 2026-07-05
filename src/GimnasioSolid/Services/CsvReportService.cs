using System.Collections.Generic;
using System.Text;
using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    public class CsvReportService : IReportService
    {
        public string GenerateMembersCsv(IEnumerable<Member> members)
        {
            var csvBuilder = new StringBuilder();
            
            // Encabezados del CSV
            csvBuilder.AppendLine("ID,Nombre,Plan,FechaVencimiento");

            // Recorremos tus miembros reales usando sus propiedades exactas
            foreach (var member in members)
            {
                csvBuilder.AppendLine($"{member.Id},{member.Name},{member.MembershipPlan.GetType().Name},{member.ExpirationDate:yyyy-MM-dd}");
            }

            return csvBuilder.ToString();
        }
    }
}