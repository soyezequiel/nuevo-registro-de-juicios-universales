# Flujo del programa y glosario

> Material de aprendizaje. Explica, al detalle, qué hace cada línea donde conviene
> poner breakpoints, con una analogía de restaurante coherente entre las 3 fases.
> Los términos técnicos llevan un número `[n]` que remite al glosario del final.

---

## La analogía: un restaurante

Cada "actor" del restaurante representa una pieza concreta del programa. Esta tabla
es la clave de lectura — si algo no se entiende, volvé acá.

| Actor del restaurante | Qué es en el programa |
|---|---|
| El arquitecto y los planos | El *builder* [1] (`WebApplicationBuilder`) antes de `Build()` |
| Anotar áreas en el plano (cocina, salón) | Registrar servicios [9] con `Add*` [10] |
| La obra terminada (local cerrado y vacío) | La app después de `Build()` [15] |
| Los carteles de las ventanillas | Los endpoints [17] registrados con `Map*` [10] |
| Abrir las puertas y atender | `Run()` + el server Kestrel [16] escuchando |
| Un cliente que entra | Un request HTTP a una URL |
| El mozo asignado a esa mesa | La instancia del `PageModel` [24] creada para ese request |
| La libreta de comandas del mozo | El logger [6] inyectado [8] en el constructor [25] |
| Tomar el pedido y preparar la mesa | El handler [27] `OnGet` [29] |
| Servir el plato | Renderizar el HTML y mandarlo al browser |
| La pizarra que se actualiza sola en la mesa | Un componente Blazor [31] |
| La foto del plato en el menú | El prerender [33] del componente |
| El plato real que se come | El componente ya hidratado [34] en el circuit [12] |

---

## Fase 1 — Arranque del servidor (corre una sola vez)

Archivo: `src/Sucesiones.Web/Program.cs`.

**Actores en esta fase:**

| Actor del restaurante | Qué es en el programa |
|---|---|
| El arquitecto con planos en blanco | El *builder* [1] (`WebApplicationBuilder`) recién creado |
| Anotar áreas en el plano (cocina, salón) | Registrar servicios [9] con `Add*` [10] |
| Cerrar los planos / obra terminada | `Build()` [15] — el local existe pero cerrado y vacío |
| Poner los carteles de las ventanillas | Registrar endpoints [17] con `Map*` [10] |
| Abrir las puertas y quedarse atendiendo | `Run()` + el server Kestrel [16] escuchando |

### `var creadorApp = WebApplication.CreateBuilder(args);`

**Qué hace:** crea el *builder* [1] de la aplicación. Es el "armador" del server, no el server todavía.

**Por dentro:**
- Lee `appsettings.json` y `appsettings.Development.json` (la configuración [2]).
- Lee `Properties/launchSettings.json` [3] para saber qué puertos usar (5118 / 7206).
- Lee las variables de entorno [4] (`ASPNETCORE_ENVIRONMENT=Development`).
- Recibe los `args` [5] por si pasaste flags por línea de comandos.
- Configura logging [6] por consola por default.

**Estado después:** tenés un `WebApplicationBuilder` que expone tres cosas:
- `creadorApp.Services` — el contenedor de inyección de dependencias [7] **vacío**.
- `creadorApp.Configuration` — la config combinada de todas las fuentes.
- `creadorApp.Environment` — info del entorno (`IsDevelopment()`, rutas).

**Analogía:** contratás un arquitecto y le das **planos en blanco**. No hay restaurante, hay con qué diseñarlo.

### `creadorApp.Services.AddRazorPages();` y `creadorApp.Services.AddServerSideBlazor();`

**Qué hace:** registra en el contenedor DI [7] todos los servicios [9] que Razor Pages [23] y Blazor Server [30] necesitan.

**Por dentro (lo que registra `AddServerSideBlazor`):**
- El hub de SignalR [11] que recibe los eventos del browser.
- La fábrica de circuits [12] (cada browser conectado tiene su circuit).
- El renderer [13] que maneja el árbol de componentes y calcula los diffs.
- `IJSRuntime` [14] para llamar JavaScript desde C#.

**Patrón general:** todos los `Add*` [10] hacen lo mismo conceptualmente — **registran servicios** que después la app puede pedir por inyección [8]. No los crean todavía; los anotan.

**Estado después:** `creadorApp.Services` tiene ~80 servicios registrados (los ves en el debugger inspeccionando `creadorApp.Services.Count`).

**Analogía:** le decís al arquitecto "en los planos poné cocina, salón y delivery". No existen físicamente aún, quedan **previstos en el plano**.

### `var aplicacion = creadorApp.Build();`

**Qué hace:** convierte el builder [1] en una aplicación real. El contenedor DI [7] queda **sellado**: no se pueden registrar más servicios después de esta línea.

**Por dentro:**
- Cierra el `ServiceCollection` y construye el `ServiceProvider` (el contenedor "real" que resuelve dependencias [8]).
- Crea la instancia de `WebApplication` con su pipeline [20] de middleware [19] vacío y su router [18].
- Conecta el hosting y el server HTTP Kestrel [16] (sin arrancarlo aún).

**Estado después:** `aplicacion.Services` ya es resolvible. Si pedís `aplicacion.Services.GetService<ILogger<IndexModel>>()`, te lo devuelve armado.

**Por qué importa este punto:** el "antes" y el "después" de `Build()` [15] son **fases distintas**. Antes: registrás servicios. Después: mapeás rutas y corrés. No es intercambiable.

**Analogía:** el arquitecto cierra los planos y se termina la obra. El local **existe físicamente, pero está cerrado y vacío**.

### `aplicacion.MapBlazorHub();`

**Qué hace:** registra la ruta `/_blazor` como un endpoint [17] en el router [18]. Cualquier request a esa URL lo maneja el hub de componentes de Blazor.

**Por dentro:**
- Internamente llama a `MapHub<ComponentHub>("/_blazor", ...)`.
- Configura el endpoint para aceptar conexiones SignalR [11] (WebSocket [21], con long-polling como fallback).
- A partir de acá, cuando el browser cargue `blazor.server.js`, ese script se conecta a `/_blazor`. Si esta línea no estuviera, el browser no tendría con quién hablar.

**Cómo se ve en runtime:** DevTools → Network → filtro **WS** → vas a ver `wss://localhost:7206/_blazor`.

**Patrón general:** todos los `Map*` [10] **registran endpoints** (rutas). Es el par "endpoint" del `Add*` (servicio).

**Analogía:** ponés los **carteles de las ventanillas**: "Trámite X → ventanilla 3". Sin cartel, el cliente llega y no sabe a dónde ir.

### `aplicacion.Run();`

**Qué hace:** arranca el server HTTP Kestrel [16] y **bloquea el thread** principal escuchando requests para siempre.

**Por dentro:**
- Abre los sockets en los puertos configurados (5118 HTTP, 7206 HTTPS).
- Imprime en consola `Now listening on: https://localhost:7206` (la línea que el `serverReadyAction` de VS Code matchea para abrir el browser solo).
- Entra en un loop infinito esperando requests.
- Cada request entrante recorre el pipeline [20]: `UseRouting → UseAuthorization → matchear endpoint → ejecutar handler`.

**Cuándo vuelve esta llamada:** **nunca** en operación normal. Solo cuando:
- Apretás Ctrl+C en la terminal.
- El proceso recibe SIGTERM [22] (en deploy real, cuando el orquestador apaga la app).
- Crashea con excepción no manejada en el arranque.

**Si ponés breakpoint acá, el debugger frena la app ANTES de arrancar el server.** Tenés que dar F5/Continue para que la línea ejecute y el server levante.

**Analogía:** **abrís las puertas del restaurante**. El encargado se queda parado en la entrada atendiendo y no se mueve hasta que cierra el local.

---

## Fase 2 — Cada carga de página (corre en cada request)

Archivo: `src/Sucesiones.Web/Pages/Index.cshtml.cs`.

**Actores en esta fase:**

| Actor del restaurante | Qué es en el programa |
|---|---|
| Un cliente que entra | Un request HTTP a una URL (ej. GET `/`) |
| El mozo nuevo y exclusivo de esa mesa | La instancia del `PageModel` [24] creada para ese request |
| La libreta de comandas del mozo | El logger [6] inyectado [8] en el constructor [25] |
| Tomar el pedido y preparar la mesa | El handler [27] `OnGet` [29] |
| Emplatar y servir el plato | Renderizar el HTML y mandarlo al browser |

### `public IndexModel(ILogger<IndexModel> registroEventos)`

**Qué hace:** es el constructor [25] de la clase `IndexModel` (un `PageModel` [24]). ASP.NET Core [36] lo llama **una vez por cada request a `/`**.

**Lo que pasa cuando se ejecuta:**
- El router [18] recibió un GET a `/` y matcheó la página `Index`.
- ASP.NET Core necesita una instancia de `IndexModel` para procesar el request.
- Antes de instanciar, mira el constructor: pide un `ILogger<IndexModel>` [6].
- Va al contenedor DI [7], busca un proveedor de `ILogger<IndexModel>` (registrado automáticamente por `CreateBuilder`).
- Le pasa esa instancia ya armada al constructor. Eso es **inyección de dependencias** [8]: la clase no hace `new Logger()`, la recibe.

**Detalle de lifetime:** cada request crea un `IndexModel` nuevo — es scoped [26] por request. Cuando el request termina, esa instancia se descarta.

**Analogía:** entra un cliente al restaurante. El encargado le asigna **un mozo nuevo y exclusivo para esa mesa**, ya equipado con su libreta de comandas (el logger [6]). Cliente nuevo → mozo nuevo. Cuando el cliente se va, ese mozo deja de existir.

### `public void OnGet()`

**Qué hace:** es el handler [27] del método HTTP GET para esta página. Convención de Razor Pages [23]: si el request es GET se llama `OnGet` [29]; si es POST, `OnPost`; si es DELETE, `OnDelete`.

**Cuándo se ejecuta:**
- Después del constructor.
- Antes de renderizar el `Index.cshtml`.
- Acá ya tenés `this.Request`, `this.HttpContext`, `this.User` disponibles.

**Para qué se usa típicamente:**
- Leer la query string [30]: `Request.Query["q"]`.
- Cargar datos para la vista: `Productos = await db.Productos.ToListAsync();`.
- Validar permisos: `if (!User.IsInRole("admin")) return Forbid();`.
- Setear propiedades del `PageModel` [24] que la vista lee con `@Model.X`.

**Hoy en este proyecto** tiene una línea de log que agregaste para ver el ciclo:

```csharp
public void OnGet()
{
    _registroEventos.LogInformation("OnGet de Index — {Hora}", DateTime.Now);
}
```

Cada vez que refrescás `/`, esa línea escribe en la Debug Console. Eso te **demuestra en vivo** que `OnGet` corre una vez por request.

**Hermanos del método:**
- `OnGetAsync` — versión async, equivalente cuando hay `await`.
- `OnPost` — cuando enviás un formulario por POST.
- `OnPostBuscar` — Razor Pages permite **named handlers** [28]: con `<form method="post" asp-page-handler="Buscar">` tenés varios POST en una misma página (`OnPostBuscar`, `OnPostLimpiar`). Lo vamos a usar en slice 2.

**Analogía:** el mozo **toma el pedido inicial y prepara la mesa** (cubiertos, pan) antes de traer la comida. Si la mesa necesitara algo de la cocina (datos de una base), acá los iría a buscar.

### Renderizar `Index.cshtml` (lo que sigue, no es una línea con breakpoint)

**Qué hace:** el motor Razor combina el HTML de la plantilla con los datos del `PageModel` [24] y produce el HTML final que viaja al browser. Si la página embebe `<component>` (como la nuestra con `PanelBusqueda`), acá arranca la Fase 3.

**Analogía:** la cocina **emplata y el mozo sirve el plato** en la mesa.

---

## Fase 3 — Componentes Blazor (si agregaste los hooks)

Archivo: `src/Sucesiones.Web/Componentes/PanelBusqueda.razor`. Estos métodos son lifecycle hooks [32] — el framework los llama en momentos predefinidos; vos los sobreescribís con `override`.

**Actores en esta fase:**

| Actor del restaurante | Qué es en el programa |
|---|---|
| La pizarra que se actualiza sola en la mesa | El componente [31] Blazor (`PanelBusqueda.razor`) |
| El cocinero preparando la receta base | El hook [32] `OnInitialized` [36] |
| La foto del plato para el menú | El prerender [33] server-side (1ª ejecución de `OnInitialized`) |
| El plato real que se cocina y se come | El componente hidratado [34] en el circuit [12] (2ª ejecución) |
| El mozo agregando toppings al plato ya servido | El hook `OnAfterRender` [37] (DOM [35] ya pintado) |

### `protected override void OnInitialized()`

**Qué hace:** se llama una vez después de que el componente [31] recibe sus parámetros, antes de renderizar por primera vez.

**Cuándo se llama exactamente (el gotcha):** con `render-mode="ServerPrerendered"` [33] se dispara **DOS veces**:
1. **Durante el prerender server-side:** Blazor genera el HTML estático que se manda en la respuesta inicial (rápido, indexable por buscadores).
2. **Durante la hidratación** [34]: cuando el WebSocket [21] conecta y se construye el circuit [12], el componente "nace de nuevo" en ese contexto.

**Por eso, si en `OnInitialized` hacés una llamada cara** (consulta a base, HTTP externo), **la hacés dos veces**. Es el bug más famoso de Blazor Server con prerender.

**Hermanos del hook:**
- `OnInitializedAsync` — versión async, la más usada.
- `OnParametersSet` / `OnParametersSetAsync` — se llama cada vez que el componente padre te pasa parámetros nuevos.
- `OnAfterRender` / `OnAfterRenderAsync` — después de cada render (ver abajo).

**Patrón canónico para evitar la doble carga:**

```csharp
protected override async Task OnInitializedAsync()
{
    if (datosYaCargados) return;
    datos = await servicio.CargarAsync();
    datosYaCargados = true;
}
```

O hacer la carga en `OnAfterRenderAsync` con `primerRender == true`, que solo corre en la fase del circuit, no en el prerender.

**Analogía:** el cocinero prepara la receta base. Pero el restaurante saca **primero una foto del plato para el menú** (prerender) y **después cocina el plato real** que se come (circuit). La receta se prepara **dos veces**. Si la receta implica salir a comprar ingredientes (consultar la base), pagás el viaje dos veces.

### `protected override void OnAfterRender(bool primerRender)`

**Qué hace:** se llama **después** de que el DOM [35] ya se actualizó en el browser.

**Para qué sirve:**
- Llamar JS interop [14] (`IJSRuntime.InvokeVoidAsync(...)`) que necesita el DOM ya pintado.
- Poner foco en un input recién mostrado.
- Inicializar librerías JS de terceros (gráficos, mapas).
- Disparar carga de datos solo en la fase real del circuit (`primerRender == true`).

**El parámetro `primerRender`:**
- `true` solo la primera vez que el componente se renderiza después de hidratar.
- `false` en todos los renders subsecuentes (cambios de estado, parámetros nuevos).

**Diferencia clave con `OnInitialized`:**

| | `OnInitialized` [36] | `OnAfterRender` [37] |
|---|---|---|
| Cuándo se dispara | Antes del primer render | Después de cada render |
| Corre en prerender [33] | Sí | No (solo en el circuit [12]) |
| Se llama dos veces | Sí (prerender + circuit) | No |
| DOM [35] disponible | No | Sí |
| JS interop [14] disponible | No (tira excepción) | Sí |

**Por eso, para cargas async pesadas se prefiere `OnAfterRenderAsync(firstRender: true)`:** corre una sola vez, en el contexto del circuit, con el DOM listo.

**Analogía:** el plato **ya está servido en la mesa**. Recién ahora el mozo puede hacer lo que requiere el plato presente: rallar queso encima, prender el flambeado, acomodar la guarnición. No podía hacerlo mientras el plato estaba en la cocina.

---

## Mini-mapa mental

```
ARRANQUE (1 vez)              POR REQUEST (cada vez)         COMPONENTE BLAZOR
────────────────              ──────────────────────         ─────────────────
CreateBuilder                 Constructor (mozo asignado)    OnInitialized (x2 con prerender)
  ↓ planos en blanco            ↓                              ↓ receta base
Services.Add*                 OnGet (toma el pedido)         OnParametersSet
  ↓ anotar en planos            ↓                              ↓
Build (sella el DI)           Renderizar (sirve el plato)    Render
  ↓ obra terminada                                             ↓
Map* (carteles)                                              OnAfterRender (plato en mesa)
  ↓
Run (puertas abiertas)
```

---

## Glosario

Los números coinciden con las marcas `[n]` del texto de arriba.

**[1] Builder (armador)** — Objeto temporal para configurar algo antes de construirlo. `WebApplicationBuilder` junta configuración + servicios; `Build()` produce la app final. Patrón de diseño "builder".

**[2] Configuración (appsettings / Configuration)** — Valores que la app lee al arrancar (cadenas de conexión, URLs, flags). Vienen combinados de `appsettings.json`, variables de entorno y args. Se accede vía `creadorApp.Configuration`.

**[3] launchSettings.json** — Archivo en `Properties/` que define cómo se lanza la app en desarrollo: qué puertos, qué perfil, qué variables de entorno. Solo aplica a `dotnet run` / IDE, no se publica.

**[4] Variables de entorno** — Valores del sistema operativo que la app lee. La más importante acá es `ASPNETCORE_ENVIRONMENT` (`Development` / `Production`), que cambia el comportamiento (ej. mostrar errores detallados).

**[5] Args (argumentos de línea de comandos)** — Lo que pasás después de `dotnet run`, ej. `dotnet run --urls=http://localhost:9000`. Llegan al programa como el array `args`.

**[6] ILogger / Logging** — Servicio para escribir mensajes de diagnóstico (info, warning, error) que aparecen en la Debug Console. `ILogger<IndexModel>` es un logger "etiquetado" con el nombre de esa clase.

**[7] Contenedor DI (ServiceCollection → ServiceProvider)** — El objeto que fabrica y reparte dependencias. Antes de `Build()` se llena con `Add*` (es `ServiceCollection`); después de `Build()` se consulta (es `ServiceProvider`, sellado).

**[8] Inyección de dependencias (DI)** — Patrón donde una clase no crea sus dependencias (no hace `new Logger()`), sino que las pide en el constructor y el framework se las entrega armadas.

**[9] Servicio** — Cualquier objeto reutilizable que se registra en el contenedor DI para que otras clases lo pidan: un logger, un cliente HTTP, un repositorio de datos.

**[10] Convención Add* / Map*** — `Add*` (`AddRazorPages`, `AddServerSideBlazor`) registra servicios, va **antes** de `Build()`. `Map*` (`MapRazorPages`, `MapBlazorHub`) registra endpoints, va **después** de `Build()`.

**[11] SignalR** — Librería de .NET para comunicación en tiempo real (WebSocket con fallbacks). Blazor Server la usa para mantener el circuit entre browser y servidor.

**[12] Circuit (circuito)** — La conexión en vivo entre un browser y el servidor en Blazor Server. Cada pestaña tiene su circuit. Si se corta la red, el circuit se pierde y el componente se "congela".

**[13] Renderer** — La pieza de Blazor que mantiene el árbol de componentes, detecta qué cambió tras un evento y manda al browser solo el diff (no toda la página).

**[14] JS interop / IJSRuntime** — Mecanismo para llamar JavaScript desde C# (y viceversa) en Blazor. Solo está disponible una vez que el DOM existe (no en `OnInitialized`).

**[15] Build()** — Método que sella la configuración y los servicios y produce la `WebApplication`. Marca la frontera entre la fase de registro y la fase de ejecución.

**[16] Kestrel** — El servidor HTTP que viene con .NET. Es lo que realmente escucha en los puertos y recibe los requests. `aplicacion.Run()` lo arranca.

**[17] Endpoint** — Una ruta que la app sabe atender (`/`, `/Privacy`, `/_blazor`). Los `Map*` los registran.

**[18] Router** — La parte de la app que mira la URL entrante y decide qué endpoint la atiende. `UseRouting()` lo activa; los `Map*` lo llenan de rutas.

**[19] Middleware** — Cada eslabón del pipeline por el que pasa un request: `UseHttpsRedirection`, `UseRouting`, `UseAuthorization`. Cada uno puede inspeccionar, modificar o cortar el request.

**[20] Pipeline** — La cadena ordenada de middleware. El request entra por arriba, baja eslabón por eslabón hasta el handler, y la respuesta sube de vuelta. El orden importa.

**[21] WebSocket** — Conexión bidireccional persistente entre browser y servidor (a diferencia de HTTP, que es pedido-respuesta y se cierra). Blazor Server la usa para el circuit. Visible en DevTools → Network → filtro WS.

**[22] SIGTERM** — Señal del sistema operativo que pide a un proceso que termine ordenadamente. En un deploy real, el orquestador la manda para apagar la app; ahí `Run()` por fin retorna.

**[23] Razor Pages** — Modelo de UI de ASP.NET Core: una URL = un archivo `.cshtml` (vista) + su `.cshtml.cs` (code-behind con la lógica). Renderiza HTML en el servidor, sin interactividad en vivo por defecto.

**[24] PageModel** — La clase base del code-behind de una Razor Page. Contiene los handlers (`OnGet`, `OnPost`) y las propiedades que la vista lee con `@Model`.

**[25] Constructor** — Método especial que corre al crear una instancia de una clase. En Razor Pages es donde el framework inyecta las dependencias.

**[26] Scoped (lifetime por request)** — Tiempo de vida de un servicio: una instancia nueva por cada request HTTP, descartada al terminar. El `PageModel` es scoped.

**[27] Handler** — El método que maneja un tipo de request. En Razor Pages: `OnGet` para GET, `OnPost` para POST.

**[28] Named handler** — Permite varios handlers POST en una misma página, distinguidos por nombre: `OnPostBuscar`, `OnPostLimpiar`, invocados con `asp-page-handler="Buscar"` en el form.

**[29] OnGet / OnPost** — Los handlers por convención de Razor Pages. El nombre determina a qué método HTTP responden. Corren después del constructor, antes de renderizar.

**[30] Query string** — La parte de la URL después del `?`: en `/buscar?apellido=perez`, la query string es `apellido=perez`. Se lee con `Request.Query["apellido"]`.

**[31] Componente (.razor)** — Unidad reutilizable de UI de Blazor: HTML + un bloque `@code` con C#. Se renderiza en el servidor y, con Blazor Server, queda interactivo vía el circuit.

**[32] Lifecycle hook (gancho de ciclo de vida)** — Método que el framework llama en momentos predefinidos de la vida de un componente (`OnInitialized`, `OnParametersSet`, `OnAfterRender`). Se enchufa lógica sobreescribiéndolo.

**[33] Prerender / ServerPrerendered** — Estrategia donde el componente se renderiza primero como HTML estático en el servidor (rápido, indexable) y después se hidrata. Causa que `OnInitialized` corra dos veces.

**[34] Hidratación (hydration)** — El momento en que el HTML estático prerenderizado "cobra vida": el script de Blazor conecta por WebSocket y engancha los event handlers al DOM ya pintado. Antes de hidratar, los botones no hacen nada.

**[35] DOM** — Document Object Model: la representación en memoria del HTML que el browser muestra y manipula. "El DOM ya está listo" significa que el HTML ya se pintó en pantalla.

**[36] ASP.NET Core** — El framework de Microsoft para aplicaciones web con .NET. Razor Pages y Blazor Server son dos formas de construir UI dentro de él.

**[37] OnAfterRender** — Lifecycle hook que corre después de cada render, con el DOM ya pintado. Único lugar seguro para JS interop. Su parámetro `primerRender` distingue el primer render de los siguientes.
