using System.Collections.Concurrent;
using System.Net;

namespace Sucesiones.Adapter;

// El "fichero" de sesiones SCBA vivas. Cada búsqueda saca una carpeta nueva
// (un searchId), y con ese ticket se recupera después para seleccionar una
// fila o bajar una página. Es singleton: hay UN solo fichero para toda la app,
// compartido entre el circuito Blazor (que busca/selecciona) y el endpoint
// /oficio (que baja las imágenes), porque ambos resuelven este mismo objeto.
//
// Limitaciones honestas (es un MVP de aprendizaje, no infra de producción):
//   - En memoria y de un solo proceso: si la app reinicia, las sesiones se
//     pierden y hay que volver a buscar. No hay persistencia ni balanceo.
//   - Vencimiento simple por tiempo. El __VIEWSTATE de SCBA puede expirar
//     antes de la ventana; si pasa, el Select$N devuelve Error (sin esconderlo).
public class AlmacenSesionesRju : IDisposable
{
    // Ventana de vida de una sesión. Pasado esto se descarta y el usuario
    // tiene que repetir la búsqueda. 20 min es un valor de demo razonable;
    // el handoff avisa que el __VIEWSTATE puede caducar antes.
    private static readonly TimeSpan Vencimiento = TimeSpan.FromMinutes(20);

    private readonly ConcurrentDictionary<string, SesionRju> _sesiones = new();
    private readonly OpcionesRju _opciones;

    public AlmacenSesionesRju(OpcionesRju opciones) => _opciones = opciones;

    // Abre una sesión nueva (HttpClient + frasco de cookies propios) y la
    // guarda bajo un ticket. Devuelve el ticket y la sesión.
    public (string searchId, SesionRju sesion) Crear()
    {
        PurgarVencidas();

        var cookies = new CookieContainer();
        var http = new HttpClient(new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookies,
        })
        {
            BaseAddress = new Uri(_opciones.UrlBase),
        };
        http.DefaultRequestHeaders.Add("Origin", _opciones.UrlBase.TrimEnd('/'));
        http.DefaultRequestHeaders.Referrer = new Uri(_opciones.UrlBase);
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Sucesiones-Adapter/1.0");

        var searchId = Guid.NewGuid().ToString("N");
        var sesion = new SesionRju(http, cookies);
        _sesiones[searchId] = sesion;
        return (searchId, sesion);
    }

    // Recupera la sesión de un ticket, o null si no existe / venció.
    public SesionRju? Obtener(string searchId)
    {
        PurgarVencidas();
        return _sesiones.TryGetValue(searchId, out var s) ? s : null;
    }

    private void PurgarVencidas()
    {
        var limite = DateTimeOffset.UtcNow - Vencimiento;
        foreach (var (id, sesion) in _sesiones)
        {
            if (sesion.Creada < limite && _sesiones.TryRemove(id, out var quitada))
                quitada.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var sesion in _sesiones.Values)
            sesion.Dispose();
        _sesiones.Clear();
    }
}
