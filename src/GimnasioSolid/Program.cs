using System.Text;
using GimnasioSolid.Controllers;
using GimnasioSolid.Memberships;
using GimnasioSolid.Models;
using GimnasioSolid.Repositories;
using GimnasioSolid.Scanners;
using GimnasioSolid.Services;

var builder = WebApplication.CreateBuilder(args);

// Repositorios originales
builder.Services.AddSingleton<IMemberRepository, MemberRepository>();
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddSingleton<BillingService>();
builder.Services.AddSingleton<AccessControl>(sp => new AccessControl(new QrCodeScanner()));
builder.Services.AddSingleton<TurnstileController>();

// === TUS NUEVOS SERVICIOS CON PRINCIPIOS SOLID ===
builder.Services.AddControllers(); // Habilita los controladores como el de reportes
builder.Services.AddScoped<IAlertService, GymAlertService>();
builder.Services.AddScoped<IReportService, CsvReportService>();

var app = builder.Build();

// Mapea las rutas automáticas de tus controladores (como /api/reportsandalerts/alerts)
app.MapControllers();

SeedMembers(app.Services.GetRequiredService<IMemberRepository>());

// MENÚ PRINCIPAL: Se agregaron los dos nuevos botones para Alertas y Reporte CSV
app.MapGet("/", () => Results.Content(PageLayout("Gimnasio SOLID", 
    "<a class=\"button\" href=\"/members\">Gestión de miembros</a>" +
    "<a class=\"button\" href=\"/access\">Validación de acceso</a>" +
    "<a class=\"button\" href=\"/billing\">Facturación y reportes</a>" +
    "<a class=\"button\" style=\"background:#dc3545;\" href=\"/api/reportsandalerts/alerts\" target=\"_blank\">⚠️ Ver Alertas</a>" +
    "<a class=\"button\" style=\"background:#28a745;\" href=\"/api/reportsandalerts/download-csv\">📊 Reporte CSV</a>", 
    "<section class=\"hero\"><p>Bienvenido al sistema web de gestión del gimnasio. Aquí puedes administrar miembros, validar accesos con QR o huella y llevar el control de pagos.</p><div class=\"hero-actions\"><a class=\"button\" href=\"/members\">Ver miembros</a><a class=\"button\" href=\"/access\">Validar acceso</a><a class=\"button\" href=\"/billing\">Ver facturación</a></div></section>"), "text/html"));

app.MapGet("/members", (IMemberRepository repository) =>
{
    var rows = new StringBuilder();
    foreach (var member in repository.GetAll())
    {
        rows.Append($"<tr><td>{member.Id}</td><td>{member.Name}</td><td>{member.MembershipPlan.GetType().Name}</td><td>{member.ExpirationDate:yyyy-MM-dd}</td></tr>");
    }

    var content = $"<h2>Gestión de miembros</h2><p class=\"summary\">Miembros registrados: {repository.GetAll().Count()}</p><section class=\"card\"><h3>Listado de miembros</h3><table class=\"data-table\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Expiración</th></tr>{rows}</table></section>" +
                  "<section class=\"card\"><h3>Crear nuevo miembro</h3><p>Completa los datos del nuevo socio. El campo <strong>QR acceso</strong> se usará para validar con el lector QR, y el campo <strong>Huella</strong> se usará para validar con el lector de huella.</p>" +
                  "<form method=\"post\" action=\"/members\">" +
                  "<label>ID:<input name=\"id\" required /></label>" +
                  "<label>Nombre:<input name=\"name\" required /></label>" +
                  "<label>QR acceso:<input name=\"accessKey\" required /></label>" +
                  "<label>Huella:<input name=\"fingerprint\" required /></label>" +
                  "<label>Plan:<select name=\"plan\"><option value=\"student\">Estudiante</option><option value=\"regular\">Regular</option><option value=\"vip\">VIP</option><option value=\"weekend\">Fin de semana</option></select></label>" +
                  "<button type=\"submit\">Agregar miembro</button></form></section>";

    return Results.Content(PageLayout("Gestión de miembros", "<a class=\"button\" href=\"/\">Menú principal</a>", content), "text/html");
});

app.MapPost("/members", async (HttpRequest request, IMemberRepository repository) =>
{
    var form = await request.ReadFormAsync();
    var id = form["id"].ToString();
    var name = form["name"].ToString();
    var accessKey = form["accessKey"].ToString();
    var fingerprint = form["fingerprint"].ToString();
    var planValue = form["plan"].ToString();

    IMembershipPlan plan = planValue switch
    {
        "student" => new StudentMembership(),
        "regular" => new RegularMembership(),
        "vip" => new VipMembership(),
        "weekend" => new WeekendMembership(),
        _ => new RegularMembership()
    };

    repository.Save(new Member(id, name, plan, accessKey, fingerprint));
    return Results.Redirect("/members");
});

app.MapGet("/access", () => Results.Content(PageLayout("Validación de acceso", "<a class=\"button\" href=\"/\">Menú principal</a>", "<h2>Validación de acceso</h2><p>Valida el ingreso de un miembro usando su credencial guardada. Elige el tipo de lector y presenta el dato correspondiente.</p><section class=\"info-box\"><strong>Cómo funciona:</strong><ul><li>Para <strong>QR</strong>: ingresa el valor exacto de <strong>QR acceso</strong> del miembro.</li><li>Para <strong>Huella</strong>: ingresa el valor de <strong>Huella</strong>. No importa si lo escribes en minúsculas.</li><li>El sistema busca el miembro y verifica si la credencial coincide con sus datos.</li></ul></section><form method=\"post\" action=\"/access\"><label>Datos presentados:<input name=\"presentedData\" required /></label><label>Lector:<select name=\"scanner\"><option value=\"qr\">QR</option><option value=\"fingerprint\">Huella</option></select></label><button type=\"submit\">Validar acceso</button></form>"), "text/html"));

app.MapPost("/access", async (HttpRequest request, TurnstileController turnstile, AccessControl accessControl) =>
{
    var form = await request.ReadFormAsync();
    var presentedData = form["presentedData"].ToString();
    var scanner = form["scanner"].ToString();

    if (scanner == "qr")
    {
        accessControl.SetScanner(new QrCodeScanner());
    }
    else
    {
        accessControl.SetScanner(new FingerprintScanner());
    }

    var allowed = turnstile.ValidateEntry(presentedData);
    var message = allowed ? "Acceso permitido" : "Acceso denegado";
    var content = $"<h2>Resultado de acceso</h2><p>{message}</p><p><a href=\"/access\">Probar otro acceso</a></p>";
    return Results.Content(PageLayout("Validación de acceso", "<a class=\"button\" href=\"/\">Menú principal</a>", content), "text/html");
});

app.MapGet("/billing", (IMemberRepository members, IPaymentRepository payments, BillingService billingService) =>
{
    var memberRows = new StringBuilder();
    foreach (var member in members.GetAll())
    {
        memberRows.Append($"<tr><td>{member.Id}</td><td>{member.Name}</td><td>{member.MembershipPlan.GetType().Name}</td><td>${billingService.CalculateMonthlyFee(member):0.00}</td></tr>");
    }

    var paymentRows = new StringBuilder();
    foreach (var payment in payments.GetAll())
    {
        paymentRows.Append($"<tr><td>{payment.Date:yyyy-MM-dd HH:mm}</td><td>{payment.MemberName}</td><td>{payment.PlanName}</td><td>${payment.Amount:0.00}</td></tr>");
    }

    var content = "<h2>Facturación y reportes</h2>" +
                  $"<p class=\"summary\">Miembros registrados: {members.GetAll().Count()} • Pagos realizados: {payments.GetAll().Count()}</p><p>Consulta las tarifas mensuales de cada miembro y registra pagos con facilidad.</p>" +
                  "<section class=\"card\"><h3>Miembros y tarifa mensual</h3>" +
                  $"<table class=\"data-table\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Cuota</th></tr>{memberRows}</table></section>" +
                  "<section class=\"card\"><h3>Registrar pago</h3>" +
                  "<form method=\"post\" action=\"/billing/pay\"><label>ID del miembro:<input name=\"memberId\" required /></label><button type=\"submit\">Registrar pago</button></form></section>" +
                  "<section class=\"card\"><h3>Historial de pagos</h3>" +
                  $"<table class=\"data-table\"><tr><th>Fecha</th><th>Miembro</th><th>Plan</th><th>Importe</th></tr>{paymentRows}</table></section>";

    return Results.Content(PageLayout("Facturación y reportes", "<a class=\"button\" href=\"/\">Menú principal</a>", content), "text/html");
});

app.MapPost("/billing/pay", async (HttpRequest request, IMemberRepository members, BillingService billingService) =>
{
    var form = await request.ReadFormAsync();
    var memberId = form["memberId"].ToString();
    var member = members.GetAll().FirstOrDefault(m => m.Id.Equals(memberId, StringComparison.OrdinalIgnoreCase));
    if (member is null)
    {
        return Results.Redirect("/billing");
    }

    billingService.RegisterMonthlyPayment(member);
    return Results.Redirect("/billing");
});

app.Run();

static string PageLayout(string title, string navigation, string content)
{
    return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>{title}</title><style>body{{font-family:Inter,system-ui,Segoe UI,Arial,sans-serif;background:linear-gradient(180deg,#eef5ff 0%,#f8fbff 100%);color:#102a43;margin:0;padding:0;}}header{{background:#0f4fa8;color:#fff;padding:24px 28px;box-shadow:0 20px 50px rgba(15,79,168,.18);}}header h1{{margin:0;font-size:2rem;letter-spacing:.02em;}}nav{{padding:18px 28px;background:#fff;border-bottom:1px solid #d9e4f5;display:flex;flex-wrap:wrap;gap:10px;}}nav a.button{{display:inline-flex;align-items:center;justify-content:center;padding:12px 18px;background:#0f4fa8;color:#fff;text-decoration:none;border-radius:12px;font-weight:600;transition:transform .15s ease,background .15s ease;}}nav a.button:hover{{transform:translateY(-1px);background:#0b3d85;}}main{{padding:28px;max-width:1100px;margin:auto;}}h2{{color:#102a43;margin-bottom:12px;}}h3{{color:#102a43;margin-bottom:10px;}}p{{line-height:1.75;margin:0 0 18px;}}section.card{{background:#fff;border:1px solid #e2e8f0;border-radius:22px;padding:24px;box-shadow:0 18px 40px rgba(16,42,67,.08);margin-top:22px;}}.hero{{background:#f1f7ff;border:1px solid #dce7fb;border-radius:24px;padding:28px;margin-top:22px;}}.hero-actions{{margin-top:20px;display:flex;flex-wrap:wrap;gap:12px;}}.data-table{{width:100%;border-collapse:collapse;margin-top:16px;font-size:.98rem;}}.data-table th,.data-table td{{border:1px solid #e2e8f0;padding:14px;text-align:left;}}.data-table th{{background:#f1f5f9;color:#1e3a8a;}}label{{display:block;margin-bottom:16px;font-weight:700;color:#334155;}}input[type=text],select{{width:100%;padding:14px 16px;margin-top:8px;border:1px solid #cbd5e1;border-radius:14px;font-size:1rem;background:#f8fafc;}}button{{cursor:pointer;background:#0f4fa8;color:#fff;border:none;padding:14px 20px;border-radius:14px;font-size:1rem;font-weight:700;box-shadow:0 14px 30px rgba(15,79,168,.16);transition:transform .15s ease,background .15s ease;}}button:hover{{transform:translateY(-1px);background:#0b3d85;}}.summary{{margin-top:0;font-weight:700;color:#334155;}}.info-box{{background:#eef6ff;border-left:5px solid #0f4fa8;border-radius:14px;padding:18px 20px;margin-bottom:22px;color:#102a43;}}ul{{margin:12px 0 0 20px;padding:0;}}ul li{{margin-bottom:10px;}}@media(max-width:720px){{main{{padding:18px;}}nav{{justify-content:center;}}section.card, .hero{{padding:20px;}}input[type=text],select,button{{font-size:.98rem;}}}}</style></head><body><header><h1>{title}</h1></header><nav>{navigation}</nav><main>{content}</main></body></html>";
}

static void SeedMembers(IMemberRepository repository)
{
    var member1 = new Member("A100", "Aaron", new StudentMembership(), "A100", "FP-A100", DateTime.Today.AddDays(-5));
    
    var member2 = new Member("B200", "Angel", new VipMembership(), "B200", "FP-B200", DateTime.Today.AddDays(3));
    
    var member3 = new Member("C300", "Luis", new WeekendMembership(), "C300", "FP-C300");
    var member4 = new Member("D400", "Sebastian", new RegularMembership(), "D400", "FP-D400");

    repository.Save(member1);
    repository.Save(member2);
    repository.Save(member3);
    repository.Save(member4);
}