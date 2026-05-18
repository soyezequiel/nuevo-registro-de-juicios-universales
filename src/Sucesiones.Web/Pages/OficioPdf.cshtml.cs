using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Sucesiones.Adapter;

namespace Sucesiones.Web.Pages;

// Arma el PDF de descarga del oficio: baja TODAS las páginas por la sesión
// (una por una hasta que ObtenerPaginaAsync devuelve null = no hay más) y
// mete cada imagen en su propia hoja, del tamaño de la imagen.
//
// Bordes honestos (sin PDF vacío ni roto en silencio):
//   - Sesión vencida / sin páginas  -> 404.
//   - Imagen en un formato que PDFsharp no incrusta (TIFF crudo, o el SVG
//     del modo fake) -> 415 con mensaje claro. PDFsharp solo digiere
//     raster común (JPEG/PNG); en modo real SCBA manda algo que el browser
//     dibuja, así que en la práctica entra bien.
public class OficioPdfModel : PageModel
{
    private readonly IClienteRju _rju;

    public OficioPdfModel(IClienteRju rju) => _rju = rju;

    public async Task<IActionResult> OnGetAsync(string searchId)
    {
        // Las imágenes tienen que seguir vivas hasta doc.Save: PDFsharp lee
        // el stream recién al guardar, no al construir el XImage.
        var streams = new List<MemoryStream>();
        using var doc = new PdfDocument();

        try
        {
            for (var pagina = 0; ; pagina++)
            {
                var p = await _rju.ObtenerPaginaAsync(searchId, pagina);
                if (p is null) break;

                // publiclyVisible:true es obligatorio: el lector JPEG de
                // PDFsharp llama a GetBuffer(), y un MemoryStream hecho con
                // new MemoryStream(bytes) NO lo permite (tira "internal buffer
                // cannot be accessed"). Con este overload sí.
                var ms = new MemoryStream(p.Bytes, 0, p.Bytes.Length,
                    writable: false, publiclyVisible: true);
                streams.Add(ms);

                XImage img;
                try
                {
                    img = XImage.FromStream(ms);
                }
                catch (Exception ex)
                {
                    // Diagnóstico: mostramos lo que SCBA mandó de verdad
                    // (Content-Type + firma de bytes) en vez de adivinar.
                    var firma = Convert.ToHexString(p.Bytes.Take(16).ToArray());
                    return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                        $"No se pudo incrustar la página {pagina} en el PDF.\n" +
                        $"Content-Type SCBA: {p.ContentType}\n" +
                        $"Primeros 16 bytes (hex): {firma}\n" +
                        $"Error PDFsharp: {ex.Message}\n" +
                        "Referencia: JPEG=FFD8FF · PNG=89504E47 · " +
                        "TIFF=49492A00 o 4D4D002A · GIF=47494638 · HTML empieza con 3C");
                }

                var hoja = doc.AddPage();
                hoja.Width = XUnit.FromPoint(img.PointWidth);
                hoja.Height = XUnit.FromPoint(img.PointHeight);
                using var gfx = XGraphics.FromPdfPage(hoja);
                gfx.DrawImage(img, 0, 0, hoja.Width.Point, hoja.Height.Point);
            }

            if (doc.PageCount == 0)
                return NotFound();

            using var salida = new MemoryStream();
            doc.Save(salida);
            // Sin fileDownloadName a propósito: el nombre lo pone el atributo
            // HTML `download` del link (lado cliente, con apellido/nombre/doc).
            // Si el server mandara un filename, el navegador lo priorizaría y
            // pisaría el del `download`. Además así no viajan datos personales
            // del causante en la URL ni en headers.
            return File(salida.ToArray(), "application/pdf");
        }
        finally
        {
            foreach (var s in streams) s.Dispose();
        }
    }
}
