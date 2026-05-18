# Plan del proyecto — Sucesiones (rediseño RJU)

> Documento vivo. Se actualiza al cerrar cada rebanada o al cambiar una decisión.
> Última actualización: 2026-05-18.

## Contexto

Rediseño de la pantalla pública de "Sucesiones" del Registro de Juicios Universales (SCBA). Proyecto de aprendizaje con foco en:

- .NET Razor Pages + Blazor Server.
- Pipeline de Tailwind sin Node.
- Construir un backend adapter contra un sistema WebForms legacy.
- Testing (unitario, integración, E2E, regresión, API).
- UX: mejorar una interfaz pública sin tocar el sistema original.

Documentos de referencia:

1. `METODOLOGIA_GENERICA.md` — reglas de trabajo (rebanadas verticales, ciclo de 5 pasos).
2. `handoff-backend-scba-rju-para-front.md` — contrato observado del backend SCBA.

## Stack confirmado

| Capa | Tecnología |
|---|---|
| Runtime | .NET 9 (SDK 9.0.313) |
| Web | Razor Pages como shell del sitio |
| Interactividad | Blazor Server embebido como isla (`<component>` tag helper) |
| Estilos | Tailwind CSS — CLI standalone (sin Node) |
| Scripts cliente | JavaScript vanilla solo si es estrictamente necesario |
| Backend adapter | C# class library separado (slice 3+) |
| HTTP a SCBA | `HttpClient` + `HtmlAgilityPack` para parsear `GridConsulta` |
| Tests | xUnit (TBD, se decide en slice 3) |
| Versionado | Git (lo maneja el usuario, fuera de scope del asistente) |
| Base de datos | No hay en este proyecto |
| React / Material UI | Fuera de scope |

**Paleta visual:** azul + negro, moderno, limpio, responsive.

## Convenciones del proyecto

### Nombres en español

Lo que escribimos nosotros va en español. Lo que es del framework o del contrato externo se queda como está.

**En español:**
- Archivos custom: `Componentes/PanelBusqueda.razor`, `Modelos/ResultadoSucesion.cs`.
- Clases, propiedades, métodos, variables: `Busqueda`, `RealizarBusqueda()`, `apellidoBuscado`.
- Carpetas custom: `Componentes/`, `Modelos/`, `Servicios/`.
- IDs y clases CSS propias: `panel-busqueda`, `tabla-resultados`.

**Obligatoriamente en inglés:**
- Palabras reservadas de C# / Razor: `class`, `public`, `@page`, `@model`.
- Métodos del framework que sobrescribimos: `OnGet`, `OnPost`, `Main`, `InvokeAsync`.
- Archivos del template `dotnet new razor`: `Program.cs`, `Pages/Index.cshtml`, `_Layout.cshtml`, `appsettings.json`.
- Clases utilitarias de Tailwind: `flex`, `bg-blue-900`, `rounded-lg`.
- Identificadores del contrato SCBA: `RJUapellido`, `__VIEWSTATE`, `Select$N`.

### Capitalización C#

- Clases, métodos, propiedades públicas: `PascalCase`.
- Variables locales y parámetros: `camelCase`.

### Tamaño de rebanadas

Máximo **~10–15 archivos por iteración**. Si una rebanada se pasa, se corta antes de empezar.

## Roadmap de rebanadas

### Slice 1 — Esqueleto + home estática *(cerrada 2026-05-16)*

Una app Razor Pages que arranca, con Blazor Server habilitado y Tailwind compilando. La home muestra título, dropdown placeholder, formulario placeholder, tabla vacía y los estados visuales (sin lógica de búsqueda real).

**Pasos:**

| # | Quién | Qué |
|---|---|---|
| 1 | Usuario | `dotnet new sln -n Sucesiones` |
| 2 | Usuario | `dotnet new razor -n Sucesiones.Web -o src/Sucesiones.Web --framework net9.0` |
| 3 | Usuario | `dotnet sln Sucesiones.sln add src/Sucesiones.Web/Sucesiones.Web.csproj` |
| 4 | Usuario | `dotnet build` y `dotnet run`, verificar home default en el browser. |
| 5 | Asistente | Crear `CLAUDE.md` con metodología + stack + convenciones. |
| 6 | Asistente | Setup Tailwind CLI standalone: guía para bajar `tailwindcss.exe` a `tools/`, crear `tailwind.config.js`, `wwwroot/css/entrada.css`, script de build. |
| 7 | Asistente | Habilitar Blazor Server en `Program.cs` (`AddServerSideBlazor` + `MapBlazorHub`). |
| 8 | Asistente | Crear `_Imports.razor` y `Componentes/PanelBusqueda.razor` (UI estática, sin lógica). |
| 9 | Asistente | Editar `Pages/Index.cshtml` y `Pages/Shared/_Layout.cshtml` para embeber el componente y aplicar el layout azul/negro. |
| 10 | Asistente | Walkthrough: qué cambió, cómo probarlo, qué quedó pendiente. |
| 11 | Usuario | Probar, confirmar o pedir ajustes. |

**Archivos que toca el asistente:** ~8–9.

**Fuera de Slice 1:**
- Lógica de búsqueda (mock o real).
- Formulario dinámico según dropdown — estará hardcodeado a Sucesiones.
- Tabla con datos — estará vacía/placeholder.

### Slice 2 — Formulario de Sucesiones + mock en memoria *(en implementación)*

> **Cambio de alcance (2026-05-17):** el MVP cubre **solo Sucesiones**. El
> formulario dinámico multi-tipo (Quiebras, CC, Fichas) queda como **deuda
> fuera del MVP**. Se sacó el dropdown y el código se acotó a Sucesiones.

Formulario fijo de Sucesiones (8 campos del handoff). Botón "Buscar" devuelve resultados hardcodeados desde una clase fake. Estados visuales reales: idle, cargando, resultados, sin resultados, error.

**Plan acordado (chat anterior), ~6 archivos:**

| # | Archivo | Qué |
|---|---|---|
| 1 | `Modelos/TiposConsulta.cs` (nuevo) | Los 6 tipos del dropdown + qué campos tiene cada uno (datos del handoff) |
| 2 | `Modelos/ResultadoSucesion.cs` (nuevo) | DTO de una fila de resultado (año, oficio, apellido, juzgado…) |
| 3 | `Modelos/EstadoBusqueda.cs` (nuevo) | Enum: `Inicial`, `Cargando`, `ConResultados`, `SinResultados`, `Error` |
| 4 | `Servicios/BuscadorFake.cs` (nuevo) | Devuelve resultados hardcodeados con un delay simulado |
| 5 | `Program.cs` (edit) | Registrar `BuscadorFake` en el DI |
| 6 | `Componentes/PanelBusqueda.razor` (reescribir `@code` + bindings) | Dropdown cambia campos; "Buscar" llama al fake; render según estado |

**Decisiones acordadas:**
- Sin interfaz `IClienteRju` todavía — la abstracción del adapter es slice 3 (no abstracción prematura).
- Delay simulado (`Task.Delay(800)`) para que el estado `Cargando` sea visible. Fake honesto, explícito en el código.
- Solo Sucesiones y Quiebras devuelven resultados fake; los otros tipos arman el form pero el fake devuelve `SinResultados` (no inventar datos de tipos no investigados).

### Slice 3 — Adapter como class library + tests unitarios

Sacar la lógica del adapter a un proyecto separado `Sucesiones.Adapter`. Definir interfaz `IClienteRju` con implementación `ClienteRjuFake`. Crear proyecto `Sucesiones.Adapter.Tests` con xUnit. Tests unitarios sobre parser de AJAX delta y de `GridConsulta` usando fixtures HTML guardados.

### Slice 4 — Adapter real contra SCBA *(cerrada 2026-05-17)*

`ClienteRjuHttp` con `HttpClient`: GET inicial → lee `__VIEWSTATE`/`__VIEWSTATEGENERATOR` de los inputs ocultos → body `x-www-form-urlencoded` armado a mano → POST con headers AJAX → `AjaxDeltaParser` + `GridConsultaParser`. Cookies vía `HttpClientHandler.CookieContainer`. Toggle `Rju:ModoReal` en `appsettings.json` elige fake vs real en `Program.cs`. Errores de red/SCBA/timeout → `EstadoConsulta.Error`, sin fallback silencioso.

**Fuera de Slice 4 (deuda anotada):**
- Manejo de certificado digital del navegador (si SCBA lo exige, el modo real devuelve `Error`).
- Sesión aislada por búsqueda: el `CookieContainer` se comparte entre búsquedas porque `IHttpClientFactory` reusa el handler ~2 min. Necesario resolverlo para `Select$N` en slice 5.
- Tests del flujo HTTP (se decidió no agregarlos en este slice).

### Slice 5 — Selección de fila + visor de páginas

> **Cambio de alcance (2026-05-18):** se partió en 5a y 5b por tamaño
> (~13 archivos). Se consiguió una **captura real** de la respuesta del
> `Select$N` (fixture `delta-seleccion.txt`), así que el parser de selección
> quedó firme, no best-effort.

#### Slice 5a — Selección + sesión + visor de la página 0 *(cerrada 2026-05-18)*

Postback `Select$N` reusando la sesión de la búsqueda. Sesión aislada por
búsqueda: `AlmacenSesionesRju` (singleton) guarda una `SesionRju` por
`searchId` (HttpClient + cookies + último `__VIEWSTATE` + criterios + nocache),
con vencimiento a 20 min. Esto resuelve la deuda de slice 4 (handler
compartido). `SeleccionParser` saca `nocache` + páginas del `UpdatePanel2`
(ignora los carteles `imagenpaginagen`). Endpoint propio
`GET /oficio/{searchId}/pagina/{n}` proxea `imagegen.aspx` por la sesión.
El visor es un modal Blazor que muestra la página 0 (cache-bust por token
en la URL del `<img>` para que cambie al seleccionar otra fila).

#### Slice 5b — Visor multi-página

Navegación entre páginas en el modal (el parser de 5a ya extrae todas las
páginas; falta solo la UI de navegación).

### Slice 6 — Manejo de "demasiados resultados" + auditoría

Detectar el mensaje de demasiados resultados y mostrar un estado visual claro pidiendo más criterios. Log estructurado de búsquedas (sin datos personales en claro).

### Slice 7+ — Tests E2E, regresión, DevOps

- Playwright contra el front.
- Tests de regresión sobre fixtures del parser.
- Pipeline de CI (GitHub Actions o similar).
- Investigación de despliegue.

## Decisiones tomadas y por qué

| Decisión | Por qué |
|---|---|
| Razor Pages, no MVC | Una página = un archivo. Encaja con "form + tabla". Menos ceremonia. |
| Razor Pages + isla Blazor Server, no Blazor entero | Aprender los dos modelos en un solo proyecto. Razor para el shell, Blazor para la interactividad de búsqueda. |
| Tailwind CLI standalone, no npm | Evita sumar Node al stack solo para CSS. Binario único. |
| Sin Bootstrap | El usuario ya sabe HTML/CSS — Tailwind enseña utility-first sin tapar fundamentos. |
| Sin SQL Server | No hay persistencia propia. Los datos viven en SCBA. Si después se agrega auditoría, se decide en su slice. |
| Sin React | El proyecto es Razor por requisito. React se practica en otro proyecto. |
| Adapter en proyecto separado (slice 3+) | Permite tests unitarios sin levantar la web. Es la forma canónica .NET de aislar dependencias. |
| Nombres en español | Proyecto de aprendizaje — bajar fricción de lectura. Solo afecta lo que escribimos nosotros. |
| Tailwind v4 (no v3) | El binario standalone más nuevo es v4. Config CSS-first con `@import`/`@source`/`@theme` en `entrada.css`; sin `tailwind.config.js`. |
| Versionar `.vscode/launch.json` y `tasks.json` | F5 levanta el debugger sin configurar en cualquier máquina. El resto de `.vscode/` se ignora. |
| `docs/flujo-y-glosario.md` | Material de aprendizaje del flujo .NET/Blazor (analogía de restaurante + glosario). No es contrato del proyecto. |
| Sin comentarios de aprendizaje en código | Se probó con tags `[BASE]/[BUSCAR]/...` y generaban ruido. Código auto-descriptivo en español; comentario solo para gotchas reales. |
| MVP solo Sucesiones (2026-05-17) | Acotar el alcance: el resto de los tipos de consulta del handoff (Quiebras, CC, Fichas) es deuda fuera del MVP. Se eliminó el dropdown y el código se redujo a Sucesiones. |
| Toggle fake/real por config (`Rju:ModoReal`) | El fake no se borra al llegar el adapter real: sirve para desarrollo y demo sin depender de SCBA. Un solo interruptor en `appsettings.json` cambia la implementación en el DI sin tocar código. |
| Body urlencoded armado a mano | Control exacto sobre nombres/valores de los campos del handoff y garantizar que `__VIEWSTATE` viaje URL-encoded (mandarlo crudo da error interno AJAX aunque el HTTP responda 200). |
| Sesión aislada por búsqueda (slice 5a) | `imagegen.aspx` y `Select$N` dependen de la sesión donde se buscó. Un almacén singleton con HttpClient+cookies propios por `searchId` lo garantiza y resuelve la deuda del handler compartido de slice 4. En memoria y un solo proceso: alcanza para el MVP. |
| Cache-bust en la URL del visor | El navegador reusaba la primera imagen porque la URL `/oficio/{id}/pagina/0` no cambiaba entre filas. Un token por selección la hace única y fuerza el re-fetch (mismo principio que el `nocache` de SCBA). |

## Riesgos abiertos (del handoff)

1. `__VIEWSTATE` puede expirar entre búsqueda y selección.
2. SCBA puede cambiar el HTML sin aviso → tests de regresión sobre fixtures.
3. `imagegen.aspx` depende de la sesión donde se hizo `Select$N`.
4. Posible requerimiento de certificado digital del navegador.
5. Diferencias entre tipos de consulta (Sucesiones, Quiebras, CC).

Estos se atacan en las slices 4 y 5.

## Comandos del proyecto

*(Se completan a medida que se construye.)*

```powershell
# Crear solución (slice 1)
dotnet new sln -n Sucesiones
dotnet new razor -n Sucesiones.Web -o src/Sucesiones.Web --framework net9.0
dotnet sln Sucesiones.sln add src/Sucesiones.Web/Sucesiones.Web.csproj

# Build y run
dotnet build
dotnet run --project src/Sucesiones.Web

# Tailwind (pendiente, se completa en slice 1 paso 6)
# .\tools\tailwindcss.exe -i ... -o ... --watch
```
