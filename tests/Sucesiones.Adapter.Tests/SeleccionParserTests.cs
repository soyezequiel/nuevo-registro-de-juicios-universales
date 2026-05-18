using Sucesiones.Adapter.Parsing;

namespace Sucesiones.Adapter.Tests;

// A diferencia de los otros, este fixture SÍ es una captura real de SCBA
// (respuesta del click en la lupa, 2026-05-17). Por eso el parser de
// selección queda firme y no "best-effort".
public class SeleccionParserTests
{
    private static string LeerFixture(string nombre) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nombre));

    [Fact]
    public void Extrae_nocache_y_las_paginas_reales()
    {
        var delta = LeerFixture("delta-seleccion.txt");

        var oficio = SeleccionParser.Parsear(delta);

        Assert.Equal("639146528298899340", oficio.Nocache);
        Assert.Equal(new[] { 0, 1 }, oficio.Paginas);
    }

    [Fact]
    public void Ignora_los_carteles_imagenpaginagen()
    {
        // El fixture tiene 4 <img> con query: 2 reales (imagegen.aspx) y 2
        // carteles "Página X de Y" (imagenpaginagen.aspx). Solo deben quedar
        // las 2 reales; si se colaran los carteles habría 4 páginas.
        var oficio = SeleccionParser.Parsear(LeerFixture("delta-seleccion.txt"));

        Assert.Equal(2, oficio.Paginas.Count);
    }

    [Fact]
    public void Sin_imagenes_devuelve_vacio_sin_explotar()
    {
        var oficio = SeleccionParser.Parsear(LeerFixture("delta-con-resultados.txt"));

        Assert.Empty(oficio.Paginas);
        Assert.Equal(string.Empty, oficio.Nocache);
    }
}
