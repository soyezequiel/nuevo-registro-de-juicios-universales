using Sucesiones.Adapter.Modelos;
using Sucesiones.Adapter.Parsing;

namespace Sucesiones.Adapter.Tests;

// Los fixtures son deltas SINTÉTICOS, armados del formato documentado en el
// handoff. No son capturas reales de SCBA: eso se valida en slice 4.
public class AjaxDeltaParserTests
{
    private static string LeerFixture(string nombre) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nombre));

    [Fact]
    public void ConResultados_extrae_viewstate_generator_y_estado()
    {
        var delta = LeerFixture("delta-con-resultados.txt");

        var r = AjaxDeltaParser.Parsear(delta);

        Assert.Equal(EstadoConsulta.ConResultados, r.Estado);
        Assert.Equal("/wEPDwUKMTIzNDU2Nzg5MGRkFAKE_VS_RESULTADOS", r.ViewState);
        Assert.Equal("CA0B0334", r.ViewStateGenerator);
        Assert.Contains("GridConsulta", r.HtmlPanel);
    }

    [Fact]
    public void SinResultados_detecta_el_estado()
    {
        var r = AjaxDeltaParser.Parsear(LeerFixture("delta-sin-resultados.txt"));

        Assert.Equal(EstadoConsulta.SinResultados, r.Estado);
        Assert.Equal("/wEPDwUKMTIzNDU2Nzg5MGRkFAKE_VS_SINRES", r.ViewState);
    }

    [Fact]
    public void DemasiadosResultados_detecta_el_estado()
    {
        var r = AjaxDeltaParser.Parsear(LeerFixture("delta-demasiados-resultados.txt"));

        Assert.Equal(EstadoConsulta.DemasiadosResultados, r.Estado);
        Assert.Equal("/wEPDwUKMTIzNDU2Nzg5MGRkFAKE_VS_DEMASIADOS", r.ViewState);
    }

    [Fact]
    public void TextoBasura_devuelve_Error_sin_explotar()
    {
        var r = AjaxDeltaParser.Parsear("esto no es un delta valido");

        Assert.Equal(EstadoConsulta.Error, r.Estado);
        Assert.Null(r.ViewState);
        Assert.Equal(string.Empty, r.HtmlPanel);
    }
}
