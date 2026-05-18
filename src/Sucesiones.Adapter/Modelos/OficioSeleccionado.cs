namespace Sucesiones.Adapter.Modelos;

// Lo que SeleccionParser saca del UpdatePanel2 de la respuesta del Select$N:
// el token que SCBA exige en cada pedido de imagen y la lista de páginas
// reales del oficio. Es interno del adapter: el front nunca ve el Nocache
// (ese viaja escondido en la sesión, no tiene por qué cruzar al navegador).
public record OficioSeleccionado(
    string Nocache,
    IReadOnlyList<int> Paginas);
