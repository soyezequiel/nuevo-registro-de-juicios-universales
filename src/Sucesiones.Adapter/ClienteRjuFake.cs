using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter;

// Datos ficticios y delay simulado. El adapter real (ClienteRjuHttp) llega en
// slice 4 y será el que use los criterios; este fake los ignora a propósito.
public class ClienteRjuFake : IClienteRju
{
    private static readonly IReadOnlyList<ResultadoSucesion> Resultados =
    [
        new ResultadoSucesion(0, "1998", "6619", "Gómez", "María Esther", "10.234.567", "18/05/1997", "Select$0"),
        new ResultadoSucesion(1, "2005", "12840", "Gómez", "Roberto Carlos", "8.901.234", "02/11/2004", "Select$1"),
        new ResultadoSucesion(2, "2017", "30551", "Gómez", "Ana Lucía", "27.456.789", "30/06/2016", "Select$2"),
    ];

    public async Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios)
    {
        await Task.Delay(800);
        return new ResultadoBusqueda(EstadoConsulta.ConResultados, Resultados);
    }
}
