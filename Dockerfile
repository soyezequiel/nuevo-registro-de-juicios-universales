# ---- Etapa de build ----
# La imagen del SDK trae todo para compilar .NET 9. Se descarta al final:
# al contenedor que corre en Render solo viaja la etapa "final".
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# El repo versiona tools/tailwindcss.exe (binario Windows) y ademas tools/ esta
# gitignoreado. Nada de eso sirve en Linux, asi que bajamos el standalone Linux.
# Es el mismo release "latest" que usa tools/README.md: el CSS sale identico al local.
ADD https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64 /usr/local/bin/tailwindcss
RUN chmod +x /usr/local/bin/tailwindcss

# Truco de cache de capas: si solo cambia codigo (no los .csproj), Docker reusa
# la capa del restore y el deploy es mucho mas rapido. Por eso copiamos primero
# solo los .csproj. El de tests no se copia: no hace falta para publicar la web.
COPY src/Sucesiones.Web/Sucesiones.Web.csproj src/Sucesiones.Web/
COPY src/Sucesiones.Adapter/Sucesiones.Adapter.csproj src/Sucesiones.Adapter/
RUN dotnet restore src/Sucesiones.Web/Sucesiones.Web.csproj

# Ahora si el resto del codigo y el publish.
# -p:TailwindBinario=... pisa la propiedad del .csproj para que el target
# TailwindBuild ejecute el binario Linux en vez del .exe de Windows. Asi no
# tocamos el build existente: la misma logica, otro binario.
COPY . .
RUN dotnet publish src/Sucesiones.Web/Sucesiones.Web.csproj \
    -c Release -o /app/publish \
    -p:TailwindBinario=/usr/local/bin/tailwindcss

# ---- Etapa final (lo que realmente corre en Render) ----
# Imagen de runtime ASP.NET: mas chica, sin SDK ni compiladores.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Render asigna el puerto en runtime via la variable PORT (no en build). Kestrel
# tiene que escuchar en 0.0.0.0 y en ese puerto. Se usa shell form ("sh -c ...")
# justamente para que ${PORT} se expanda al arrancar el contenedor; en exec form
# la variable quedaria literal. El :-8080 es el fallback para correrlo local.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet Sucesiones.Web.dll"]
