using System.Net;
using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter;

// Una conversación viva con SCBA, abierta al hacer una búsqueda y retomada al
// seleccionar una fila o pedir una página de un oficio.
//
// Por qué existe: SCBA es WebForms con estado. La grilla, el Select$N y las
// imágenes del oficio sólo funcionan si viajan por la MISMA sesión: mismas
// cookies y el __VIEWSTATE que SCBA fue actualizando paso a paso. Si cortamos
// la conexión después de buscar (como hacía slice 4) perdemos esa conversación
// y no se puede abrir ningún oficio. Esta clase es esa conversación guardada.
//
// Cada sesión tiene su PROPIO HttpClient con su propio frasco de cookies. No
// usamos IHttpClientFactory acá a propósito: el factory reusa el handler entre
// requests y mezclaría las cookies de búsquedas distintas (esa era la deuda
// anotada en slice 4). Son pocas sesiones y de vida corta; el almacén las
// descarta (y dispone el HttpClient) al vencer.
public sealed class SesionRju : IDisposable
{
    public HttpClient Http { get; }
    public CookieContainer Cookies { get; }

    // El __VIEWSTATE / generator se pisan en cada POST con lo último que mandó
    // SCBA: el próximo request tiene que usar el más reciente, no el inicial.
    public string? ViewState { get; set; }
    public string? ViewStateGenerator { get; set; }

    // Los criterios de la búsqueda original. El body del Select$N (handoff §2)
    // tiene que reenviarlos tal cual, no alcanza con el __VIEWSTATE.
    public IReadOnlyDictionary<string, string> Criterios { get; set; } =
        new Dictionary<string, string>();

    // Se llenan al seleccionar una fila (los saca SeleccionParser). El endpoint
    // de imagen los necesita para armar imagegen.aspx?pagina=N&nocache=…
    public string Nocache { get; set; } = string.Empty;
    public int CantidadPaginas { get; set; }

    public DateTimeOffset Creada { get; } = DateTimeOffset.UtcNow;

    public SesionRju(HttpClient http, CookieContainer cookies)
    {
        Http = http;
        Cookies = cookies;
    }

    public void Dispose() => Http.Dispose();
}
