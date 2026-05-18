using System.Text;
using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter;

// Datos ficticios y delay simulado. Sirve para desarrollar y demostrar el
// flujo completo (buscar → seleccionar → ver página) sin depender de SCBA.
// Ignora searchId/criterios a propósito: no hay sesión real que mantener.
public class ClienteRjuFake : IClienteRju
{
    private const string SearchIdFake = "fake";

    private static readonly IReadOnlyList<ResultadoSucesion> Resultados =
    [
        new ResultadoSucesion(0, "1998", "6619", "Gómez", "María Esther", "10.234.567", "18/05/1997", "Select$0"),
        new ResultadoSucesion(1, "2005", "12840", "Gómez", "Roberto Carlos", "8.901.234", "02/11/2004", "Select$1"),
        new ResultadoSucesion(2, "2017", "30551", "Gómez", "Ana Lucía", "27.456.789", "30/06/2016", "Select$2"),
    ];

    public async Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios)
    {
        await Task.Delay(800);
        return new ResultadoBusqueda(EstadoConsulta.ConResultados, Resultados, SearchId: SearchIdFake);
    }

    public async Task<ResultadoSeleccion> SeleccionarAsync(string searchId, int indice)
    {
        await Task.Delay(500);
        // El oficio fake tiene 1 sola página (alcanza para probar el visor de 5a).
        return new ResultadoSeleccion(true, 1);
    }

    public async Task<PaginaOficio?> ObtenerPaginaAsync(string searchId, int pagina)
    {
        await Task.Delay(300);
        if (pagina != 0) return null;

        // SVG de relleno: se dibuja en un <img> sin imágenes binarias de por
        // medio. Deja claro a la vista que es modo fake, no un oficio real.
        var svg =
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="800" height="1000">
              <rect width="100%" height="100%" fill="#f1f5f9"/>
              <rect x="40" y="40" width="720" height="920" fill="#ffffff" stroke="#1e3a8a" stroke-width="2"/>
              <text x="400" y="120" font-family="Arial" font-size="28" fill="#0f172a" text-anchor="middle" font-weight="bold">Oficio (FAKE)</text>
              <text x="400" y="170" font-family="Arial" font-size="18" fill="#475569" text-anchor="middle">Página 0 — datos ficticios</text>
              <text x="400" y="540" font-family="Arial" font-size="16" fill="#94a3b8" text-anchor="middle">Modo real (Rju:ModoReal=true) trae la imagen real de SCBA</text>
            </svg>
            """;
        return new PaginaOficio(Encoding.UTF8.GetBytes(svg), "image/svg+xml");
    }
}
