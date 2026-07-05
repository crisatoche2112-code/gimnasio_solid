using System;
using System.Collections.Generic;
using GimnasioSolid.Models;

namespace GimnasioSolid.Services
{
    public class GymAlertService : IAlertService
    {
        public List<string> CheckMembershipExpirations(IEnumerable<Member> members)
        {
            var alerts = new List<string>();
            DateTime hoy = DateTime.Today;

            foreach (var member in members)
            {
                if (member.ExpirationDate < hoy)
                {
                    alerts.Add($"[CRÍTICO] El miembro {member.Name} (ID: {member.Id}) tiene la membresía VENCIDA desde el {member.ExpirationDate:yyyy-MM-dd}.");
                }
                else if ((member.ExpirationDate - hoy).TotalDays <= 7)
                {
                    int diasRestantes = (int)(member.ExpirationDate - hoy).TotalDays;
                    alerts.Add($"[ADVERTENCIA] La membresía de {member.Name} (ID: {member.Id}) vencerá en {diasRestantes} días ({member.ExpirationDate:yyyy-MM-dd}).");
                }
            }

            return alerts;
        }
    }
}