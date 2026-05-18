namespace Sucesiones.Web.Modelos;

public enum GrupoCampo
{
    Principal,
    Variantes,
}

public record CampoConsulta(string Etiqueta, string Nombre, GrupoCampo Grupo, int Span);

// MVP: solo Sucesiones (dropAplicaciones=1). El rediseño reordena los campos
// para priorizar los más buscados (apellido / nombre) y agrupa las variantes
// como sección secundaria, separada por una sub-rule en el formulario.
//
// El `Nombre` de cada campo es el identificador que viaja al backend SCBA
// y NO debe cambiar (`RJUapellido`, etc. son parte del contrato observado).
public static class CamposSucesiones
{
    public static readonly IReadOnlyList<CampoConsulta> Todos =
    [
        new CampoConsulta("Apellido",            "RJUapellido",          GrupoCampo.Principal, 2),
        new CampoConsulta("Nombre",              "RJUnombre",            GrupoCampo.Principal, 2),
        new CampoConsulta("Año",                 "RJUaño",               GrupoCampo.Principal, 1),
        new CampoConsulta("Nº de oficio",        "RJUnum_oficio",        GrupoCampo.Principal, 1),
        new CampoConsulta("D.N.I. / Documento",  "RJUnum_documento",     GrupoCampo.Principal, 1),
        new CampoConsulta("Fallecimiento",       "RJUfallecimiento",     GrupoCampo.Principal, 1),
        new CampoConsulta("Variante · apellido", "RJUvariante_apellido", GrupoCampo.Variantes, 2),
        new CampoConsulta("Variante · nombre",   "RJUvariante_nombre",   GrupoCampo.Variantes, 2),
    ];
}
