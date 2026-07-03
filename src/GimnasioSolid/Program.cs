using System.Net;
using System.Text;
using GimnasioSolid.Controllers;
using GimnasioSolid.Memberships;
using GimnasioSolid.Models;
using GimnasioSolid.Repositories;
using GimnasioSolid.Scanners;
using GimnasioSolid.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IMemberRepository, MemberRepository>();
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddSingleton<BillingService>();
builder.Services.AddSingleton<AccessControl>(sp => new AccessControl(new QrCodeScanner()));
builder.Services.AddSingleton<TurnstileController>();

var app = builder.Build();
SeedMembers(app.Services.GetRequiredService<IMemberRepository>());

app.MapGet("/", (IMemberRepository members, IPaymentRepository payments, IAccessLogRepository accessLogs, BillingService billingService) =>
{
    var allMembers = members.GetAll().ToList();
    var totalRevenue = payments.GetAll().Sum(payment => payment.Amount);
    var monthlyProjection = allMembers.Sum(member => billingService.CalculateMonthlyFee(member));
    var allowedEntries = accessLogs.GetAll().Count(log => log.Allowed);

    var content =
        "<section class=\"hero\">" +
        "<div><span class=\"eyebrow\">Panel principal</span><h2>Gimnasio SOLID</h2><p>Administra socios, valida accesos y registra pagos desde un tablero limpio y directo.</p></div>" +
        "<div class=\"hero-actions\"><a class=\"primary-action\" href=\"/members\">Gestionar miembros</a><a class=\"secondary-action\" href=\"/access\">Validar acceso</a></div>" +
        "</section>" +
        "<section class=\"stats-grid\">" +
        StatCard("Miembros activos", allMembers.Count.ToString(), "Socios registrados") +
        StatCard("Ingresos registrados", Money(totalRevenue), "Pagos confirmados") +
        StatCard("Proyección mensual", Money(monthlyProjection), "Según planes actuales") +
        StatCard("Accesos permitidos", allowedEntries.ToString(), "Historial de validación") +
        "</section>" +
        "<section class=\"content-grid\">" +
        "<article class=\"panel\"><h3>Operaciones rápidas</h3><div class=\"quick-actions\"><a href=\"/members\">Nuevo miembro</a><a href=\"/access\">Control de puerta</a><a href=\"/billing\">Registrar pago</a></div></article>" +
        "<article class=\"panel\"><h3>Planes disponibles</h3><div class=\"plan-list\"><span>Estudiante · S/25.00</span><span>Regular · S/40.00</span><span>VIP · S/55.00</span><span>Fin de semana · S/20.00</span></div></article>" +
        "</section>";

    return Results.Content(PageLayout("Panel principal", "home", content), "text/html");
});

app.MapGet("/members", (IMemberRepository repository) =>
{
    var members = repository.GetAll().OrderBy(member => member.Name).ToList();
    var rows = new StringBuilder();

    foreach (var member in members)
    {
        rows.Append("<tr>");
        rows.Append($"<td><strong>{Enc(member.Id)}</strong></td>");
        rows.Append($"<td>{Enc(member.Name)}</td>");
        rows.Append($"<td><span class=\"badge\">{PlanDisplayName(member.MembershipPlan)}</span></td>");
        rows.Append($"<td>{member.ExpirationDate:dd/MM/yyyy}</td>");
        rows.Append("</tr>");
    }

    if (rows.Length == 0)
    {
        rows.Append("<tr><td colspan=\"4\" class=\"empty-state\">No hay miembros registrados.</td></tr>");
    }

    var content =
        "<section class=\"page-heading\"><span class=\"eyebrow\">Miembros</span><h2>Gestión de miembros</h2><p>Consulta socios activos y registra nuevas credenciales de acceso.</p></section>" +
        "<section class=\"content-grid two-columns\">" +
        "<article class=\"panel wide\"><div class=\"panel-title\"><h3>Listado de miembros</h3><span class=\"counter\">" + members.Count + " registrados</span></div>" +
        "<div class=\"table-wrap\"><table class=\"data-table\"><thead><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Expiración</th></tr></thead><tbody>" + rows + "</tbody></table></div></article>" +
        "<article class=\"panel\"><h3>Crear nuevo miembro</h3>" +
        "<form class=\"stacked-form\" method=\"post\" action=\"/members\">" +
        "<label>ID<input name=\"id\" required placeholder=\"Ej. E500\" /></label>" +
        "<label>Nombre<input name=\"name\" required placeholder=\"Nombre completo\" /></label>" +
        "<label>QR acceso<input name=\"accessKey\" required placeholder=\"Código QR\" /></label>" +
        "<label>Huella<input name=\"fingerprint\" required placeholder=\"Firma de huella\" /></label>" +
        "<label>Plan<select name=\"plan\"><option value=\"student\">Estudiante</option><option value=\"regular\">Regular</option><option value=\"vip\">VIP</option><option value=\"weekend\">Fin de semana</option></select></label>" +
        "<button type=\"submit\">Agregar miembro</button></form></article>" +
        "</section>";

    return Results.Content(PageLayout("Gestión de miembros", "members", content), "text/html");
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

app.MapGet("/access", (IMemberRepository members, IAccessLogRepository accessLogs) =>
{
    return Results.Content(PageLayout("Validación de acceso", "access", RenderAccessPage(accessLogs.GetAll(), members.GetAll())), "text/html");
});

app.MapPost("/access", async (HttpRequest request, AccessControl accessControl, IMemberRepository repo, IAccessLogRepository accessLogs) =>
{
    var form = await request.ReadFormAsync();
    var readerOutput = form["readerOutput"].ToString();
    var readerType = form["readerType"].ToString();
    var readerName = readerType == "fingerprint" ? "Huella biométrica" : "Código QR";
    IAccessScanner activeScanner = readerType == "fingerprint"
        ? new FingerprintScanner()
        : new QrCodeScanner();

    accessControl.SetScanner(activeScanner);
    var normalizedCredential = activeScanner.Scan(readerOutput);

    var member = repo.GetAll()
        .FirstOrDefault(m =>
            readerType == "qr"
                ? m.AccessKey == normalizedCredential
                : m.FingerprintSignature == normalizedCredential
        );

    var allowed = accessControl.CanOpenDoor(member, readerOutput);
    accessLogs.Save(new AccessLog(member?.Id, member?.Name, DateTime.Now, allowed, readerName, readerOutput));

    var result =
        "<section class=\"result-panel " + (allowed ? "allowed" : "denied") + "\">" +
        "<span class=\"status-dot\"></span>" +
        "<div><h2>" + (allowed ? "Acceso permitido" : "Acceso denegado") + "</h2>" +
        "<p>" + (allowed
            ? $"El lector {Enc(readerName)} devolvió una credencial válida para {Enc(member?.Name ?? "miembro")}."
            : $"El lector {Enc(readerName)} devolvió una credencial que no coincide con ningún socio activo.") + "</p></div>" +
        "</section>";

    return Results.Content(PageLayout("Resultado de acceso", "access", result + RenderAccessPage(accessLogs.GetAll(), repo.GetAll(), false)), "text/html");
});

app.MapGet("/billing", (IMemberRepository members, IPaymentRepository payments, BillingService billingService) =>
{
    var allMembers = members.GetAll().OrderBy(member => member.Name).ToList();
    var allPayments = payments.GetAll().OrderByDescending(payment => payment.Date).ToList();
    var memberRows = new StringBuilder();
    var paymentRows = new StringBuilder();

    foreach (var member in allMembers)
    {
        memberRows.Append("<tr>");
        memberRows.Append($"<td><strong>{Enc(member.Id)}</strong></td>");
        memberRows.Append($"<td>{Enc(member.Name)}</td>");
        memberRows.Append($"<td>{PlanDisplayName(member.MembershipPlan)}</td>");
        memberRows.Append($"<td>{Money(billingService.CalculateMonthlyFee(member))}</td>");
        memberRows.Append("</tr>");
    }

    foreach (var payment in allPayments)
    {
        paymentRows.Append("<tr>");
        paymentRows.Append($"<td>{payment.Date:dd/MM/yyyy HH:mm}</td>");
        paymentRows.Append($"<td>{Enc(payment.MemberName)}</td>");
        paymentRows.Append($"<td>{Enc(payment.PlanName)}</td>");
        paymentRows.Append($"<td>{Money(payment.Amount)}</td>");
        paymentRows.Append("</tr>");
    }

    if (paymentRows.Length == 0)
    {
        paymentRows.Append("<tr><td colspan=\"4\" class=\"empty-state\">Todavía no hay pagos registrados.</td></tr>");
    }

    var content =
        "<section class=\"page-heading\"><span class=\"eyebrow\">Facturación</span><h2>Pagos y reportes</h2><p>Revisa cuotas mensuales, registra pagos y consulta el historial financiero.</p></section>" +
        "<section class=\"stats-grid compact\">" +
        StatCard("Miembros", allMembers.Count.ToString(), "Con plan vigente") +
        StatCard("Pagos", allPayments.Count.ToString(), "Transacciones guardadas") +
        StatCard("Total cobrado", Money(allPayments.Sum(payment => payment.Amount)), "Ingresos registrados") +
        "</section>" +
        "<section class=\"content-grid two-columns\">" +
        "<article class=\"panel wide\"><h3>Miembros y tarifa mensual</h3><div class=\"table-wrap\"><table class=\"data-table\"><thead><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Cuota</th></tr></thead><tbody>" + memberRows + "</tbody></table></div></article>" +
        "<article class=\"panel\"><h3>Registrar pago</h3><form class=\"stacked-form\" method=\"post\" action=\"/billing/pay\"><label>ID del miembro<input name=\"memberId\" required placeholder=\"Ej. A100\" /></label><button type=\"submit\">Registrar pago</button></form></article>" +
        "</section>" +
        "<section class=\"panel\"><h3>Historial de pagos</h3><div class=\"table-wrap\"><table class=\"data-table\"><thead><tr><th>Fecha</th><th>Miembro</th><th>Plan</th><th>Importe</th></tr></thead><tbody>" + paymentRows + "</tbody></table></div></section>";

    return Results.Content(PageLayout("Facturación y reportes", "billing", content), "text/html");
});

app.MapGet("/access/qr", (string presentedData) =>
{
    return Results.Redirect($"/access?presentedData={Uri.EscapeDataString(presentedData)}&scanner=qr");
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

static string RenderAccessPage(IEnumerable<AccessLog> logs, IEnumerable<Member> members, bool includeHeading = true)
{
    var rows = new StringBuilder();
    var exampleRows = new StringBuilder();

    foreach (var member in members.OrderBy(member => member.Name))
    {
        exampleRows.Append("<tr>");
        exampleRows.Append($"<td>{Enc(member.Name)}</td>");
        exampleRows.Append($"<td><code>{Enc(member.AccessKey)}</code></td>");
        exampleRows.Append($"<td><code>{Enc(member.FingerprintSignature)}</code></td>");
        exampleRows.Append("</tr>");
    }

    if (exampleRows.Length == 0)
    {
        exampleRows.Append("<tr><td colspan=\"3\" class=\"empty-state\">No hay socios para simular credenciales.</td></tr>");
    }

    foreach (var log in logs.OrderByDescending(log => log.Timestamp).Take(8))
    {
        rows.Append("<tr>");
        rows.Append($"<td>{log.Timestamp:dd/MM/yyyy HH:mm}</td>");
        rows.Append($"<td>{Enc(log.MemberName ?? "No identificado")}</td>");
        rows.Append($"<td>{Enc(log.ScannerType)}</td>");
        rows.Append($"<td><span class=\"badge {(log.Allowed ? "success" : "danger")}\">{(log.Allowed ? "Permitido" : "Denegado")}</span></td>");
        rows.Append("</tr>");
    }

    if (rows.Length == 0)
    {
        rows.Append("<tr><td colspan=\"4\" class=\"empty-state\">Aún no hay validaciones registradas.</td></tr>");
    }

    var heading = includeHeading
        ? "<section class=\"page-heading\"><span class=\"eyebrow\">Prototipo de lectores</span><h2>Validación de acceso</h2><p>Panel para acceso mediante QR o lector biometrico de huella.</p></section>"
        : string.Empty;

    return heading +
        "<section class=\"content-grid two-columns\">" +
        "<article class=\"panel\"><h3>Simular lectura</h3><form class=\"stacked-form\" method=\"post\" action=\"/access\">" +
        "<label>Salida del lector<input name=\"readerOutput\" required placeholder=\"Ej. A100 o FP-A100\" /></label>" +
        "<label>Dispositivo simulado<select name=\"readerType\"><option value=\"qr\">Cámara / lector QR</option><option value=\"fingerprint\">Lector biométrico de huella</option></select></label>" +
        "<button type=\"submit\">Validar acceso</button></form></article>" +
        "<article class=\"panel wide\"><h3>Credenciales simuladas</h3><div class=\"table-wrap\"><table class=\"data-table compact-table\"><thead><tr><th>Socio</th><th>Salida QR</th><th>Salida huella</th></tr></thead><tbody>" + exampleRows + "</tbody></table></div></article>" +
        "</section>" +
        "<section class=\"panel\"><h3>Historial reciente</h3><div class=\"table-wrap\"><table class=\"data-table\"><thead><tr><th>Fecha</th><th>Miembro</th><th>Lector</th><th>Estado</th></tr></thead><tbody>" + rows + "</tbody></table></div></section>";
}

static string PageLayout(string title, string activeSection, string content)
{
    var navigation =
        NavLink("/", "Inicio", activeSection == "home") +
        NavLink("/members", "Miembros", activeSection == "members") +
        NavLink("/access", "Acceso", activeSection == "access") +
        NavLink("/billing", "Facturación", activeSection == "billing");

    return "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
        $"<title>{Enc(title)} | Gimnasio SOLID</title><style>{CssStyles()}</style></head>" +
        "<body><div class=\"app-shell\"><header><div class=\"topbar\">" +
        "<a class=\"brand\" href=\"/\"><span class=\"brand-mark\">GS</span><span><strong>Gimnasio SOLID</strong><span>Control operativo</span></span></a>" +
        $"<nav aria-label=\"Navegación principal\">{navigation}</nav></div></header><main>{content}</main></div></body></html>";
}

static string CssStyles()
{
    return """
        :root {
            color-scheme: light;
            --ink: #172033;
            --muted: #647084;
            --line: #dfe6f0;
            --surface: #ffffff;
            --surface-soft: #f6f8fb;
            --brand: #116149;
            --brand-dark: #0b4635;
            --accent: #d88b18;
            --danger: #b42318;
            --success: #138a54;
            --shadow: 0 18px 38px rgba(23, 32, 51, .08);
        }

        * { box-sizing: border-box; }

        body {
            margin: 0;
            min-height: 100vh;
            font-family: Inter, "Segoe UI", Arial, sans-serif;
            background: var(--surface-soft);
            color: var(--ink);
        }

        a { color: inherit; }

        header {
            background: var(--surface);
            border-bottom: 1px solid var(--line);
            position: sticky;
            top: 0;
            z-index: 10;
        }

        .topbar {
            max-width: 1180px;
            margin: 0 auto;
            padding: 16px 24px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 18px;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 12px;
            min-width: 210px;
            text-decoration: none;
        }

        .brand-mark {
            width: 42px;
            height: 42px;
            border-radius: 8px;
            display: grid;
            place-items: center;
            background: var(--brand);
            color: #fff;
            font-weight: 800;
        }

        .brand strong { display: block; font-size: 1rem; }
        .brand span span { color: var(--muted); font-size: .84rem; }

        nav {
            display: flex;
            flex-wrap: wrap;
            justify-content: flex-end;
            gap: 8px;
        }

        nav a {
            text-decoration: none;
            padding: 10px 13px;
            border-radius: 8px;
            color: var(--muted);
            font-weight: 700;
            font-size: .92rem;
        }

        nav a.active,
        nav a:hover {
            background: #eaf4ef;
            color: var(--brand-dark);
        }

        main {
            max-width: 1180px;
            margin: 0 auto;
            padding: 28px 24px 44px;
        }

        .hero {
            min-height: 260px;
            display: grid;
            grid-template-columns: minmax(0, 1.3fr) auto;
            align-items: end;
            gap: 28px;
            padding: 36px;
            border-radius: 8px;
            color: #fff;
            background:
                linear-gradient(110deg, rgba(11, 70, 53, .96), rgba(17, 97, 73, .78)),
                url("https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=1600&q=80") center/cover;
            box-shadow: var(--shadow);
        }

        .hero h2,
        .page-heading h2,
        .result-panel h2 {
            margin: 6px 0 10px;
            font-size: clamp(2rem, 5vw, 4rem);
            line-height: 1;
        }

        .hero p,
        .page-heading p,
        .result-panel p {
            margin: 0;
            color: inherit;
            max-width: 680px;
            line-height: 1.65;
        }

        .hero-actions,
        .quick-actions {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
        }

        .primary-action,
        .secondary-action,
        button,
        .quick-actions a {
            min-height: 44px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            border-radius: 8px;
            border: 0;
            padding: 0 16px;
            font-weight: 800;
            text-decoration: none;
            cursor: pointer;
        }

        .primary-action,
        button {
            background: var(--brand);
            color: #fff;
        }

        .primary-action:hover,
        button:hover { background: var(--brand-dark); }

        .secondary-action {
            background: rgba(255, 255, 255, .14);
            color: #fff;
            border: 1px solid rgba(255, 255, 255, .35);
        }

        .page-heading { padding: 16px 0 8px; }
        .page-heading h2 { color: var(--ink); }
        .page-heading p { color: var(--muted); }

        .eyebrow {
            display: inline-flex;
            color: var(--accent);
            font-size: .78rem;
            font-weight: 900;
            text-transform: uppercase;
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(4, minmax(0, 1fr));
            gap: 14px;
            margin-top: 18px;
        }

        .stats-grid.compact { grid-template-columns: repeat(3, minmax(0, 1fr)); }

        .stat-card,
        .panel {
            background: var(--surface);
            border: 1px solid var(--line);
            border-radius: 8px;
            box-shadow: var(--shadow);
        }

        .stat-card { padding: 18px; }

        .stat-card span,
        .counter {
            color: var(--muted);
            font-size: .84rem;
            font-weight: 700;
        }

        .stat-card strong {
            display: block;
            margin: 8px 0 4px;
            font-size: 1.65rem;
        }

        .content-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 18px;
            margin-top: 18px;
        }

        .content-grid.two-columns {
            grid-template-columns: minmax(0, 1.45fr) minmax(320px, .75fr);
        }

        .panel {
            padding: 22px;
            overflow: hidden;
        }

        .panel.wide { min-width: 0; }

        .panel-title {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
        }

        h3 {
            margin: 0 0 16px;
            font-size: 1.08rem;
        }

        .quick-actions a {
            background: #eef6f2;
            color: var(--brand-dark);
        }

        .plan-list {
            display: grid;
            gap: 10px;
            color: var(--muted);
            font-weight: 700;
        }

        .simulation-note {
            margin-top: 18px;
            border-left: 5px solid var(--accent);
            background: #fffaf1;
        }

        .simulation-note p {
            margin: 0;
            color: var(--muted);
            line-height: 1.65;
        }

        .stacked-form {
            display: grid;
            gap: 14px;
        }

        label {
            display: grid;
            gap: 7px;
            color: var(--ink);
            font-size: .9rem;
            font-weight: 800;
        }

        input,
        select {
            width: 100%;
            min-height: 44px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            padding: 0 12px;
            background: #fff;
            color: var(--ink);
            font: inherit;
        }

        input:focus,
        select:focus {
            outline: 3px solid rgba(17, 97, 73, .16);
            border-color: var(--brand);
        }

        .table-wrap {
            overflow-x: auto;
            border: 1px solid var(--line);
            border-radius: 8px;
        }

        .data-table {
            width: 100%;
            min-width: 620px;
            border-collapse: collapse;
            font-size: .94rem;
        }

        .compact-table {
            min-width: 480px;
        }

        .data-table th,
        .data-table td {
            padding: 13px 14px;
            text-align: left;
            border-bottom: 1px solid var(--line);
        }

        .data-table th {
            background: #f8fafc;
            color: var(--muted);
            font-size: .78rem;
            text-transform: uppercase;
        }

        .data-table tr:last-child td { border-bottom: 0; }

        code {
            display: inline-flex;
            align-items: center;
            min-height: 26px;
            padding: 0 8px;
            border-radius: 6px;
            background: #edf2f7;
            color: var(--brand-dark);
            font-family: Consolas, "Courier New", monospace;
            font-size: .88rem;
            font-weight: 700;
        }

        .badge {
            display: inline-flex;
            align-items: center;
            min-height: 28px;
            padding: 0 10px;
            border-radius: 999px;
            background: #eef2f7;
            color: var(--ink);
            font-size: .8rem;
            font-weight: 800;
        }

        .badge.success {
            background: #e6f6ee;
            color: var(--success);
        }

        .badge.danger {
            background: #fdeceb;
            color: var(--danger);
        }

        .result-panel {
            display: flex;
            gap: 16px;
            align-items: center;
            padding: 24px;
            border-radius: 8px;
            color: #fff;
            margin-bottom: 18px;
        }

        .result-panel.allowed { background: var(--success); }
        .result-panel.denied { background: var(--danger); }

        .status-dot {
            width: 16px;
            height: 16px;
            flex: 0 0 auto;
            border-radius: 999px;
            background: #fff;
            box-shadow: 0 0 0 8px rgba(255, 255, 255, .18);
        }

        .empty-state {
            color: var(--muted);
            text-align: center;
        }

        @media (max-width: 900px) {
            .topbar,
            .hero,
            .content-grid,
            .content-grid.two-columns {
                grid-template-columns: 1fr;
            }

            .topbar { align-items: flex-start; }
            nav { justify-content: flex-start; }

            .stats-grid,
            .stats-grid.compact {
                grid-template-columns: repeat(2, minmax(0, 1fr));
            }
        }

        @media (max-width: 560px) {
            .topbar,
            main {
                padding-left: 16px;
                padding-right: 16px;
            }

            .brand { min-width: 0; }

            nav a {
                flex: 1 1 calc(50% - 8px);
                text-align: center;
            }

            .hero {
                min-height: 320px;
                padding: 24px;
            }

            .stats-grid,
            .stats-grid.compact {
                grid-template-columns: 1fr;
            }
        }
        """;
}

static string NavLink(string href, string text, bool active)
{
    return $"<a class=\"{(active ? "active" : string.Empty)}\" href=\"{href}\">{text}</a>";
}

static string StatCard(string label, string value, string description)
{
    return $"<article class=\"stat-card\"><span>{Enc(label)}</span><strong>{Enc(value)}</strong><span>{Enc(description)}</span></article>";
}

static string PlanDisplayName(IMembershipPlan plan)
{
    return plan switch
    {
        StudentMembership => "Estudiante",
        RegularMembership => "Regular",
        VipMembership => "VIP",
        WeekendMembership => "Fin de semana",
        _ => plan.GetType().Name
    };
}

static string Money(decimal amount)
{
    return $"S/{amount:0.00}";
}

static string Enc(string value)
{
    return WebUtility.HtmlEncode(value);
}

static void SeedMembers(IMemberRepository repository)
{
    var members = new List<Member>
    {
        new Member("A100", "Aaron", new StudentMembership(), "A100", "FP-A100"),
        new Member("B200", "Angel", new VipMembership(), "B200", "FP-B200"),
        new Member("C300", "Luis", new WeekendMembership(), "C300", "FP-C300"),
        new Member("D400", "Sebastian", new RegularMembership(), "D400", "FP-D400")
    };

    foreach (var member in members)
    {
        repository.Save(member);
    }
}
