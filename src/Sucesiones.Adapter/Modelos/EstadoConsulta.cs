namespace Sucesiones.Adapter.Modelos;

// Lo que el adapter observó en SCBA. Distinto de EstadoBusqueda (UI), que
// además tiene Inicial/Cargando, que son estados de pantalla, no del backend.
public enum EstadoConsulta
{
    ConResultados,
    SinResultados,
    DemasiadosResultados,
    Error,
}
