# Herramientas externas — `tools/`

Esta carpeta contiene binarios standalone que el build usa pero que **no se versionan en git** (ver `.gitignore`).

## `tailwindcss.exe` (requerido)

Compilador de Tailwind CSS standalone. No requiere Node.js. Se ejecuta automáticamente en cada `dotnet build` vía un MSBuild target definido en `src/Sucesiones.Web/Sucesiones.Web.csproj`.

### Cómo bajarlo

1. Ir a la página de releases: <https://github.com/tailwindlabs/tailwindcss/releases/latest>
2. Bajar el asset `tailwindcss-windows-x64.exe`.
3. Renombrarlo a `tailwindcss.exe` y guardarlo en esta carpeta (`tools/tailwindcss.exe`).

O en PowerShell desde la raíz del repo:

```powershell
$url = "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-windows-x64.exe"
Invoke-WebRequest -Uri $url -OutFile .\tools\tailwindcss.exe
```

### Verificar

```powershell
.\tools\tailwindcss.exe --help
```

Si el binario no está, `dotnet build` va a fallar con un mensaje claro pidiendo seguir estos pasos.

## Modo desarrollo (watch)

Durante desarrollo activo conviene correr Tailwind en `--watch` en otra terminal para no esperar al rebuild de MSBuild:

```powershell
.\tools\tailwindcss.exe -i src\Sucesiones.Web\wwwroot\css\entrada.css -o src\Sucesiones.Web\wwwroot\css\salida.css --watch
```
