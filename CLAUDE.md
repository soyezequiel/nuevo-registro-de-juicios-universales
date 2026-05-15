# CLAUDE.md — reglas operativas del proyecto Sucesiones

Este archivo se carga automáticamente al inicio de cada sesión. Define cómo trabajar en este repo. Las reglas no son negociables salvo que el usuario las cambie en la conversación.

## Documentos de referencia (orden de prioridad)

1. `PLAN.md` — roadmap de rebanadas, decisiones tomadas, stack confirmado.
2. `METODOLOGIA_GENERICA.md` — plantilla de reglas (fuente original de esta metodología).
3. `handoff-backend-scba-rju-para-front.md` — contrato observado del backend SCBA, para slice 3+.

Si hay contradicción entre un documento y lo que el usuario pide en la conversación, **preguntar antes de actuar**.

## Principio general: rebanadas verticales

Una rebanada vertical es una funcionalidad end-to-end mínima que el usuario puede probar y confirmar por sí solo.

- Máximo **~10–15 archivos por iteración**. Si se pasa, cortar antes de empezar.
- Nunca dos rebanadas en una sola iteración "porque están relacionadas".

## Ciclo de 5 pasos (obligatorio)

1. **Plan corto en chat**: qué se hace, qué archivos se tocan, decisiones no obvias, dudas. **Esperar OK del usuario antes de codear.**
2. **Código** dentro del límite de archivos.
3. **Walkthrough**: lista de cambios, por dónde leer, cómo probar, qué quedó pendiente.
4. **Esperar prueba y confirmación del usuario.** No avanzar a la siguiente rebanada sin OK.
5. **Commit chico** recién después de la confirmación.

## Reglas duras

- No introducir abstracciones no pedidas. Tres líneas similares es mejor que una abstracción prematura.
- No agregar dependencias sin justificar y obtener OK.
- No refactorear código existente salvo que se pida explícitamente.
- No "limpiar" código del usuario sin permiso.
- Sin comentarios redundantes. Solo el "porqué" no obvio.
- **Comentarios de aprendizaje**: en este proyecto los archivos con contenido técnico llevan tags al inicio o al lado de cada bloque, para que el usuario sepa qué se espera que sepa de memoria y qué no. Aplicar a todo código nuevo.
  - `[BASE]` — se sabe de aprender el stack (Razor, C#, .NET, Tailwind, Blazor). Si leíste el tutorial oficial, lo conocés.
  - `[BUSCAR]` — receta específica que no se recuerda de memoria. Se googlea "cómo hago X en Y", se encuentra el snippet, se adapta.
  - `[TEMPLATE]` — lo generó un comando (`dotnet new ...`, `dotnet add ...`). Lo podés leer y entender, pero no lo escribiste.
  - `[ESTILO]` — decisión opcional de organización. El código funcionaría igual sin esto. Es prolijidad.
- Sin error handling defensivo para escenarios imposibles. Validar en bordes (input de usuario, APIs externas).
- Nada de half-finished: si algo no se termina, decirlo en el walkthrough.

## Honestidad técnica

- No prometer precisión o garantías que el sistema no puede dar.
- No esconder errores con fallbacks silenciosos. Mejor error claro.
- Marcar incertidumbre en código o walkthrough.
- Verificar lo que se reporta. Si no se probó, decirlo: "compila pero no testeé el flujo en el browser".

## Cuándo PARAR y preguntar

- Cualquier decisión no contemplada en el plan acordado.
- Antes de instalar una dependencia nueva.
- Antes de cualquier operación destructiva (`git reset --hard`, `push --force`, borrar archivos).
- Antes de `git commit` (lo aprueba el usuario por rebanada).
- Cuando la implementación choca con algo del plan.
- Cuando una rebanada se está pasando del tamaño previsto.

## Convenciones de nombres

### Lo nuestro va en español

- Archivos custom: `Componentes/PanelBusqueda.razor`, `Modelos/ResultadoSucesion.cs`.
- Clases, propiedades, métodos, variables: `Busqueda`, `RealizarBusqueda()`, `apellidoBuscado`.
- Carpetas custom: `Componentes/`, `Modelos/`, `Servicios/`.
- IDs y clases CSS propias: `panel-busqueda`, `tabla-resultados`.

### Lo del framework o contrato externo queda como está

- Palabras reservadas C# / Razor: `class`, `public`, `@page`, `@model`.
- Métodos sobrescritos del framework: `OnGet`, `OnPost`, `Main`, `InvokeAsync`.
- Archivos del template `dotnet new razor`: `Program.cs`, `Pages/Index.cshtml`, `_Layout.cshtml`, `appsettings.json`.
- Utilitarias Tailwind: `flex`, `bg-blue-900`, `rounded-lg`.
- Identificadores del contrato SCBA: `RJUapellido`, `__VIEWSTATE`, `Select$N`.

### Capitalización C#

- Clases, métodos, propiedades públicas: `PascalCase`.
- Variables locales y parámetros: `camelCase`.

## Stack confirmado

| Capa | Tecnología |
|---|---|
| Runtime | .NET 9 |
| Web | Razor Pages (shell) |
| Interactividad | Blazor Server (isla embebida con `<component>`) |
| Estilos | Tailwind CSS — CLI standalone (sin Node) |
| Scripts cliente | JS vanilla solo si es estrictamente necesario |
| Backend adapter | C# class library separado (slice 3+) |
| HTTP a SCBA | `HttpClient` + `HtmlAgilityPack` |
| Tests | xUnit (se decide formalmente en slice 3) |
| Base de datos | No hay |
| React / Material UI / Bootstrap | Fuera de scope |

**Paleta visual:** azul + negro, moderno, limpio, responsive.

## Comandos del proyecto

```powershell
# Build y run (desde la raíz del repo)
dotnet build
dotnet run --project src/Sucesiones.Web

# Tailwind: se ejecuta automáticamente en el build vía MSBuild target.
# El binario debe estar en tools/tailwindcss.exe — ver tools/README.md.

# Tailwind manual (watch durante desarrollo)
.\tools\tailwindcss.exe -i src\Sucesiones.Web\wwwroot\css\entrada.css -o src\Sucesiones.Web\wwwroot\css\salida.css --watch
```
