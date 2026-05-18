# Migración del rediseño — Propuesta B · Sentencia moderna

Carpeta con los archivos finales para reemplazar en el proyecto. La estructura
espeja `src/Sucesiones.Web/` para que sea un drag-and-drop directo.

> **Una sola rebanada grande** (10 archivos tocados — dentro del límite de
> 10-15 que pide tu `CLAUDE.md`). Find-and-replace visual; deja la lógica
> de búsqueda, sesión SCBA, selección y visor intactos.

## Por qué B (vs A)

B comparte el mismo ADN serif institucional que A pero apunta a **utilidad
sobre forma**:

- **Inputs en caja con borde tenue** — hit-target más cómodo, focus ring
  visible, autocompletar del browser funciona mejor.
- **Tarjetas blancas** delimitan formulario y resultados sobre el papel
  suave del fondo, ayudando a leer la jerarquía.
- **Tabla con header gris cálido + zebra suave** — fila por fila más fácil
  de seguir en consultas largas; "Ver oficio" es un botón outline con icono
  ojo (no link tipográfico abstracto).
- **Visor con sidebar de thumbnails** — saltás directo a la página que te
  interesa cuando el oficio tiene varias páginas.
- **Paginación al pie** de la tabla — UX clásica de tabla, no inventa nada.

Sigue siendo formal e institucional (no es startup-y): los radios son chicos
(2-4px), no hay gradientes ni emojis, la tipografía sigue siendo Source
Serif 4 + Cormorant Garamond.

## Archivos modificados

```
src/Sucesiones.Web/
├── wwwroot/
│   ├── css/entrada.css           ← tokens institucionales + capa @components (B)
│   └── img/header-rju.jpg        ← copiar el JPEG vos (ver paso 2)
├── Pages/
│   ├── Shared/_Layout.cshtml     ← header oficial, meta-bar, footer
│   ├── Index.cshtml              ← h1 y lead en serif romana
│   └── Privacy.cshtml            ← restyleado pasivamente
├── Componentes/
│   └── PanelBusqueda.razor       ← rediseño completo (lógica intacta)
└── Modelos/
    └── CamposSucesiones.cs       ← orden + etiquetas + grupo (principal/variantes)
```

## Pasos

### 1. Copiar los archivos

Desde la raíz del repo:

```powershell
git add -A; git commit -m "antes del rediseño"
Copy-Item -Recurse -Force migracion/src/* src/
```

### 2. Copiar el header oficial

El JPEG del Poder Judicial va a:

```
src/Sucesiones.Web/wwwroot/img/header-rju.jpg
```

El layout ya lo referencia con `<img src="~/img/header-rju.jpg">`.

### 3. Rebuild y correr

```powershell
dotnet build
dotnet run --project src/Sucesiones.Web
```

## Qué cambia

### `entrada.css`

- Nuevo bloque `@theme` con la paleta institucional:
  - **Papel:** `bg-papel`, `bg-papel-suave`, `bg-papel-tarjeta`, `bg-papel-hondo`
  - **Tinta:** `text-tinta`, `text-tinta-suave`, `text-tinta-tenue`, `text-tinta-fina`
  - **Reglas:** `border-regla`, `border-regla-suave`, `border-regla-fina`
  - **Accent:** `bg-marca-azul` = `#1e3a5f` (azul pizarra)
  - **Auxiliares:** `bg-azul-tinte` (focus ring), `text-vino` (errores)
- Las clases `marca-*` viejas siguen funcionando — solo cambian de hex.
  No hace falta tocar markup que use `bg-marca-azul`, `text-marca-negro`.
- Fuentes desde Google Fonts (Source Serif 4 + Cormorant Garamond +
  Source Sans 3). Si en algún momento querés self-hosted, reemplazá el
  `@import url(…)` por `@font-face` apuntando a `wwwroot/fonts/`.
- Capa `@layer components` con las clases custom del Razor:
  `.card-rju` `.input-caja` `.label-caja` `.btn-primario` `.btn-ghost`
  `.link-ver` `.chip` `.page-btn` `.tabla-rju` `.eyebrow` `.spinner-rju`
  `.num-tab`.

### `_Layout.cshtml`

- Header reemplazado: franja slate de 5px + `<img>` del JPEG oficial
  centrado en `max-w-[1280px]` + hairline + meta-bar (breadcrumb tipográfico
  + fecha en versalitas con cultura `es-AR`).
- Body: `bg-papel-suave` (en propuesta B el fondo es el más claro de la
  paleta para que las tarjetas blancas tengan contraste).
- Footer institucional con tres columnas (marca, dirección, links).
- Sacado el header propio anterior con el "puntito azul" — la imagen
  oficial ya cumple esa función.

### `Index.cshtml`

- H1 en display family con `<em>` tipográfico en "Juicios Universales".
- Lead en italic serif. Eyebrow accent sobre el h1.

### `PanelBusqueda.razor` (la pieza grande)

Lógica del `@code` intacta — solo cambia presentación.

**Formulario:**
- Tarjeta blanca (`.card-rju`) con header propio (eyebrow + h2 + hint italic
  a la derecha) separado por hairline.
- Dos secciones: **Principales** (4 cols, span 1/2) y **Variantes**
  (eyebrow inline + texto explicativo + grid de 2 cols), separadas por
  border-top.
- Inputs `.input-caja` con borde tenue, focus ring azul-tinte.
- Botones `.btn-primario` (Buscar — con icono lupa) + `.btn-ghost`
  (Limpiar — con icono reset). Footer de acciones separado por hairline.

**Resultados:**
- Tarjeta blanca con header (eyebrow + h2 con conteo + chips de orden y
  export a la derecha).
- Tabla `.tabla-rju` con `thead` de fondo `papel-hondo` y zebra suave
  (filas pares `papel-tarjeta`).
- Columnas: # · Año · Oficio · Apellido y nombre · Documento (right) ·
  Fallecimiento (right) · acciones.
- "Ver oficio" como botón outline `.link-ver` con icono ojo. Hover
  invierte: fondo `marca-azul`, texto `papel-tarjeta`.
- Pie con paginación (`Mostrando 1–N de N` + `< 1 >`).
- Estados Cargando / Sin resultados / Error: centrados dentro de la
  tarjeta con icono circular (azul para neutros, vino para errores).

**Visor modal:**
- Tarjeta blanca con grid de dos columnas: sidebar de thumbnails (300px) +
  canvas principal.
- Header de ancho completo: eyebrow + nombre del causante en display +
  metadatos en línea (Oficio, Fallecimiento, DNI). Botón **Descargar PDF**
  primario + cerrar (×) ghost.
- Sidebar: lista vertical de thumbnails con mini-doc dibujado, página
  activa con borde azul. Click cambia `paginaActiva` (no hace scroll
  todavía — punto de iteración).
- Canvas: todas las páginas apiladas con su `<figcaption>`. Si más
  adelante querés "saltar a la página activa", agregar `scroll-margin-top`
  + `scrollIntoView` por id.

### `CamposSucesiones.cs`

- Nuevo enum `GrupoCampo { Principal, Variantes }`.
- Record `CampoConsulta` ahora lleva `Grupo` y `Span` (1 o 2 columnas).
- Orden nuevo: **Apellido · Nombre · Año · Nº de oficio · D.N.I. ·
  Fallecimiento · Variante apellido · Variante nombre**. Los más buscados
  arriba, variantes al fondo agrupadas.
- Etiquetas más cortas. Los `Nombre` (identificadores del contrato SCBA:
  `RJUapellido`, etc.) **NO cambian**.

## Riesgos / cosas para confirmar en QA

1. **`<text>` tag en Razor del header del modal** — uso `<text>` para
   emitir texto plano dentro de un `if` (sintaxis Razor estándar). Si el
   compilador se queja, lo reemplazo por `@:` o por `@(...)`.
2. **Filas con `:nth-child(even)` en Tailwind** — la zebra está en el CSS
   de la clase `.tabla-rju`, no como utility. Funciona sin safelist.
3. **`paginaActiva` del sidebar** — hoy solo cambia el highlight visual;
   las páginas se muestran apiladas. Si querés "click → ir a esa página",
   agregar `id="oficio-pagina-{i}"` y `scrollIntoView`.
4. **Hot Reload** — el cambio de `GrupoCampo` en `CamposSucesiones.cs` es
   un *rude edit*; la primera vez `dotnet watch` va a reiniciar la app.
5. **Anchos de la tabla** — propuesta B no usa `table-layout: fixed`. Si
   con apellidos muy largos algún oficio empuja, podemos volver a fixed
   con `<colgroup>` (está en la versión A si necesitás referencia).
6. **Print del PDF** — no se tocó nada de `OficioPdf.cshtml`.

## Si algo no compila

- Confirmá que `_Imports.razor` exporta `Sucesiones.Web.Modelos` (debería
  ya hacerlo). Si no, agregá `@using Sucesiones.Web.Modelos`.
- Si Tailwind no genera `col-span-1` / `col-span-2`: están como strings
  literales en los ternarios del `.razor` — el `@source` los detecta.

## Tras la migración — posibles próximas rebanadas

- **Slice 6 con el nuevo estilo:** "Demasiados resultados" como banner
  sobre la tabla (el diseño está en el mockup, `Rediseño RJU.html`,
  artboard "B · Demasiados resultados").
- **Sidebar interactivo:** click en thumb hace scroll a la página
  correspondiente en el canvas.
- **Self-host de fuentes:** bajar `.woff2` a `wwwroot/fonts/` y reemplazar
  `@import url(...)` por `@font-face`.
- **Imprimir listado:** chip "Exportar CSV" hoy es placeholder; armar la
  generación CSV en el server.
- **Accesibilidad:** revisar contrastes (azul pizarra `#1e3a5f` sobre
  blanco da 11.2:1 — sobrado), focus rings, navegación por teclado en el
  modal (Esc para cerrar, ←/→ para cambiar página activa).
