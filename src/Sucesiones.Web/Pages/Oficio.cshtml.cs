using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sucesiones.Adapter;

namespace Sucesiones.Web.Pages;

// Intermediario de imágenes. El navegador NO le pide la imagen a SCBA directo
// (CORS, contrato frágil, datos sensibles en crudo, sesión/__VIEWSTATE: lo
// explica el handoff). En vez de eso, el <img> del visor apunta a
// /oficio/{searchId}/pagina/{n} de ESTA app; acá la traemos de SCBA por la
// sesión de esa búsqueda y la reenviamos.
public class OficioModel : PageModel
{
    private readonly IClienteRju _rju;

    public OficioModel(IClienteRju rju) => _rju = rju;

    public async Task<IActionResult> OnGetAsync(string searchId, int pagina)
    {
        var p = await _rju.ObtenerPaginaAsync(searchId, pagina);
        // null = sesión vencida o SCBA no la dio. 404 claro, sin imagen falsa.
        if (p is null) return NotFound();

        return File(p.Bytes, p.ContentType);
    }
}
