# syntax=docker/dockerfile:1

# Backend del estacionamiento: la API web. La consola no entra en la imagen.
#
# Compilación en dos etapas: el SDK (~800 MB) se queda en la etapa de compilación y la imagen
# final sólo lleva el runtime de ASP.NET más los binarios publicados.

ARG VERSION_DOTNET=8.0

# ---------------------------------------------------------------------------------------------
# Etapa 1: compilar y publicar
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${VERSION_DOTNET} AS compilacion

WORKDIR /origen

# Primero sólo los .csproj. Mientras no cambien las dependencias, Docker reutiliza la capa del
# restore: editar código no vuelve a bajar NuGet. Se copian uno a uno, y no con un comodín,
# porque un comodín arrastraría también la consola y las pruebas, que no forman parte de la API.
COPY src/Estacionamiento.Dominio/Estacionamiento.Dominio.csproj                  src/Estacionamiento.Dominio/
COPY src/Estacionamiento.Aplicacion/Estacionamiento.Aplicacion.csproj            src/Estacionamiento.Aplicacion/
COPY src/Estacionamiento.Infraestructura/Estacionamiento.Infraestructura.csproj  src/Estacionamiento.Infraestructura/
COPY src/Estacionamiento.Web/Estacionamiento.Web.csproj                          src/Estacionamiento.Web/

RUN dotnet restore src/Estacionamiento.Web/Estacionamiento.Web.csproj

COPY src/Estacionamiento.Dominio/         src/Estacionamiento.Dominio/
COPY src/Estacionamiento.Aplicacion/      src/Estacionamiento.Aplicacion/
COPY src/Estacionamiento.Infraestructura/ src/Estacionamiento.Infraestructura/
COPY src/Estacionamiento.Web/             src/Estacionamiento.Web/

RUN dotnet publish src/Estacionamiento.Web/Estacionamiento.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /aplicacion

# ---------------------------------------------------------------------------------------------
# Etapa 2: imagen final
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${VERSION_DOTNET} AS final

# tzdata: sin él el contenedor creería vivir en UTC y las horas de entrada y salida —que es lo
# que se factura— saldrían desplazadas seis horas.
# curl: lo usa el healthcheck de más abajo; la imagen de runtime no trae ningún cliente HTTP.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=America/Mexico_City \
    ASPNETCORE_HTTP_PORTS=5209 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /aplicacion

COPY --from=compilacion /aplicacion .

# Punto de montaje de los informes. Se crea con dueño antes de bajar de privilegios para que la
# API pueda escribir en él cuando se usa sin montar nada desde el anfitrión.
RUN mkdir -p /informes && chown $APP_UID:$APP_UID /informes

# Sin privilegios: si alguien encuentra un agujero en la API, no lo encuentra siendo root.
USER $APP_UID

EXPOSE 5209

# Comprueba que el proceso atiende peticiones. No consulta la base de datos a propósito: si la
# base cae con la API en pie, marcar la API como enferma haría que el orquestador la reiniciara
# en bucle sin arreglar nada.
HEALTHCHECK --interval=15s --timeout=3s --start-period=40s --retries=3 \
    CMD curl --fail --silent --show-error http://localhost:5209/salud || exit 1

ENTRYPOINT ["dotnet", "Estacionamiento.Web.dll"]
