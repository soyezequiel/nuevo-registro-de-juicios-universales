using Sucesiones.Adapter;

var creadorApp = WebApplication.CreateBuilder(args);

creadorApp.Services.AddRazorPages();
creadorApp.Services.AddServerSideBlazor();

var opcionesRju = creadorApp.Configuration.GetSection("Rju").Get<OpcionesRju>() ?? new OpcionesRju();

if (opcionesRju.ModoReal)
{
    // Slice 5: cada búsqueda abre su PROPIA sesión SCBA (HttpClient + frasco
    // de cookies propio) guardada en el almacén singleton por searchId. El
    // Select$N y el endpoint de imagen la retoman por ese ticket. Esto
    // reemplaza el handler compartido de slice 4 (que mezclaba cookies entre
    // búsquedas: la deuda anotada). El almacén crea/dispone los HttpClient.
    creadorApp.Services.AddSingleton(opcionesRju);
    creadorApp.Services.AddSingleton<AlmacenSesionesRju>();
    creadorApp.Services.AddScoped<IClienteRju, ClienteRjuHttp>();
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
