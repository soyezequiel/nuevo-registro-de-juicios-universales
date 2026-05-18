using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter;

public interface IClienteRju
{
    Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios);

    // Abre la fila `indice` de la búsqueda `searchId` (postback Select$N
    // reusando la sesión de esa búsqueda). Devuelve cuántas páginas tiene.
    Task<ResultadoSeleccion> SeleccionarAsync(string searchId, int indice);

    // Baja una página del oficio seleccionado en `searchId`. null si la
    // sesión venció o SCBA no la dio (el endpoint lo traduce a 404/410).
    Task<PaginaOficio?> ObtenerPaginaAsync(string searchId, int pagina);
}
