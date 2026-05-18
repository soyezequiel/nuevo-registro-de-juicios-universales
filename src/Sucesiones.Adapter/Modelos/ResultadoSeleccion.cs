namespace Sucesiones.Adapter.Modelos;

// Lo que el adapter le devuelve al front cuando se clickea "Ver" en una fila.
// No lleva el Nocache ni las URLs de SCBA: el front solo necesita saber si
// salió bien y cuántas páginas tiene para armar el visor. Si Ok es false,
// Mensaje explica por qué (sesión vencida, SCBA caído, etc.); nada de
// fallback silencioso.
public record ResultadoSeleccion(
    bool Ok,
    int CantidadPaginas,
    string? Mensaje = null);
