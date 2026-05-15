var creadorApp = WebApplication.CreateBuilder(args);

creadorApp.Services.AddRazorPages();
creadorApp.Services.AddServerSideBlazor();

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
