// [TEMPLATE] Archivo generado por `dotnet new razor`. Edité dos líneas para Blazor
// Server (marcadas con [BUSCAR]). El resto es el host minimal de ASP.NET Core estándar.

var builder = WebApplication.CreateBuilder(args);

// [TEMPLATE] Razor Pages.
builder.Services.AddRazorPages();

// [BUSCAR] Habilita Blazor Server. Sin esto, los componentes .razor no tienen
// circuit SignalR y se renderizan estáticos. Va de la doc oficial de Blazor.
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// [TEMPLATE] Middleware estándar del template.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// [BUSCAR] Endpoint SignalR `/_blazor` que sirve el circuit Blazor Server.
// Va junto con AddServerSideBlazor(). Es el par "registrar servicio + mapear endpoint".
app.MapBlazorHub();

app.Run();
