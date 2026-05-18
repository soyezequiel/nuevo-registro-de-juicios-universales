using Sucesiones.Adapter;

var creadorApp = WebApplication.CreateBuilder(args);

creadorApp.Services.AddRazorPages();
creadorApp.Services.AddServerSideBlazor();

var opcionesRju = creadorApp.Configuration.GetSection("Rju").Get<OpcionesRju>() ?? new OpcionesRju();

if (opcionesRju.ModoReal)
{
    // HttpClient con "frasco de cookies": el GET inicial y el POST de
    // búsqueda comparten sesión porque el handler guarda las cookies de
    // SCBA entre ambos requests. Limitación conocida: IHttpClientFactory
    // reusa el handler ~2 min, así que el frasco se comparte entre
    // búsquedas. Alcanza para slice 4 (cada búsqueda pide su __VIEWSTATE
    // fresco); la sesión aislada por búsqueda se ataca en slice 5.
    creadorApp.Services.AddHttpClient<IClienteRju, ClienteRjuHttp>(cliente =>
    {
        cliente.BaseAddress = new Uri(opcionesRju.UrlBase);
        cliente.DefaultRequestHeaders.Add("Origin", opcionesRju.UrlBase.TrimEnd('/'));
        cliente.DefaultRequestHeaders.Referrer = new Uri(opcionesRju.UrlBase);
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Sucesiones-Adapter/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer(),
    });
}
else
{
    creadorApp.Services.AddScoped<IClienteRju, ClienteRjuFake>();
}

var aplicacion = creadorApp.Build();

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Error");
    aplicacion.UseHsts();
}

aplicacion.UseHttpsRedirection();
aplicacion.UseRouting();
aplicacion.UseAuthorization();

aplicacion.MapStaticAssets();
aplicacion.MapRazorPages()
   .WithStaticAssets();
aplicacion.MapBlazorHub();

aplicacion.Run();
