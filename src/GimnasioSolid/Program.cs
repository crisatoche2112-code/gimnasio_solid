using System.Net;
using System.Text;
using System.Net;
using GimnasioSolid.Controllers;
using GimnasioSolid.Memberships;
using GimnasioSolid.Models;
using GimnasioSolid.Repositories;
using GimnasioSolid.Scanners;
using GimnasioSolid.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IMemberRepository, MemberRepository>();
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IBillingService, BillingService>();
builder.Services.AddSingleton<IReceiptPdfGenerator, ReceiptPdfGenerator>();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddSingleton<LoginController>();
builder.Services.AddSingleton<AccessControl>(sp => new AccessControl(new QrCodeScanner()));
builder.Services.AddSingleton<TurnstileController>();
builder.Services.AddScoped<IAlertService, GymAlertService>();
builder.Services.AddScoped<IReportService, CsvReportService>();

var app = builder.Build();

app.MapControllers();

SeedMembers(app.Services.GetRequiredService<IMemberRepository>());
SeedUsers(app.Services.GetRequiredService<IUserRepository>(), app.Services.GetRequiredService<AuthenticationService>());

app.MapGet("/", () =>
{
    var navigation =
        Button("/members", "Gestion de miembros") +
        Button("/access", "Validacion de acceso") +
        Button("/billing", "Facturacion y reportes") +
        Button("/login", "Login") +
        Button("/api/reportsandalerts/alerts", "Ver alertas", "background:#dc3545;", true) +
        Button("/api/reportsandalerts/download-csv", "Reporte CSV", "background:#28a745;");

    var content =
        "<section class=\"hero\"><p>Bienvenido al sistema web de gestion del gimnasio. Aqui puedes administrar miembros, validar accesos con QR o huella, llevar el control de pagos, revisar alertas y descargar reportes.</p>" +
        "<div class=\"hero-actions\">" +
        Button("/members", "Ver miembros") +
        Button("/access", "Validar acceso") +
        Button("/billing", "Ver facturacion") +
        Button("/login", "Login") +
        "</div></section>";

    return Results.Content(PageLayout("Gimnasio SOLID", navigation, content), "text/html");
});

app.MapGet("/login", () =>
{
    var content =
        "<h2>Iniciar sesion</h2><section class=\"card\"><form method=\"post\" action=\"/login\">" +
        "<label>Usuario:<input name=\"username\" required /></label>" +
        "<label>Contrasena:<input name=\"password\" type=\"password\" required /></label>" +
        "<button type=\"submit\">Ingresar</button></form>" +
        "<p><a href=\"/register\">No tienes cuenta? Registrate aqui</a></p></section>";

    return Results.Content(PageLayout("Login", BackHome(), content), "text/html");
});

app.MapPost("/login", async (HttpRequest request, LoginController loginController) =>
{
    var form = await request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var user = loginController.Login(username, password);

    if (user is null)
    {
        var errorContent = "<h2>Error de login</h2><p>Usuario o contrasena incorrectos.</p><p><a href=\"/login\">Volver a intentar</a></p>";
        return Results.Content(PageLayout("Error de Login", BackHome(), errorContent), "text/html");
    }

    var successContent = $"<h2>Bienvenido, {Enc(user.Username)}!</h2><p>Tu rol es: <strong>{Enc(user.Role.ToString())}</strong></p><p><a href=\"/\">Ir al menu principal</a></p>";
    return Results.Content(PageLayout("Login exitoso", BackHome(), successContent), "text/html");
});

app.MapGet("/register", () =>
{
    var content =
        "<h2>Crear nueva cuenta</h2><section class=\"card\"><form method=\"post\" action=\"/register\">" +
        "<label>ID:<input name=\"id\" required /></label>" +
        "<label>Usuario:<input name=\"username\" required /></label>" +
        "<label>Email:<input name=\"email\" type=\"email\" required /></label>" +
        "<label>Contrasena:<input name=\"password\" type=\"password\" required /></label>" +
        "<label>Rol:<select name=\"role\"><option value=\"Member\">Miembro</option><option value=\"Staff\">Personal</option><option value=\"Manager\">Gerente</option><option value=\"Admin\">Administrador</option></select></label>" +
        "<button type=\"submit\">Registrarse</button></form><p><a href=\"/login\">Ya tienes cuenta? Inicia sesion</a></p></section>";

    return Results.Content(PageLayout("Registrarse", BackHome(), content), "text/html");
});

app.MapPost("/register", async (HttpRequest request, LoginController loginController) =>
{
    var form = await request.ReadFormAsync();
    var id = form["id"].ToString();
    var username = form["username"].ToString();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var roleValue = form["role"].ToString();

    if (!Enum.TryParse<Role>(roleValue, out var role))
    {
        role = Role.Member;
    }

    try
    {
        loginController.Register(id, username, email, password, role);
        var successContent = $"<h2>Registro exitoso</h2><p>Cuenta creada para <strong>{Enc(username)}</strong>.</p><p><a href=\"/login\">Inicia sesion aqui</a></p>";
        return Results.Content(PageLayout("Registro completado", BackHome(), successContent), "text/html");
    }
    catch (InvalidOperationException ex)
    {
        var errorContent = $"<h2>Error en el registro</h2><p>{Enc(ex.Message)}</p><p><a href=\"/register\">Intentar de nuevo</a></p>";
        return Results.Content(PageLayout("Error de Registro", BackHome(), errorContent), "text/html");
    }
});

app.MapGet("/members", (IMemberRepository repository, string? query) =>
{
    var filteredMembers = string.IsNullOrWhiteSpace(query)
        ? repository.GetAll()
        :repository.GetAll().Where(member => 
        member.Id.Contains(query,
        StringComparison.OrdinalIgnoreCase)||
        member.Name.Contains(query,
        StringComparison.OrdinalIgnoreCase));


    var rows = new StringBuilder();
    foreach (var member in filteredMembers)
    foreach (var member in repository.GetAll().OrderBy(member => member.Name))
    {
        rows.Append($"<tr><td>{member.Id}</td><td>{member.Name}</td><td>{member.MembershipPlan.GetType().Name}</td><td>{member.ExpirationDate:yyyy-MM-dd}</td><td><a class=\"button small\" href=\"/members/edit/{member.Id}\">Editar</a> <form method=\"post\" action=\"/members/delete\" style=\"display:inline;margin:0;\"><input type=\"hidden\" name=\"id\" value=\"{member.Id}\" /><button type=\"submit\" class=\"button danger\" onclick=\"return confirm('¿Eliminar miembro {member.Name}?');\">Eliminar</button></form></td></tr>");
        rows.Append("<tr>");
        rows.Append($"<td><strong>{Enc(member.Id)}</strong></td>");
        rows.Append($"<td>{Enc(member.Name)}</td>");
        rows.Append($"<td>{PlanDisplayName(member.MembershipPlan)}</td>");
        rows.Append($"<td>{member.ExpirationDate:yyyy-MM-dd}</td>");
        rows.Append("</tr>");
    }

    if (rows.Length == 0)
    {
        rows.Append("<tr><td colspan=\"4\" class=\"empty-state\">No hay miembros registrados.</td></tr>");
    }

    var encodedQuery = WebUtility.HtmlEncode(query ?? string.Empty);
    var searchInfo = string.IsNullOrWhiteSpace(query) 
    ? string.Empty 
    : $"<p>Resultados de búsqueda para: <strong>{encodedQuery}</strong></p>";

    var content = $"<h2>Gestión de miembros</h2><p class=\"summary\">Miembros registrados: {repository.GetAll().Count()}</p>" +
                  "<section class=\"card\"><h3>Buscar miembro</h3>" +
                  $"<form method=\"get\" action=\"/members\"><label>Buscar por ID o nombre:<input name=\"query\" value=\"{encodedQuery}\" /></label><button type=\"submit\">Buscar</button></form>{searchInfo}</section>" +
                  "<section class=\"card\"><h3>Listado de miembros</h3>" +
                  $"<table class=\"data-table\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Expiración</th><th>Acciones</th></tr>{rows}</table></section>" +
                  "<section class=\"card\"><h3>Crear nuevo miembro</h3> ... </section>";

    return Results.Content(PageLayout("Gestión de miembros", "<a class=\"button\" href=\"/\">Menú principal</a>", content), "text/html");

    /*var content = $"<h2>Gestión de miembros</h2><p class=\"summary\">Miembros registrados: {repository.GetAll().Count()}</p><section class=\"card\"><h3>Listado de miembros</h3><table class=\"data-table\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Expiración</th></tr>{rows}</table></section>" +
                  "<section class=\"card\"><h3>Crear nuevo miembro</h3><p>Completa los datos del nuevo socio. El campo <strong>QR acceso</strong> se usará para validar con el lector QR, y el campo <strong>Huella</strong> se usará para validar con el lector de huella.</p>" +
                  "<form method=\"post\" action=\"/members\">" +
                  "<label>ID:<input name=\"id\" required /></label>" +
                  "<label>Nombre:<input name=\"name\" required /></label>" +
                  "<label>QR acceso:<input name=\"accessKey\" required /></label>" +
                  "<label>Huella:<input name=\"fingerprint\" required /></label>" +
                  "<label>Plan:<select name=\"plan\"><option value=\"student\">Estudiante</option><option value=\"regular\">Regular</option><option value=\"vip\">VIP</option><option value=\"weekend\">Fin de semana</option></select></label>" +
                  "<button type=\"submit\">Agregar miembro</button></form></section>";
    var content =
        $"<h2>Gestion de miembros</h2><p class=\"summary\">Miembros registrados: {repository.GetAll().Count()}</p>" +
        "<section class=\"card\"><h3>Listado de miembros</h3><table class=\"data-table\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Expiracion</th></tr>" + rows + "</table></section>" +
        "<section class=\"card\"><h3>Crear nuevo miembro</h3><p>Completa los datos del nuevo socio. QR acceso se usara para validar con lector QR y Huella para lector biometrico.</p>" +
        "<form method=\"post\" action=\"/members\">" +
        "<label>ID:<input name=\"id\" required /></label>" +
        "<label>Nombre:<input name=\"name\" required /></label>" +
        "<label>QR acceso:<input name=\"accessKey\" required /></label>" +
        "<label>Huella:<input name=\"fingerprint\" required /></label>" +
        "<label>Plan:<select name=\"plan\"><option value=\"student\">Estudiante</option><option value=\"regular\">Regular</option><option value=\"vip\">VIP</option><option value=\"weekend\">Fin de semana</option></select></label>" +
        "<button type=\"submit\">Agregar miembro</button></form></section>";

    return Results.Content(PageLayout("Gestión de miembros", "<a class=\"button\" href=\"/\">Menú principal</a>", content), "text/html");
*/
    return Results.Content(PageLayout("Gestion de miembros", BackHome(), content), "text/html");
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

app.MapGet("/access", () =>
{
    var content =
        "<h2>Validacion de acceso</h2><p>Valida el ingreso de un miembro usando su credencial guardada.</p>" +
        "<section class=\"info-box\"><strong>Como funciona:</strong><ul><li>Para QR: ingresa el valor exacto de QR acceso.</li><li>Para Huella: ingresa el valor de Huella.</li></ul></section>" +
        "<form method=\"post\" action=\"/access\"><label>Datos presentados:<input name=\"presentedData\" required /></label>" +
        "<label>Lector:<select name=\"scanner\"><option value=\"qr\">QR</option><option value=\"fingerprint\">Huella</option></select></label>" +
        "<button type=\"submit\">Validar acceso</button></form>";

    return Results.Content(PageLayout("Validacion de acceso", BackHome(), content), "text/html");
});

app.MapPost("/access", async (HttpRequest request, TurnstileController turnstile, AccessControl accessControl) =>
{
    var form = await request.ReadFormAsync();
    var presentedData = form["presentedData"].ToString();
    var scanner = form["scanner"].ToString();

    accessControl.SetScanner(scanner == "qr" ? new QrCodeScanner() : new FingerprintScanner());

    var allowed = turnstile.ValidateEntry(presentedData);
    var message = allowed ? "Acceso permitido" : "Acceso denegado";
    var content = $"<h2>Resultado de acceso</h2><p>{message}</p><p><a href=\"/access\">Probar otro acceso</a></p>";
    return Results.Content(PageLayout("Validacion de acceso", BackHome(), content), "text/html");
});

app.MapGet("/billing", (HttpRequest request, IMemberRepository members, IPaymentRepository payments, IBillingService billingService) =>
{
    var memberRows = new StringBuilder();
    foreach (var member in members.GetAll().OrderBy(member => member.Name))
    {
        var statusBadge = member.IsOverdue
            ? "<span class=\"badge badge-danger\">Vencido</span>"
            : "<span class=\"badge badge-ok\">Activo</span>";
        var fee = billingService.CalculateMonthlyFee(member);
        var rowClass = member.IsOverdue ? "member-row row-overdue" : "member-row";
        var statusValue = member.IsOverdue ? "overdue" : "active";
        var planType = member.MembershipPlan.GetType().Name;

        memberRows.Append("<tr class=\"" + rowClass + "\" data-id=\"" + Enc(member.Id) + "\" data-name=\"" + Enc(member.Name.ToLowerInvariant()) + "\" data-plan=\"" + planType + "\" data-status=\"" + statusValue + "\" data-expiration=\"" + member.ExpirationDate.ToString("yyyy-MM-dd") + "\" onclick=\"selectMember('" + Enc(member.Id) + "')\">");
        memberRows.Append($"<td>{Enc(member.Id)}</td><td>{Enc(member.Name)}</td><td>{PlanDisplayName(member.MembershipPlan)}</td><td>{statusBadge}</td><td>{member.ExpirationDate:yyyy-MM-dd}</td><td>{Money(fee)}</td></tr>");
    }

    var paymentRows = new StringBuilder();
    foreach (var payment in payments.GetAll().OrderByDescending(payment => payment.Date))
    {
        var lateFeeText = payment.LateFee > 0 ? Money(payment.LateFee) : "-";
        var receipt = Enc(payment.ReceiptNumber);
        var downloadLink = $"<a class=\"link-download\" href=\"/billing/receipt/{receipt}\" target=\"_blank\">Descargar PDF</a>";
        paymentRows.Append($"<tr><td>{payment.Date:yyyy-MM-dd HH:mm}</td><td>{Enc(payment.MemberName)}</td><td>{Enc(payment.PlanName)}</td><td>{Money(payment.BaseAmount)}</td><td>{lateFeeText}</td><td>{Money(payment.Amount)}</td><td>{Enc(payment.PaymentMethod)}</td><td>{receipt}</td><td>{downloadLink}</td></tr>");
    }

    if (paymentRows.Length == 0)
    {
        paymentRows.Append("<tr><td colspan=\"9\" class=\"empty-state\">Todavia no hay pagos registrados.</td></tr>");
    }

    var banner = "";
    var status = request.Query["status"].ToString();
    if (status == "ok")
    {
        var amount = Enc(request.Query["amount"].ToString());
        var receipt = Enc(request.Query["receipt"].ToString());
        banner = $"<section class=\"info-box\"><strong>Pago registrado correctamente.</strong> Comprobante {receipt} por S/{amount}. La membresia fue renovada. <a class=\"link-download\" href=\"/billing/receipt/{receipt}\" target=\"_blank\">Descargar comprobante en PDF</a></section>";
    }
    else if (status == "notfound")
    {
        banner = "<section class=\"info-box\"><strong>No se encontro ningun miembro con ese ID.</strong> Verifica el dato e intentalo nuevamente.</section>";
    }

    var filterBar =
        "<div class=\"filter-bar\">" +
        "<input type=\"text\" id=\"filterText\" placeholder=\"Buscar por ID o nombre...\" oninput=\"filterMembers()\" />" +
        "<select id=\"filterPlan\" onchange=\"filterMembers()\"><option value=\"\">Todos los planes</option><option value=\"StudentMembership\">Estudiante</option><option value=\"RegularMembership\">Regular</option><option value=\"VipMembership\">VIP</option><option value=\"WeekendMembership\">Fin de semana</option></select>" +
        "<select id=\"filterStatus\" onchange=\"filterMembers()\"><option value=\"\">Todos los estados</option><option value=\"active\">Activo</option><option value=\"overdue\">Vencido</option></select>" +
        "<label class=\"filter-date\">Vence desde:<input type=\"date\" id=\"filterFrom\" onchange=\"filterMembers()\" /></label>" +
        "<label class=\"filter-date\">Vence hasta:<input type=\"date\" id=\"filterTo\" onchange=\"filterMembers()\" /></label>" +
        "<button type=\"button\" class=\"button-secondary\" onclick=\"clearMemberFilters()\">Limpiar filtros</button></div>";

    var content =
        "<h2>Facturacion y reportes</h2>" + banner +
        $"<p class=\"summary\">Miembros registrados: {members.GetAll().Count()} - Pagos realizados: {payments.GetAll().Count()}</p>" +
        "<section class=\"card\"><h3>Miembros, estado y tarifa mensual</h3>" + filterBar +
        $"<table class=\"data-table\" id=\"membersTable\"><tr><th>ID</th><th>Nombre</th><th>Plan</th><th>Estado</th><th>Vence</th><th>Cuota a pagar</th></tr>{memberRows}</table>" +
        "<p id=\"noResults\" class=\"no-results\" style=\"display:none;\">No hay miembros que coincidan con los filtros.</p></section>" +
        "<section class=\"card\"><h3>Registrar pago</h3><form method=\"post\" action=\"/billing/pay\">" +
        "<label>ID del miembro:<input id=\"memberIdInput\" name=\"memberId\" required /></label>" +
        "<label>Tipo de pago:<select name=\"paymentMethod\"><option value=\"Efectivo\">Efectivo</option><option value=\"Tarjeta\">Tarjeta</option><option value=\"Billetera digital\">Billetera digital</option></select></label>" +
        "<button type=\"submit\">Registrar pago</button></form></section>" +
        $"<section class=\"card\"><h3>Historial de pagos</h3><table class=\"data-table\"><tr><th>Fecha</th><th>Miembro</th><th>Plan</th><th>Cuota</th><th>Mora</th><th>Total</th><th>Tipo de pago</th><th>Comprobante</th><th>Descarga</th></tr>{paymentRows}</table></section>" +
        BillingScripts();

    return Results.Content(PageLayout("Facturacion y reportes", BackHome(), content), "text/html");
});

app.MapPost("/billing/pay", async (HttpRequest request, IMemberRepository members, IBillingService billingService) =>
{
    var form = await request.ReadFormAsync();
    var memberId = form["memberId"].ToString();
    var paymentMethod = form["paymentMethod"].ToString();
    var member = members.GetAll().FirstOrDefault(m => m.Id.Equals(memberId, StringComparison.OrdinalIgnoreCase));
    if (member is null)
    {
        return Results.Redirect("/billing?status=notfound");
    }

    var receipt = billingService.RegisterMonthlyPayment(member, paymentMethod);
    return Results.Redirect($"/billing?status=ok&amount={receipt.Amount:0.00}&receipt={Uri.EscapeDataString(receipt.ReceiptNumber)}");
});

app.MapGet("/billing/receipt/{receiptNumber}", (string receiptNumber, IPaymentRepository payments, IReceiptPdfGenerator receiptPdfGenerator) =>
{
    var payment = payments.GetByReceiptNumber(receiptNumber);
    if (payment is null)
    {
        return Results.NotFound("No se encontro ningun comprobante con ese numero.");
    }

    var pdfBytes = receiptPdfGenerator.Generate(payment);
    return Results.File(pdfBytes, "application/pdf", $"comprobante-{payment.ReceiptNumber}.pdf");
});

app.Run();

static string PageLayout(string title, string navigation, string content)
{
    return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>{Enc(title)}</title><style>{Styles()}</style></head><body><header><h1>{Enc(title)}</h1></header><nav>{navigation}</nav><main>{content}</main></body></html>";
}

static string Button(string href, string text, string style = "", bool newTab = false)
{
    var target = newTab ? " target=\"_blank\"" : "";
    var styleAttr = string.IsNullOrWhiteSpace(style) ? "" : $" style=\"{style}\"";
    return $"<a class=\"button\" href=\"{href}\"{styleAttr}{target}>{Enc(text)}</a>";
}

static string BackHome()
{
    return Button("/", "Menu principal");
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

static string BillingScripts()
{
    return """
        <script>
        function selectMember(id){document.getElementById('memberIdInput').value=id;document.getElementById('memberIdInput').scrollIntoView({behavior:'smooth',block:'center'});}
        function filterMembers(){
            var text=document.getElementById('filterText').value.trim().toLowerCase();
            var plan=document.getElementById('filterPlan').value;
            var status=document.getElementById('filterStatus').value;
            var from=document.getElementById('filterFrom').value;
            var to=document.getElementById('filterTo').value;
            var rows=document.querySelectorAll('#membersTable .member-row');
            var visibleCount=0;
            rows.forEach(function(row){
                var id=row.dataset.id.toLowerCase();
                var name=row.dataset.name;
                var visible=true;
                if(text && id.indexOf(text)===-1 && name.indexOf(text)===-1){visible=false;}
                if(plan && row.dataset.plan!==plan){visible=false;}
                if(status && row.dataset.status!==status){visible=false;}
                if(from && row.dataset.expiration<from){visible=false;}
                if(to && row.dataset.expiration>to){visible=false;}
                row.style.display=visible?'':'none';
                if(visible){visibleCount++;}
            });
            document.getElementById('noResults').style.display=visibleCount===0?'':'none';
        }
        function clearMemberFilters(){
            document.getElementById('filterText').value='';
            document.getElementById('filterPlan').value='';
            document.getElementById('filterStatus').value='';
            document.getElementById('filterFrom').value='';
            document.getElementById('filterTo').value='';
            filterMembers();
        }
        </script>
        """;
}

static string Styles()
{
    return "body{font-family:Inter,system-ui,Segoe UI,Arial,sans-serif;background:linear-gradient(180deg,#eef5ff 0%,#f8fbff 100%);color:#102a43;margin:0;padding:0;}header{background:#0f4fa8;color:#fff;padding:24px 28px;box-shadow:0 20px 50px rgba(15,79,168,.18);}header h1{margin:0;font-size:2rem;}nav{padding:18px 28px;background:#fff;border-bottom:1px solid #d9e4f5;display:flex;flex-wrap:wrap;gap:10px;}a.button{display:inline-flex;align-items:center;justify-content:center;padding:12px 18px;background:#0f4fa8;color:#fff;text-decoration:none;border-radius:12px;font-weight:600;}main{padding:28px;max-width:1100px;margin:auto;}p{line-height:1.75;}section.card,.hero{background:#fff;border:1px solid #e2e8f0;border-radius:22px;padding:24px;box-shadow:0 18px 40px rgba(16,42,67,.08);margin-top:22px;}.hero{background:#f1f7ff;}.hero-actions,.filter-bar{margin-top:20px;display:flex;flex-wrap:wrap;gap:12px;}.data-table{width:100%;border-collapse:collapse;margin-top:16px;font-size:.98rem;}.data-table th,.data-table td{border:1px solid #e2e8f0;padding:14px;text-align:left;}.data-table th{background:#f1f5f9;color:#1e3a8a;}label{display:block;margin-bottom:16px;font-weight:700;color:#334155;}input,select{width:100%;padding:12px 14px;margin-top:8px;border:1px solid #cbd5e1;border-radius:12px;background:#f8fafc;box-sizing:border-box;}button{cursor:pointer;background:#0f4fa8;color:#fff;border:none;padding:14px 20px;border-radius:14px;font-size:1rem;font-weight:700;}.summary{font-weight:700;color:#334155;}.info-box{background:#eef6ff;border-left:5px solid #0f4fa8;border-radius:14px;padding:18px 20px;margin-bottom:22px;color:#102a43;}.badge{display:inline-block;padding:4px 10px;border-radius:999px;font-size:.85rem;font-weight:700;}.badge-ok{background:#dcfce7;color:#166534;}.badge-danger{background:#fee2e2;color:#991b1b;}.button-secondary{background:#e2e8f0;color:#1e293b;}.member-row{cursor:pointer;}.member-row:hover{background:#eef6ff;}.row-overdue{background:#fee2e2;}.row-overdue td{color:#7f1d1d;}.no-results{color:#64748b;font-style:italic;margin-top:12px;}.link-download{color:#0f4fa8;font-weight:700;text-decoration:none;}@media(max-width:720px){main{padding:18px;}nav{justify-content:center;}section.card,.hero{padding:20px;}}";
}

static void SeedMembers(IMemberRepository repository)
{
    var members = new List<Member>
    {
        new Member("A100", "Aaron", new StudentMembership(), "A100", "FP-A100", DateTime.Today.AddDays(-5)),
        new Member("B200", "Angel", new VipMembership(), "B200", "FP-B200", DateTime.Today.AddDays(3)),
        new Member("C300", "Luis", new WeekendMembership(), "C300", "FP-C300"),
        new Member("D400", "Sebastian", new RegularMembership(), "D400", "FP-D400")
    };

    foreach (var member in members)
    {
        repository.Save(member);
    }
}

static void SeedUsers(IUserRepository repository, AuthenticationService authService)
{
    try
    {
        authService.CreateUser("U001", "admin", "admin@gimnasio.com", "admin123", Role.Admin);
        authService.CreateUser("U002", "manager", "manager@gimnasio.com", "manager123", Role.Manager);
        authService.CreateUser("U003", "staff", "staff@gimnasio.com", "staff123", Role.Staff);
        authService.CreateUser("U004", "member", "member@gimnasio.com", "member123", Role.Member);
    }
    catch (InvalidOperationException)
    {
        // Seed can be called again while the singleton repository already has users.
    }
}
