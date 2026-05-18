using System.Text;
using HtmlAgilityPack;
using Sucesiones.Adapter.Modelos;
using Sucesiones.Adapter.Parsing;

namespace Sucesiones.Adapter;

// Adapter REAL contra SCBA RJU. SCBA es un sitio WebForms viejo: hay que
// hacer un "trámite de ventanilla" en vez de pedir datos a una API limpia.
//
//   1. GET /  -> SCBA nos da el HTML del formulario con un código secreto
//                escondido (__VIEWSTATE). Sin ese código, rechaza el resto.
//   2. Armamos el body del formulario con los criterios + el código pegado.
//   3. POST /  -> entregamos el formulario; SCBA contesta en un formato
//                apelmazado (delta de Microsoft AJAX), no en JSON.
//   4. Traducimos esa respuesta con los parsers del slice 3 y devolvemos
//      el mismo ResultadoBusqueda que devuelve el fake.
//
// Si algo del borde externo falla (red caída, SCBA con error, certificado
// rechazado) devolvemos EstadoConsulta.Error con un mensaje claro. Nunca
// inventamos resultados ni escondemos el error con un fallback silencioso.
public class ClienteRjuHttp : IClienteRju
{
    private readonly HttpClient _http;

    public ClienteRjuHttp(HttpClient http) => _http = http;

    public async Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios)
    {
        try
        {
            // Paso 1: entrar y pedir el formulario.
            var htmlInicial = await _http.GetStringAsync("");
            var (viewState, generator) = LeerCamposOcultos(htmlInicial);

            if (viewState is null)
                return Error("No se pudo leer el __VIEWSTATE inicial de SCBA.");

            // Paso 2: armar el body con el código secreto pegado.
            var body = ArmarBody(criterios, viewState, generator);

            // Paso 3: entregar el formulario (POST AJAX). Estos headers le
            // dicen a SCBA "esto es un postback AJAX", y por eso contesta
            // en formato delta en vez de devolver la página entera.
            using var pedido = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"),
            };
            pedido.Headers.Add("X-MicrosoftAjax", "Delta=true");
            pedido.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var respuesta = await _http.SendAsync(pedido);
            if (!respuesta.IsSuccessStatusCode)
                return Error($"SCBA respondió con código {(int)respuesta.StatusCode}.");

            var deltaTexto = await respuesta.Content.ReadAsStringAsync();

            // Paso 4: traducir el choclo con los parsers del slice 3.
            var delta = AjaxDeltaParser.Parsear(deltaTexto);
            var filas = GridConsultaParser.Parsear(delta.HtmlPanel);

            return new ResultadoBusqueda(delta.Estado, filas);
        }
        catch (HttpRequestException ex)
        {
            // Red caída, DNS, o TLS/certificado rechazado: borde externo.
            return Error($"No se pudo contactar a SCBA: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            // Incluye el timeout del HttpClient.
            return Error("SCBA tardó demasiado en responder.");
        }
    }

    private static ResultadoBusqueda Error(string mensaje) =>
        new(EstadoConsulta.Error, Array.Empty<ResultadoSucesion>(), mensaje);

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

    // Arma el body x-www-form-urlencoded a mano. Lo hacemos manual (y no con
    // FormUrlEncodedContent) para tener control exacto sobre qué campos van y
    // con qué nombre, y para garantizar que __VIEWSTATE viaje URL-encoded tal
    // como exige el handoff (mandarlo crudo puede dar error interno AJAX
    // aunque el HTTP responda 200).
    private static string ArmarBody(
        IReadOnlyDictionary<string, string> criterios,
        string viewState,
        string? generator)
    {
        // Campos fijos del flujo de búsqueda (handoff). dropAplicaciones=1
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

        return string.Join("&", campos.Select(c =>
            $"{Uri.EscapeDataString(c.Key)}={Uri.EscapeDataString(c.Value)}"));
    }
}
