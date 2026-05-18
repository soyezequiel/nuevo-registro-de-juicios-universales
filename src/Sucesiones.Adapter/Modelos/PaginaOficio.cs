namespace Sucesiones.Adapter.Modelos;

// Una página del oficio ya bajada de SCBA: los bytes crudos de la imagen y
// el Content-Type que mandó SCBA (imagegen.aspx convierte el TIFF original a
// algo que el navegador sí sabe dibujar; reenviamos ese tipo tal cual, sin
// asumirlo nosotros). Es lo que el endpoint /oficio le pasa al <img>.
public record PaginaOficio(
    byte[] Bytes,
    string ContentType);
