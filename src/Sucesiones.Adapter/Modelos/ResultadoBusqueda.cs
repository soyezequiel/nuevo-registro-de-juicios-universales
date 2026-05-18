namespace Sucesiones.Adapter.Modelos;

// SearchId es el número de ticket de la sesión SCBA donde se hizo esta
// búsqueda. El front lo guarda y lo reenvía al seleccionar una fila o pedir
// una página, para que el adapter retome la MISMA conversación con SCBA
// (mismas cookies + mismo __VIEWSTATE). El fake devuelve uno fijo y lo ignora.
public record ResultadoBusqueda(
    EstadoConsulta Estado,
    IReadOnlyList<ResultadoSucesion> Filas,
    string? Mensaje = null,
    string SearchId = "");
