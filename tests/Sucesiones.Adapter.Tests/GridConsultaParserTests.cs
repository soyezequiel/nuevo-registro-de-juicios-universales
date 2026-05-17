using Sucesiones.Adapter.Parsing;

namespace Sucesiones.Adapter.Tests;

public class GridConsultaParserTests
{
    private static string LeerFixture(string nombre) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nombre));

    [Fact]
    public void Parsea_las_filas_de_GridConsulta()
    {
        var delta = LeerFixture("delta-con-resultados.txt");
        var html = AjaxDeltaParser.Parsear(delta).HtmlPanel;

        var filas = GridConsultaParser.Parsear(html);

        Assert.Equal(2, filas.Count);

        Assert.Equal(0, filas[0].Indice);
        Assert.Equal("1998", filas[0].Anio);
        Assert.Equal("6619", filas[0].NumOficio);
        Assert.Equal("Gómez", filas[0].Apellido);
        Assert.Equal("María Esther", filas[0].Nombre);
        Assert.Equal("10.234.567", filas[0].NumDocumento);
        Assert.Equal("18/05/1997", filas[0].Fallecimiento);
        Assert.Equal("Select$0", filas[0].ArgumentoSeleccion);

        Assert.Equal(1, filas[1].Indice);
        Assert.Equal("Roberto Carlos", filas[1].Nombre);
        Assert.Equal("Select$1", filas[1].ArgumentoSeleccion);
    }

    [Fact]
    public void Sin_tabla_GridConsulta_devuelve_lista_vacia()
    {
        var html = AjaxDeltaParser.Parsear(LeerFixture("delta-sin-resultados.txt")).HtmlPanel;

        var filas = GridConsultaParser.Parsear(html);

        Assert.Empty(filas);
    }
}
