namespace Sucesiones.Adapter.Modelos;

public record ResultadoBusqueda(
    EstadoConsulta Estado,
    IReadOnlyList<ResultadoSucesion> Filas,
    string? Mensaje = null);
