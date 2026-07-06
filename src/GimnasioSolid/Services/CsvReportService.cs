using System;
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

            // Encabezados del CSV separados por punto y coma en mayúsculas
            csvBuilder.AppendLine("ID;Nombre;Plan;FechaVencimiento;Estado");

            // Recorremos tus miembros reales usando sus propiedades exactas
            foreach (var member in members)
            {
                // Calculamos el estado actual dinámicamente
                string estado = member.ExpirationDate < DateTime.Today ? "VENCIDO" : "ACTIVO";

                // Construimos la línea usando punto y coma (;) como separador para Excel en español
                csvBuilder.AppendLine($"{member.Id};{member.Name};{member.MembershipPlan.GetType().Name};{member.ExpirationDate:yyyy-MM-dd};{estado}");
            }

            return csvBuilder.ToString();
        }
    }
}