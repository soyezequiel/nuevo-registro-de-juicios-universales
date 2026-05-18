namespace Sucesiones.Adapter;

// Se llena solo desde la sección "Rju" de appsettings.json al arrancar la app.
public class OpcionesRju
{
    // Dirección del sitio de SCBA. Termina en "/".
    public string UrlBase { get; set; } = "https://rju.scba.gov.ar/";

    // El interruptor: false = usa el actor falso (ClienteRjuFake, datos
    // inventados). true = habla con SCBA de verdad (ClienteRjuHttp).
    public bool ModoReal { get; set; }
}
