using HtmlAgilityPack;
using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter.Parsing;

// Después del postback Select$N, SCBA contesta otro delta. Adentro del
// UpdatePanel2 viene el visor con los <img> del documento. Ejemplo real
// (fixture delta-seleccion.txt):
//
//   <img src="imagegen.aspx?pagina=0&nocache=639146528298899340" id="image0" .../>
//   <img src="imagenpaginagen.aspx?pagina=1 de 2&nocache=..." id="imagePage0" .../>
//   <img src="imagegen.aspx?pagina=1&nocache=639146528298899340" id="image1" .../>
//   <img src="imagenpaginagen.aspx?pagina=2 de 2&nocache=..." id="imagePage1" .../>
//
// Lo que nos importa: el token `nocache` (el mismo para todas las páginas de
// esa selección; hay que reenviarlo en cada GET de imagen) y la lista de
// páginas reales. Los `imagenpaginagen.aspx` son los carteles "Página X de Y",
// NO el documento: el handoff pide ignorarlos y los descartamos pidiendo que
// el src arranque exactamente con "imagegen.aspx?pagina=".
//
// No partimos el delta a mano: cargamos el texto entero en HtmlAgilityPack
// (es tolerante, ignora el envoltorio delta y igual encuentra los <img>) y
// filtramos por src. Es robusto y no duplica el tokenizer de AjaxDeltaParser.
public static class SeleccionParser
{
    private const string PrefijoImagenReal = "imagegen.aspx?pagina=";

    public static OficioSeleccionado Parsear(string deltaTexto)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(deltaTexto);

        var imgs = doc.DocumentNode.SelectNodes("//img");
        if (imgs is null)
            return new OficioSeleccionado(string.Empty, Array.Empty<int>());

        var nocache = string.Empty;
        var paginas = new List<int>();

        foreach (var img in imgs)
        {
            var src = img.GetAttributeValue("src", "");
            if (!src.StartsWith(PrefijoImagenReal, StringComparison.OrdinalIgnoreCase))
                continue;

            var query = LeerQuery(src);

            // "pagina" tiene que ser un entero (las páginas reales son 0, 1, …;
            // los carteles traen "1 de 2" y no entran acá porque su src es
            // imagenpaginagen.aspx, ya filtrado arriba).
            if (!query.TryGetValue("pagina", out var pag) || !int.TryParse(pag, out var n))
                continue;

            if (!paginas.Contains(n))
                paginas.Add(n);

            // El nocache es el mismo en todas; lo tomamos del primero que aparezca.
            if (nocache.Length == 0 && query.TryGetValue("nocache", out var nc))
                nocache = nc;
        }

        return new OficioSeleccionado(nocache, paginas);
    }

    // Parte "imagegen.aspx?pagina=0&nocache=639..." en {pagina:0, nocache:639...}.
    // Split manual y simple: la query es de SCBA, son pares clave=valor sin
    // escapes raros; no hace falta traer System.Web sólo para esto.
    private static Dictionary<string, string> LeerQuery(string src)
    {
        var pares = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var inicio = src.IndexOf('?');
        if (inicio < 0) return pares;

        foreach (var par in src[(inicio + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = par.IndexOf('=');
            if (i <= 0) continue;
            pares[par[..i]] = par[(i + 1)..];
        }

        return pares;
    }
}
