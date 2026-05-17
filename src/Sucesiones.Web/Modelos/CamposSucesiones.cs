namespace Sucesiones.Web.Modelos;

public record CampoConsulta(string Etiqueta, string Nombre);

// MVP: solo Sucesiones (dropAplicaciones=1). El resto de los tipos del handoff
// (Quiebras, CC, Fichas) queda como deuda fuera del MVP.
public static class CamposSucesiones
{
    public static readonly IReadOnlyList<CampoConsulta> Todos =
    [
        new CampoConsulta("Año", "RJUaño"),
        new CampoConsulta("Num Oficio", "RJUnum_oficio"),
        new CampoConsulta("Apellido", "RJUapellido"),
        new CampoConsulta("Nombre", "RJUnombre"),
        new CampoConsulta("Num Documento", "RJUnum_documento"),
        new CampoConsulta("Fecha fallec.", "RJUfallecimiento"),
        new CampoConsulta("Variante Nombre", "RJUvariante_nombre"),
        new CampoConsulta("Variante Apellido", "RJUvariante_apellido"),
    ];
}
