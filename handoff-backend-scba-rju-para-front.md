# Handoff tecnico - Backend observado de SCBA RJU para construir un front nuevo

Fecha de investigacion: 2026-05-14

Sistema: Registro de Juicios Universales de SCBA

URL base:

```txt
https://rju.scba.gov.ar/
```

## Resumen para el programador

El sistema de SCBA RJU no expone una API REST publica/documentada. Lo que se
observo es una aplicacion ASP.NET WebForms que interactua con el backend usando
postbacks AJAX de Microsoft.

Se investigo ademas si existia una respuesta "limpia" directa, por ejemplo
JSON, Swagger/OpenAPI, SOAP/ASMX o WCF/SVC. Con la evidencia disponible no
aparecio ningun endpoint publico de ese tipo: las rutas convencionales
(`/api`, `/swagger`, `/openapi.json`, `*.asmx`, `*.svc`) dieron 404 y el HTML/JS
visible no referencia servicios JSON.

En terminos practicos: el front nuevo no deberia hablar "directo" con SCBA
desde el navegador del usuario como si fuera una API moderna. Lo correcto es
crear un backend propio que actue como adaptador:

```txt
Front nuevo -> Backend propio -> SCBA RJU WebForms
```

Ese backend propio tiene que manejar:

- cookies/sesion;
- certificado digital si aplica;
- campos ocultos de WebForms (`__VIEWSTATE`, `__VIEWSTATEGENERATOR`);
- respuestas `text/plain` con formato Microsoft AJAX delta;
- parseo de HTML embebido;
- obtencion de imagenes/documentos luego de seleccionar una fila.

## Conceptos clave

### WebForms

ASP.NET WebForms es una tecnologia vieja de Microsoft. La pagina mantiene estado
entre requests usando campos ocultos, principalmente `__VIEWSTATE`. Ese valor
cambia despues de cada interaccion importante.

Consecuencia: no se puede hardcodear un `__VIEWSTATE`. El backend propio debe
obtenerlo fresco, reenviarlo y actualizarlo con cada respuesta.
Cuando lo envie en un body `application/x-www-form-urlencoded`, debe ir
URL-encoded. Esto fue validado en Bruno: enviar `__VIEWSTATE` crudo puede
provocar error interno AJAX aunque el HTTP externo responda 200.

### Postback

Postback significa que el formulario se envia al mismo endpoint que renderiza la
pagina. En este caso casi todo va contra:

```txt
POST https://rju.scba.gov.ar/
```

### AJAX delta de Microsoft

La respuesta no viene en JSON. Viene como `text/plain` con un formato propio de
ASP.NET AJAX, por ejemplo:

```txt
1|#||4|9434|updatePanel|UpdatePanel1|...HTML...|0|hiddenField|__VIEWSTATE|...
```

Dentro de ese texto hay fragmentos HTML, campos ocultos actualizados y metadatos
del `UpdatePanel`.

### API limpia

No hay evidencia de una API limpia publica para RJU. En esta investigacion,
"limpia" significa una interfaz pensada para software cliente: JSON, REST
(transferencia de estado representacional), Swagger/OpenAPI, SOAP/ASMX o
WCF/SVC.

Lo que si se puede entregar al front nuevo es una API limpia propia. Ese backend
interno debe:

- ejecutar el flujo WebForms contra SCBA;
- parsear la respuesta delta `text/plain`;
- extraer filas de `GridConsulta`;
- seleccionar una fila con `GridConsulta` + `Select$N`;
- obtener las paginas reales por `imagegen.aspx`;
- devolver al front JSON estable y archivos/imagenes por endpoints propios.

Importante: no afirmar que "SCBA no tiene ninguna API". Lo correcto es decir:
"no se encontro una API publica o descubrible en las superficies verificadas".
Podria existir un servicio privado/institucional no publicado.

## Flujo completo observado

### 1. Busqueda

Request:

```http
POST https://rju.scba.gov.ar/
Content-Type: application/x-www-form-urlencoded; charset=UTF-8
X-MicrosoftAjax: Delta=true
X-Requested-With: XMLHttpRequest
Origin: https://rju.scba.gov.ar
Referer: https://rju.scba.gov.ar/
```

Body `x-www-form-urlencoded`:

```txt
ScriptManager1=ScriptManager1|btnBuscar
__EVENTTARGET=
__EVENTARGUMENT=
__LASTFOCUS=
__VIEWSTATE=<VIEWSTATE_INICIAL_O_ACTUAL>
__VIEWSTATEGENERATOR=CA0B0334
HiddenScrollbarPosition=0
altoBody=
dropAplicaciones=1
RJUaño=
RJUnum_oficio=
RJUapellido=<APELLIDO_BUSCADO>
RJUnombre=
RJUnum_documento=
RJUfallecimiento=
RJUvariante_nombre=
RJUvariante_apellido=
__ASYNCPOST=true
btnBuscar=Realizar búsqueda
```

Notas:

- En algunos HAR/Fiddler puede verse `RJUa%C3%B1o` en vez de `RJUaño`, porque
  el body esta codificado como `application/x-www-form-urlencoded`.
- `dropAplicaciones=1` corresponde a sucesiones.
- `RJUapellido` fue el criterio probado en la captura.
- El backend propio deberia soportar mas criterios, no solo apellido.
- En requests manuales, codificar tambien `__VIEWSTATE`. En la coleccion Bruno
  se usa `rju_viewstate_encoded`.

Respuesta exitosa esperada:

```txt
Content-Type: text/plain; charset=utf-8
```

La respuesta debe contener:

```txt
updatePanel|UpdatePanel1
Resultados obtenidos
GridConsulta
hiddenField|__VIEWSTATE|...
```

Ejemplo estructural de tabla devuelta:

```html
<table id="GridConsulta">
  <tr>
    <th>Ver</th>
    <th>año</th>
    <th>num_oficio</th>
    <th>apellido</th>
    <th>nombre</th>
    <th>num_documento</th>
    <th>fallecimiento</th>
  </tr>
  <tr>
    <td>
      <input
        type="image"
        src="./Images/Lupa.png"
        alt="Ver oficio"
        onclick="javascript:__doPostBack('GridConsulta','Select$0');return false;" />
    </td>
    <td>...</td>
    <td>...</td>
    <td>...</td>
    <td>...</td>
    <td>...</td>
    <td>...</td>
  </tr>
</table>
```

Columnas observadas:

| Campo visible | Significado |
| --- | --- |
| `Ver` | boton/lupa para seleccionar la fila |
| `año` | año del registro |
| `num_oficio` | numero de oficio |
| `apellido` | apellido del causante/persona |
| `nombre` | nombre |
| `num_documento` | numero de documento |
| `fallecimiento` | fecha de fallecimiento |

Cada fila trae un indice de seleccion:

```txt
Select$0
Select$1
Select$2
...
```

Ese indice es necesario para abrir el oficio/detalle.

### 2. Seleccionar una fila de la grilla

Luego de la busqueda, hay que usar el `__VIEWSTATE` actualizado que vino en la
respuesta anterior. No usar el `__VIEWSTATE` inicial.

Request:

```http
POST https://rju.scba.gov.ar/
Content-Type: application/x-www-form-urlencoded; charset=UTF-8
X-MicrosoftAjax: Delta=true
X-Requested-With: XMLHttpRequest
Origin: https://rju.scba.gov.ar
Referer: https://rju.scba.gov.ar/
```

Body observado para seleccionar la primera fila:

```txt
ScriptManager1=UpdatePanel1|GridConsulta
HiddenScrollbarPosition=0
altoBody=
dropAplicaciones=1
RJUaño=
RJUnum_oficio=
RJUapellido=<APELLIDO_BUSCADO>
RJUnombre=
RJUnum_documento=
RJUfallecimiento=
RJUvariante_nombre=
RJUvariante_apellido=
__EVENTTARGET=GridConsulta
__EVENTARGUMENT=Select$0
__LASTFOCUS=
__VIEWSTATE=<VIEWSTATE_ACTUALIZADO_DE_LA_BUSQUEDA>
__VIEWSTATEGENERATOR=CA0B0334
__ASYNCPOST=true
```

Para seleccionar otra fila:

```txt
__EVENTARGUMENT=Select$1
__EVENTARGUMENT=Select$2
__EVENTARGUMENT=Select$3
```

etc.

Respuesta esperada:

- `text/plain; charset=utf-8`
- nuevo `__VIEWSTATE`
- habilitacion del documento/oficio en la sesion;
- deteccion de un token `nocache`;
- luego el navegador carga endpoints de imagen/documento.

### 3. Cargar imagenes/documento del oficio

Despues del postback de seleccion, el navegador carga recursos como:

```http
GET https://rju.scba.gov.ar/imagegen.aspx?pagina=0&nocache=<valor>
GET https://rju.scba.gov.ar/imagegen.aspx?pagina=1&nocache=<valor>
```

Observaciones:

- `imagegen.aspx?pagina=0` parece ser la primera pagina o imagen principal.
- `imagegen.aspx?pagina=1` corresponde a la segunda pagina real del documento.
- `imagenpaginagen.aspx?pagina=...` y rutas similares pueden devolver imagenes
  auxiliares del visor, como carteles de `Pagina 1` / `Pagina 2`; no tratarlas
  como documento judicial.
- El parametro `nocache` cambia y probablemente evita cacheo.
- Estas imagenes/documentos dependen de la sesion donde se hizo `Select$N`.
  No asumir que se pueden abrir sin haber seleccionado una fila antes.

Tambien se observo:

```http
GET https://rju.scba.gov.ar/Images/descargar.png
```

Eso es solo un icono/recurso visual, no el documento.

## Tipos de consulta conocidos

El selector `dropAplicaciones` usa estos valores:

| Valor | Tipo |
| --- | --- |
| `1` | Sucesiones |
| `2` | Fichas_Sucesiones |
| `8` | CC SUCESIONES |
| `11` | Quiebras |
| `12` | Fichas_Quiebras |
| `15` | CC Quiebras |

## Campos conocidos por tipo

### Sucesiones (`dropAplicaciones=1`)

| Label | Campo |
| --- | --- |
| Año | `RJUaño` |
| Num Oficio | `RJUnum_oficio` |
| Apellido | `RJUapellido` |
| Nombre | `RJUnombre` |
| Num Documento | `RJUnum_documento` |
| Fecha fallec. | `RJUfallecimiento` |
| Variante Nombre | `RJUvariante_nombre` |
| Variante Apellido | `RJUvariante_apellido` |

### Fichas_Sucesiones (`dropAplicaciones=2`)

| Label | Campo |
| --- | --- |
| Num Oficio | `RJUnum_oficio` |
| Año | `RJUaño` |

### CC SUCESIONES (`dropAplicaciones=8`)

| Label | Campo |
| --- | --- |
| Numero CC | `RJUnumero_cc` |
| Año CC | `RJUaño_cc` |
| Numero Oficio | `RJUnumero_oficio` |
| Año Oficio | `RJUaño_oficio` |

### Quiebras (`dropAplicaciones=11`)

| Label | Campo |
| --- | --- |
| Año | `RJUaño` |
| Num Oficio | `RJUnum_oficio` |
| Apellido | `RJUapellido` |
| Nombre | `RJUnombre` |
| Variante Nombre | `RJUvariante_nombre` |
| Variante Apellido | `RJUvariante_apellido` |
| Tipo Documento | `RJUtipo_documento` |
| Num Documento | `RJUnum_documento` |
| Razon Social | `RJUrazon_social` |
| CUIT | `RJUcuit` |
| Causa | `RJUcausa` |
| Año Causa | `RJUaño_causa` |

### Fichas_Quiebras (`dropAplicaciones=12`)

| Label | Campo |
| --- | --- |
| Num Oficio | `RJUnum_oficio` |
| Año | `RJUaño` |
| Num CC | `RJUnum_cc` |
| Año CC | `RJUaño_cc` |

### CC Quiebras (`dropAplicaciones=15`)

| Label | Campo |
| --- | --- |
| Numero CC | `RJUnumero_cc` |
| Año CC | `RJUaño_cc` |
| Numero Oficio | `RJUnumero_oficio` |
| Año Oficio | `RJUaño_oficio` |

## Respuestas y estados observados

### Resultados obtenidos

La respuesta contiene:

```txt
Resultados obtenidos
GridConsulta
```

Y una tabla HTML con filas.

### Sin resultados

La respuesta puede contener:

```txt
Sin resultados
```

### Demasiados resultados

La respuesta puede contener:

```txt
Su busqueda genera demasiados resultados, modifique los criterios ingresados y vuelva a intentarlo.
```

Segun el instructivo oficial, cuando hay demasiadas coincidencias se deben
agregar criterios adicionales.

## Diseno recomendado del backend propio

### Responsabilidad del backend propio

No exponer SCBA directamente al front. Crear un servicio propio con endpoints
claros, por ejemplo:

```txt
POST /api/scba/rju/search
POST /api/scba/rju/select
GET  /api/scba/rju/document-page
```

o una variante mas orientada al producto:

```txt
POST /api/juicios-universales/busquedas
GET  /api/juicios-universales/busquedas/:id/resultados
POST /api/juicios-universales/busquedas/:id/resultados/:index/seleccionar
GET  /api/juicios-universales/documentos/:documentoId/paginas/:pagina
```

### Por que no hacerlo directo desde el front

No conviene que el front nuevo llame directamente a SCBA porque:

- el navegador tendria problemas de CORS (Cross-Origin Resource Sharing,
  intercambio de recursos entre origenes);
- se expondria demasiado el contrato fragil de SCBA;
- habria que manejar `__VIEWSTATE` en el cliente;
- habria datos personales en HTML crudo;
- el certificado digital/sesion es una preocupacion de backend/infraestructura,
  no de UI.

### Estado de sesion

El backend propio tiene que mantener una sesion por busqueda o por usuario:

```txt
sessionId interno
cookies SCBA
ultimo __VIEWSTATE
ultimo __VIEWSTATEGENERATOR
criterios enviados
resultados normalizados
timestamp
```

### Modelo sugerido de respuesta para el front

Busqueda:

```json
{
  "searchId": "uuid-interno",
  "status": "results",
  "source": "SCBA_RJU",
  "query": {
    "type": "sucesiones",
    "apellido": "..."
  },
  "results": [
    {
      "index": 0,
      "year": "1970",
      "officeNumber": "6619",
      "lastName": "...",
      "firstName": "...",
      "documentNumber": "...",
      "deathDate": "18/05/1969",
      "selectArgument": "Select$0"
    }
  ]
}
```

Sin resultados:

```json
{
  "searchId": "uuid-interno",
  "status": "no_results",
  "results": []
}
```

Demasiados resultados:

```json
{
  "searchId": "uuid-interno",
  "status": "too_many_results",
  "message": "La busqueda genera demasiados resultados. Agregue mas criterios."
}
```

Seleccion:

```json
{
  "searchId": "uuid-interno",
  "selectedIndex": 0,
  "document": {
    "documentId": "uuid-interno",
    "pages": [
      {
        "page": 0,
        "url": "/api/scba/rju/documents/uuid-interno/pages/0"
      }
    ]
  }
}
```

## Parser necesario

El backend propio debe parsear dos cosas:

1. El formato Microsoft AJAX delta.
2. El HTML de `GridConsulta`.

### Parseo del AJAX delta

Objetivo minimo:

- extraer fragmento de `UpdatePanel1`;
- extraer `__VIEWSTATE` actualizado;
- extraer `__VIEWSTATEGENERATOR`;
- detectar mensajes (`Resultados obtenidos`, `Sin resultados`, demasiados
  resultados).

### Parseo de `GridConsulta`

Objetivo:

- localizar `<table id="GridConsulta">`;
- leer headers;
- leer filas;
- extraer `Select$N` desde `onclick`;
- normalizar columnas.

No parsear con regex si se puede evitar. Usar un parser HTML real:

- Node.js: `cheerio` o `node-html-parser`;
- Python: `BeautifulSoup`/`lxml`;
- .NET: `HtmlAgilityPack`.

## Seguridad y cumplimiento

Este sistema devuelve datos personales y documentos judiciales. Tratar como
informacion sensible.

Recomendaciones:

- No loguear HTML completo en produccion.
- No guardar documentos sin justificacion funcional/legal.
- Redactar datos personales en logs.
- Registrar auditoria de usuario, fecha, criterios de busqueda y motivo.
- Limitar frecuencia de consultas.
- Evitar consultas masivas.
- Definir retencion de datos.

## Riesgos tecnicos

1. `__VIEWSTATE` cambia y puede expirar.
2. SCBA puede cambiar HTML sin aviso.
3. El endpoint de imagen depende de sesion.
4. Fiddler Classic degrada HTTP/2 a HTTP/1.1 en la captura; no asumir que eso
   cambia el contrato funcional, pero si documentar que la evidencia viene de
   captura proxificada.
5. La app puede depender de certificado digital del navegador/usuario.
6. Puede haber diferencias entre tipos de consulta (`Sucesiones`, `Quiebras`,
   `CC`, etc.).

## Pruebas minimas para el adaptador

### Tests unitarios

- Parsear respuesta con `GridConsulta`.
- Parsear respuesta `Sin resultados`.
- Parsear respuesta `too_many_results`.
- Extraer `__VIEWSTATE` nuevo.
- Extraer `Select$N`.

### Tests de integracion controlados

- GET inicial obtiene `__VIEWSTATE`.
- POST busqueda devuelve estado reconocido.
- POST seleccion con `Select$0` habilita imagen.
- GET imagen pagina 0 devuelve contenido visual esperado.

### Tests del front

El front no deberia depender de HTML SCBA. Debe consumir JSON normalizado del
backend propio.

Estados que debe soportar:

- idle;
- cargando;
- resultados;
- sin resultados;
- demasiados resultados;
- error de sesion/certificado;
- error de SCBA no disponible;
- documento cargando;
- documento con varias paginas;
- documento no disponible.

## Checklist para continuar investigacion

- Capturar y guardar HAR del click en lupa completo.
- Verificar si existe boton real de descarga y que request dispara.
- Confirmar si las imagenes son TIFF/PNG/JPEG o HTML que renderiza imagenes.
- Verificar comportamiento con `Quiebras`.
- Confirmar si `imagegen.aspx` requiere cookies de la misma sesion.
- Definir si el backend correra en una maquina con certificado digital
  instalado o si el flujo sera asistido por usuario.

## Conclusion

La informacion necesaria para construir un front nuevo no es solo "endpoint y
payload". El front nuevo necesita un backend intermediario que traduzca un
sistema WebForms estatal y con estado a una API interna estable.

El contrato externo observado es:

```txt
POST /                       -> busqueda AJAX
POST / + GridConsulta Select -> seleccion de fila/oficio
GET /imagegen.aspx           -> paginas reales del oficio
```

El contrato interno recomendado para el front deberia ser JSON propio,
versionado y estable.
