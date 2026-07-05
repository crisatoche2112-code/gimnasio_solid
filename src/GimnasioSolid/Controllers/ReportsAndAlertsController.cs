using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Collections.Generic;
using GimnasioSolid.Services;
using GimnasioSolid.Repositories;

namespace GimnasioSolid.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsAndAlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly IReportService _reportService;
        private readonly IMemberRepository _memberRepository;

        public ReportsAndAlertsController(IAlertService alertService, IReportService reportService, IMemberRepository memberRepository)
        {
            _alertService = alertService;
            _reportService = reportService;
            _memberRepository = memberRepository;
        }

        [HttpGet("alerts")]
public IActionResult GetActiveAlerts()
{
    var realMembers = _memberRepository.GetAll();
    var alerts = _alertService.CheckMembershipExpirations(realMembers);

    var itemsBuilder = new StringBuilder();
    
    if (alerts.Count == 0)
    {
        itemsBuilder.Append("<div class='alert-item item-info'>No se encontraron alertas activas. Todos los miembros están al día.</div>");
    }
    else
    {
        foreach (var alert in alerts)
        {
            // Validamos de manera más segura si contiene la palabra clave para aplicar el estilo correcto
            bool isCritical = alert.Contains("CRITICO", StringComparison.OrdinalIgnoreCase) || 
                              alert.Contains("CRÍTICO", StringComparison.OrdinalIgnoreCase);

            string typeClass = isCritical ? "item-danger" : "item-warning";
            itemsBuilder.Append($"<div class='alert-item {typeClass}'>{alert}</div>");
        }
    }

    string htmlTemplate = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Alertas de Membresías</title>
        <style>
            body {{ font-family: Inter, system-ui, Segoe UI, sans-serif; background: #eef5ff; color: #102a43; margin: 0; padding: 40px 20px; }}
            .container {{ max-width: 800px; margin: auto; background: #fff; border: 1px solid #e2e8f0; border-radius: 22px; padding: 32px; box-shadow: 0 18px 40px rgba(16,42,67,.08); }}
            h2 {{ color: #102a43; margin-top: 0; margin-bottom: 24px; border-bottom: 2px solid #e2e8f0; padding-bottom: 12px; }}
            .alert-item {{ padding: 16px 20px; border-radius: 12px; margin-bottom: 14px; font-size: 1rem; line-height: 1.5; font-weight: 500; border-left: 6px solid; }}
            .item-danger {{ background: #fff1f2; color: #991b1b; border-color: #f43f5e; }}
            .item-warning {{ background: #fffbeb; color: #92400e; border-color: #f59e0b; }}
            .item-info {{ background: #f0fdf4; color: #166534; border-color: #22c55e; }}
            .actions-layout {{ margin-top: 24px; display: flex; gap: 12px; }}
            .btn {{ display: inline-block; padding: 10px 20px; text-decoration: none; font-weight: 600; font-size: 0.95rem; border-radius: 10px; transition: background 0.15s; }}
            .btn-primary {{ background: #0f4fa8; color: #fff; }}
            .btn-primary:hover {{ background: #0b3d85; }}
            .btn-secondary {{ background: #f1f5f9; color: #475569; }}
            .btn-secondary:hover {{ background: #e2e8f0; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <h2>Panel de Alertas de Control de Socios</h2>
            {itemsBuilder}
            <div class='actions-layout'>
                <a href='/' class='btn btn-primary'>Regresar al Menú</a>
                <a href='javascript:window.close();' class='btn btn-secondary'>Cerrar Ventana</a>
            </div>
        </div>
    </body>
    </html>";

    return Content(htmlTemplate, "text/html", Encoding.UTF8);
}

        [HttpGet("download-csv")]
        public IActionResult DownloadCsvReport()
        {
            var realMembers = _memberRepository.GetAll();
            var csvString = _reportService.GenerateMembersCsv(realMembers);
            
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            return File(csvBytes, "text/csv", "reporte_socios.csv");
        }
    }
}