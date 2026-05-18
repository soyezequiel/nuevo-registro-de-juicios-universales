using System.Text;
using HtmlAgilityPack;
using Sucesiones.Adapter.Modelos;
using Sucesiones.Adapter.Parsing;

namespace Sucesiones.Adapter;

// Adapter REAL contra SCBA RJU. SCBA es un sitio WebForms viejo: hay que
// hacer un "trámite de ventanilla" con estado, no pedir datos a una API limpia.
//
// Búsqueda (slice 4):
//   1. GET /  -> SCBA da el HTML con un código secreto escondido (__VIEWSTATE).
//   2. Armamos el body del formulario con los criterios + ese código.
//   3. POST /  -> contesta en formato delta de Microsoft AJAX (no JSON).
//   4. Traducimos con los parsers y devolvemos ResultadoBusqueda.
//
// Selección + imagen (slice 5): el oficio sólo se abre si seguimos hablando
// EN LA MISMA conversación que la búsqueda (mismas cookies + el __VIEWSTATE
// que SCBA fue actualizando). Por eso la búsqueda ahora abre una SesionRju y
// la guarda en el almacén; el Select$N y el GET de imagen la retoman por su
// searchId. Sin eso, imagegen.aspx no devuelve nada (depende de la sesión
// donde se hizo el Select$N — lo dice el handoff).
//
// Si algo del borde externo falla (red, SCBA con error, sesión vencida)
// devolvemos un estado de error con mensaje claro. Nunca inventamos resultados
// ni escondemos el error con un fallback silencioso.
public class ClienteRjuHttp : IClienteRju
{
    private readonly AlmacenSesionesRju _almacen;

    public ClienteRjuHttp(AlmacenSesionesRju almacen) => _almacen = almacen;

    public async Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios)
    {
        var (searchId, sesion) = _almacen.Crear();
        try
        {
            // Paso 1: entrar y pedir el formulario (por la sesión nueva, que
            // así se queda con las cookies que SCBA setea en el GET inicial).
            var htmlInicial = await sesion.Http.GetStringAsync("");
            var (viewState, generator) = LeerCamposOcultos(htmlInicial);

            if (viewState is null)
                return Error("No se pudo leer el __VIEWSTATE inicial de SCBA.", searchId);

            // Paso 2: armar el body con el código secreto pegado.
            var body = ArmarBodyBusqueda(criterios, viewState, generator);

            // Paso 3: entregar el formulario (POST AJAX). Estos headers le
            // dicen a SCBA "esto es un postback AJAX", y por eso contesta
            // en formato delta en vez de devolver la página entera.
            var deltaTexto = await PostAjax(sesion, body);
            if (deltaTexto is null)
                return Error("SCBA respondió con un código de error a la búsqueda.", searchId);

            // Paso 4: traducir el choclo con los parsers del slice 3.
            var delta = AjaxDeltaParser.Parsear(deltaTexto);
            var filas = GridConsultaParser.Parsear(delta.HtmlPanel);

            // Guardamos en la sesión lo necesario para retomar la conversación
            // al seleccionar una fila: el __VIEWSTATE actualizado (NO el
            // inicial) y los criterios (el body del Select$N los reenvía).
            sesion.ViewState = delta.ViewState ?? viewState;
            sesion.ViewStateGenerator = delta.ViewStateGenerator ?? generator;
            sesion.Criterios = criterios;

            return new ResultadoBusqueda(delta.Estado, filas, SearchId: searchId);
        }
        catch (HttpRequestException ex)
        {
            return Error($"No se pudo contactar a SCBA: {ex.Message}", searchId);
        }
        catch (TaskCanceledException)
        {
            return Error("SCBA tardó demasiado en responder.", searchId);
        }
    }

    public async Task<ResultadoSeleccion> SeleccionarAsync(string searchId, int indice)
    {
        var sesion = _almacen.Obtener(searchId);
        if (sesion is null || sesion.ViewState is null)
            return new ResultadoSeleccion(false, 0,
                "La búsqueda venció o no existe. Repetí la consulta.");

        try
        {
            // Body del Select$N (handoff §2): reusa los criterios y el
            // __VIEWSTATE que dejó la búsqueda, y dispara el postback de la
            // grilla con __EVENTARGUMENT=Select$N.
            var body = ArmarBodySeleccion(sesion, indice);

            var deltaTexto = await PostAjax(sesion, body);
            if (deltaTexto is null)
                return new ResultadoSeleccion(false, 0,
                    "SCBA respondió con un código de error a la selección.");

            // El __VIEWSTATE se volvió a actualizar; hay que guardarlo por si
            // después se navega entre páginas (slice 5b).
            var delta = AjaxDeltaParser.Parsear(deltaTexto);
            if (delta.ViewState is not null) sesion.ViewState = delta.ViewState;
            if (delta.ViewStateGenerator is not null) sesion.ViewStateGenerator = delta.ViewStateGenerator;

            var oficio = SeleccionParser.Parsear(deltaTexto);
            if (oficio.Paginas.Count == 0)
                return new ResultadoSeleccion(false, 0,
                    "SCBA no devolvió páginas para esa fila (puede haber expirado la sesión).");

            sesion.Nocache = oficio.Nocache;
            sesion.CantidadPaginas = oficio.Paginas.Count;
            return new ResultadoSeleccion(true, oficio.Paginas.Count);
        }
        catch (HttpRequestException ex)
        {
            return new ResultadoSeleccion(false, 0, $"No se pudo contactar a SCBA: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new ResultadoSeleccion(false, 0, "SCBA tardó demasiado en responder.");
        }
    }

    public async Task<PaginaOficio?> ObtenerPaginaAsync(string searchId, int pagina)
    {
        var sesion = _almacen.Obtener(searchId);
        if (sesion is null || sesion.Nocache.Length == 0) return null;
        if (pagina < 0 || pagina >= sesion.CantidadPaginas) return null;

        try
        {
            // imagegen.aspx convierte el TIFF original a algo que el navegador
            // dibuja; sólo responde si el GET viaja por la sesión donde se hizo
            // el Select$N (de ahí que use sesion.Http, con sus cookies).
            var url = $"imagegen.aspx?pagina={pagina}&nocache={Uri.EscapeDataString(sesion.Nocache)}";
            using var resp = await sesion.Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var tipo = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return new PaginaOficio(bytes, tipo);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    // POST AJAX a "/" con el body urlencoded. Devuelve el texto delta, o null
    // si SCBA contestó con un código de error HTTP.
    private static async Task<string?> PostAjax(SesionRju sesion, string body)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"),
        };
        pedido.Headers.Add("X-MicrosoftAjax", "Delta=true");
        pedido.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var resp = await sesion.Http.SendAsync(pedido);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync() : null;
    }

    private static ResultadoBusqueda Error(string mensaje, string searchId) =>
        new(EstadoConsulta.Error, Array.Empty<ResultadoSucesion>(), mensaje, searchId);

    // Lee __VIEWSTATE y __VIEWSTATEGENERATOR de la página inicial. Ojo: en el
    // GET inicial NO vienen en formato delta; vienen como inputs ocultos
    // normales (<input type="hidden" id="__VIEWSTATE" value="..." />). Por eso
    // esto NO lo hace AjaxDeltaParser: ese parsea la respuesta del POST, que
    // es otra cosa. Acá usamos HtmlAgilityPack, igual que el otro parser.
    private static (string? viewState, string? generator) LeerCamposOcultos(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string? Valor(string id) =>
            doc.DocumentNode
               .SelectSingleNode($"//input[@id='{id}']")
               ?.Attributes["value"]?.Value;

        return (Valor("__VIEWSTATE"), Valor("__VIEWSTATEGENERATOR"));
    }

    // Body x-www-form-urlencoded de la BÚSQUEDA. Lo armamos a mano (y no con
    // FormUrlEncodedContent) para tener control exacto sobre qué campos van y
    // con qué nombre, y para garantizar que __VIEWSTATE viaje URL-encoded tal
    // como exige el handoff (mandarlo crudo puede dar error interno AJAX
    // aunque el HTTP responda 200).
    private static string ArmarBodyBusqueda(
        IReadOnlyDictionary<string, string> criterios,
        string viewState,
        string? generator)
    {
        // Campos fijos del flujo de búsqueda (handoff §1). dropAplicaciones=1
        // es el código de "Sucesiones"; el MVP solo cubre ese tipo.
        var campos = new List<KeyValuePair<string, string>>
        {
            new("ScriptManager1", "ScriptManager1|btnBuscar"),
            new("__EVENTTARGET", ""),
            new("__EVENTARGUMENT", ""),
            new("__LASTFOCUS", ""),
            new("__VIEWSTATE", viewState),
            new("__VIEWSTATEGENERATOR", generator ?? ""),
            new("HiddenScrollbarPosition", "0"),
            new("altoBody", ""),
            new("dropAplicaciones", "1"),
        };

        // Lo que tipeó el usuario. Las claves ya vienen con el nombre exacto
        // que SCBA espera (RJUapellido, RJUaño, …; ver CamposSucesiones).
        foreach (var (clave, valor) in criterios)
            campos.Add(new(clave, valor ?? ""));

        campos.Add(new("__ASYNCPOST", "true"));
        campos.Add(new("btnBuscar", "Realizar búsqueda"));

        return Urlencode(campos);
    }

    // Body del POSTBACK de SELECCIÓN (handoff §2). Mismos criterios que la
    // búsqueda, pero esto NO trae btnBuscar: en vez de "buscar", dispara el
    // postback de la grilla (ScriptManager1=UpdatePanel1|GridConsulta,
    // __EVENTTARGET=GridConsulta, __EVENTARGUMENT=Select$N). El __VIEWSTATE es
    // el ACTUALIZADO que dejó la búsqueda, no el inicial.
    private static string ArmarBodySeleccion(SesionRju sesion, int indice)
    {
        var campos = new List<KeyValuePair<string, string>>
        {
            new("ScriptManager1", "UpdatePanel1|GridConsulta"),
            new("HiddenScrollbarPosition", "0"),
            new("altoBody", ""),
            new("dropAplicaciones", "1"),
        };

        foreach (var (clave, valor) in sesion.Criterios)
            campos.Add(new(clave, valor ?? ""));

        campos.Add(new("__EVENTTARGET", "GridConsulta"));
        campos.Add(new("__EVENTARGUMENT", $"Select${indice}"));
        campos.Add(new("__LASTFOCUS", ""));
        campos.Add(new("__VIEWSTATE", sesion.ViewState ?? ""));
        campos.Add(new("__VIEWSTATEGENERATOR", sesion.ViewStateGenerator ?? ""));
        campos.Add(new("__ASYNCPOST", "true"));

        return Urlencode(campos);
    }

    private static string Urlencode(IEnumerable<KeyValuePair<string, string>> campos) =>
        string.Join("&", campos.Select(c =>
            $"{Uri.EscapeDataString(c.Key)}={Uri.EscapeDataString(c.Value)}"));
}
