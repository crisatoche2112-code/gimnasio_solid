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
            return Ok(alerts);
        }

        [HttpGet("download-csv")]
        public IActionResult DownloadCsvReport()
        {
            var realMembers = _memberRepository.GetAll();
            var csvString = _reportService.GenerateMembersCsv(realMembers);
            
            // Convertimos la cadena de texto a bytes de forma segura aquí mismo
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            return File(csvBytes, "text/csv", "reporte_socios.csv");
        }
    }
}