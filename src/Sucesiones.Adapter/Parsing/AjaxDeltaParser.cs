using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter.Parsing;

// El resultado ya limpio del parser: el __VIEWSTATE y su generator (que hacen
// falta para el próximo request a SCBA), el HTML útil y en qué estado quedó
// la búsqueda. Es lo único que el resto del adapter necesita ver.
public record RespuestaDelta(
    string? ViewState,
    string? ViewStateGenerator,
    string HtmlPanel,
    EstadoConsulta Estado);

// SCBA responde en el formato "delta" de Microsoft AJAX: bloques pegados uno
// atrás de otro con la forma  largo|tipo|id|contenido|  . Ejemplo:
//   38|hiddenField|__VIEWSTATE|/wEPDw...(38 caracteres)|
// El "largo" dice cuántos caracteres ocupa "contenido", y así sabemos dónde
// termina cada bloque. Este parser recorre los bloques y saca el HTML útil,
// el __VIEWSTATE y el estado de la búsqueda. Asume el formato estándar; con
// capturas reales de SCBA se revalida en slice 4.
public static class AjaxDeltaParser
{
    // Punto de entrada. Parte el texto en bloques, ubica el bloque de HTML del
    // panel, saca los campos ocultos y deduce el estado. Si no hubo panel,
    // mira el texto entero para detectar el estado (igual viene el mensaje).
    public static RespuestaDelta Parsear(string deltaTexto)
    {
        var segmentos = Tokenizar(deltaTexto);

        var htmlPanel = segmentos
            .Where(s => s.Tipo == "updatePanel")
            .Select(s => s.Contenido)
            .FirstOrDefault() ?? string.Empty;

        var viewState = ValorCampoOculto(segmentos, "__VIEWSTATE");
        var generator = ValorCampoOculto(segmentos, "__VIEWSTATEGENERATOR");
        var estado = DetectarEstado(htmlPanel.Length > 0 ? htmlPanel : deltaTexto);

        return new RespuestaDelta(viewState, generator, htmlPanel, estado);
    }

    // Busca un campo oculto por su id (ej. "__VIEWSTATE") entre los bloques de
    // tipo "hiddenField" y devuelve su valor; null si SCBA no lo mandó.
    private static string? ValorCampoOculto(IEnumerable<Segmento> segmentos, string id) =>
        segmentos.FirstOrDefault(s => s.Tipo == "hiddenField" && s.Id == id)?.Contenido;

    // Deduce el estado mirando los mensajes que SCBA escribe en el HTML.
    // El orden importa: "demasiados resultados" se chequea primero porque ese
    // texto también contiene la palabra "resultados" y caería en otra rama.
    private static EstadoConsulta DetectarEstado(string texto)
    {
        if (Contiene(texto, "demasiados resultados"))
            return EstadoConsulta.DemasiadosResultados;
        if (Contiene(texto, "sin resultados"))
            return EstadoConsulta.SinResultados;
        if (Contiene(texto, "resultados obtenidos") || Contiene(texto, "GridConsulta"))
            return EstadoConsulta.ConResultados;

        return EstadoConsulta.Error;
    }

    // "¿el texto contiene la aguja?" sin distinguir mayúsculas de minúsculas
    // (ojo: NO ignora acentos, solo la caja de las letras).
    private static bool Contiene(string texto, string aguja) =>
        texto.Contains(aguja, StringComparison.OrdinalIgnoreCase);

    // Un bloque ya separado del delta: su tipo, su id y su contenido.
    // Tipos que nos importan (el resto los recorremos pero los ignoramos):
    //   updatePanel  (id "UpdatePanel1")          -> Contenido = el HTML útil:
    //       el mensaje (Resultados/Sin/demasiados) y la <table GridConsulta>.
    //   hiddenField  (id "__VIEWSTATE")           -> estado de la página
    //       WebForms; hay que reenviarlo en el próximo request (slice 5).
    //   hiddenField  (id "__VIEWSTATEGENERATOR")  -> código corto que viaja
    //       siempre de la mano del __VIEWSTATE.
    //   scriptBlock, formAction, pageTitle, …     -> motor interno de
    //       WebForms (scripts, config del postback); no los usamos.
    private record Segmento(string Tipo, string Id, string Contenido);

    // Recorre el texto cortándolo en bloques `largo|tipo|id|contenido|`.
    // Si encuentra algo mal formado (un largo que no es número, o que se pasa
    // del final) corta y devuelve lo que pudo leer en vez de explotar: la
    // respuesta de SCBA es un borde externo y puede venir rara.
    private static List<Segmento> Tokenizar(string texto)
    {
        var segmentos = new List<Segmento>();
        var pos = 0;

        while (pos < texto.Length)
        {
            if (!LeerHasta(texto, ref pos, out var lenStr) || !int.TryParse(lenStr, out var len)) break;
            if (!LeerHasta(texto, ref pos, out var tipo)) break;
            if (!LeerHasta(texto, ref pos, out var id)) break;
            if (pos + len > texto.Length) break;

            var contenido = texto.Substring(pos, len);
            pos += len;
            if (pos < texto.Length && texto[pos] == '|') pos++;

            segmentos.Add(new Segmento(tipo, id, contenido));
        }

        return segmentos;
    }

    // Lee desde 'pos' hasta el próximo '|'. Avanza 'pos' (por eso va por ref)
    // dejándolo justo después del separador, listo para la próxima lectura.
    // Devuelve false si no hay más '|' (se acabó el texto).
    private static bool LeerHasta(string texto, ref int pos, out string token)
    {
        var inicio = pos;
        while (pos < texto.Length && texto[pos] != '|') pos++;
        if (pos >= texto.Length)
        {
            token = string.Empty;
            return false;
        }

        token = texto[inicio..pos];
        pos++;
        return true;
    }
}
