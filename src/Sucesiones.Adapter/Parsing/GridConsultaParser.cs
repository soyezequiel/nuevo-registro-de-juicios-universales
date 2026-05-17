using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter.Parsing;

// Recibe el HTML que salió del updatePanel y extrae las filas de la
// <table id="GridConsulta">. Acá sí usamos un parser HTML real
// (HtmlAgilityPack), no regex: el handoff lo pide y es más robusto ante
// cambios chicos del HTML de SCBA.
public static partial class GridConsultaParser
{
    // Regex compilada en tiempo de compilación (source generator) que busca
    // "Select$N" dentro del onclick de la lupa. Ese N es el índice que SCBA
    // necesita después para abrir ese oficio (slice 5).
    [GeneratedRegex(@"Select\$\d+")]
    private static partial Regex ArgumentoSeleccion();

    // Convierte el HTML de la grilla en una lista de ResultadoSucesion.
    // Si no está la tabla (ej. respuesta "Sin resultados") devuelve lista
    // vacía: es un caso normal, no un error.
    public static IReadOnlyList<ResultadoSucesion> Parsear(string htmlPanel)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlPanel);

        var tabla = doc.DocumentNode.SelectSingleNode("//table[@id='GridConsulta']");
        if (tabla is null) return Array.Empty<ResultadoSucesion>();

        var filas = new List<ResultadoSucesion>();

        foreach (var tr in tabla.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>())
        {
            // La fila de encabezado usa <th>, no <td>; al pedir "./td" nos da
            // null o menos celdas de las esperadas, y la salteamos.
            var celdas = tr.SelectNodes("./td");
            if (celdas is null || celdas.Count < 7) continue;

            // La celda 0 es la lupa "Ver"; su onclick trae __doPostBack(...,'Select$N').
            var onclick = tr.SelectSingleNode(".//input")?.GetAttributeValue("onclick", "") ?? "";
            var arg = ArgumentoSeleccion().Match(onclick).Value;

            // Sacamos el N de "Select$N". Si por algo no vino, usamos la
            // posición en la lista como índice de respaldo.
            var indice = filas.Count;
            if (arg.Length > 0 && int.TryParse(arg["Select$".Length..], out var n))
                indice = n;

            // Orden de columnas del handoff: 0=Ver(lupa), 1=año,
            // 2=num_oficio, 3=apellido, 4=nombre, 5=num_documento,
            // 6=fallecimiento. La 0 no se lee acá (ya sacamos el Select$N).
            filas.Add(new ResultadoSucesion(
                indice,
                Texto(celdas[1]),
                Texto(celdas[2]),
                Texto(celdas[3]),
                Texto(celdas[4]),
                Texto(celdas[5]),
                Texto(celdas[6]),
                arg));
        }

        return filas;
    }

    // Limpia el texto de una celda: decodifica entidades HTML (&amp; -> &,
    // &nbsp; -> espacio) y recorta los espacios de los bordes.
    private static string Texto(HtmlNode celda) =>
        HtmlEntity.DeEntitize(celda.InnerText).Trim();
}
