# Sucesiones — Registro de Juicios Universales (rediseño)

Interfaz pública en construcción que consulta el sistema oficial de SCBA a través de un backend adaptador. Razor Pages como shell + isla Blazor Server para la búsqueda. Estilos con Tailwind CLI standalone (sin Node).

Para el roadmap y las decisiones de diseño, ver `PLAN.md`. Para las reglas de trabajo, `CLAUDE.md`.

## Requisitos

- **.NET 9 SDK** (probado con `9.0.313`). Verificar con `dotnet --version`.
- **`tools/tailwindcss.exe`** — binario standalone de Tailwind. No se versiona en git y el build falla sin él. Cómo bajarlo: ver `tools/README.md`.

## Iniciar el proyecto

Desde la raíz del repo:

```powershell
# 1. Bajar el binario de Tailwind (solo la primera vez)
$url = "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-windows-x64.exe"
Invoke-WebRequest -Uri $url -OutFile .\tools\tailwindcss.exe

# 2. Compilar (Tailwind se compila solo en este paso vía MSBuild)
dotnet build

# 3. Levantar la app
dotnet run --project src/Sucesiones.Web
```

`dotnet run` imprime la URL en la consola (por ej. `https://localhost:xxxx`). Abrirla en el navegador.

## Desarrollo

Durante desarrollo activo conviene correr Tailwind en `--watch` en otra terminal para no esperar al rebuild de MSBuild:

```powershell
.\tools\tailwindcss.exe -i src\Sucesiones.Web\wwwroot\css\entrada.css -o src\Sucesiones.Web\wwwroot\css\salida.css --watch
```

En VS Code, `F5` levanta el debugger sin configuración extra (`.vscode/launch.json` está versionado).

## Estructura

```
src/Sucesiones.Web/        App Razor Pages + isla Blazor Server
  Componentes/             Componentes Blazor (.razor)
  Modelos/                 DTOs y catálogos
  Servicios/               Lógica de búsqueda (fake por ahora)
tools/                     Binarios externos (Tailwind) — no versionados
PLAN.md                    Roadmap de rebanadas y decisiones
CLAUDE.md                  Reglas operativas del proyecto
```
