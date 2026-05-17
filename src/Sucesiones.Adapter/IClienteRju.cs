using Sucesiones.Adapter.Modelos;

namespace Sucesiones.Adapter;

public interface IClienteRju
{
    Task<ResultadoBusqueda> BuscarAsync(IReadOnlyDictionary<string, string> criterios);
}
