# syntax=docker/dockerfile:1

# Front del estacionamiento: la aplicación de Angular, ya compilada, servida por nginx.
#
# Compilación en dos etapas. Node y las ~630 dependencias de desarrollo (unos 500 MB) se quedan
# en la primera etapa; la imagen final sólo lleva nginx y los archivos estáticos del bundle.
#
# Además de servir, nginx reenvía /api y /salud al backend. No es un adorno: la API no tiene
# CORS configurada, así que el front y la API tienen que compartir origen o el navegador
# bloquea todas las peticiones. Es el mismo papel que cumple proxy.conf.json en desarrollo.

ARG VERSION_NODE=22-alpine
ARG VERSION_NGINX=1.27-alpine

# ---------------------------------------------------------------------------------------------
# Etapa 1: compilar el bundle
# ---------------------------------------------------------------------------------------------
FROM node:${VERSION_NODE} AS compilacion

WORKDIR /origen

# Primero sólo el manifiesto y el candado. Mientras no cambien las dependencias, Docker reutiliza
# la capa de npm ci: editar un componente no vuelve a bajar medio registro de npm.
COPY package.json package-lock.json ./

# `npm ci` y no `npm install`: instala exactamente lo que fija package-lock.json y falla si el
# candado y el manifiesto no concuerdan. Una compilación reproducible no puede depender de que
# npm resuelva un rango de versiones el día que se construya la imagen.
RUN npm ci

COPY angular.json tsconfig.json tsconfig.app.json ./
COPY public/ public/
COPY src/ src/

# Angular descarga e incrusta las fuentes de Google en tiempo de compilación (optimization.fonts
# viene activada por omisión), de modo que la imagen final no depende de fonts.googleapis.com.
RUN npm run build

# ---------------------------------------------------------------------------------------------
# Etapa 2: imagen final
# ---------------------------------------------------------------------------------------------
# La variante «unprivileged» de nginx en lugar de la oficial: la oficial arranca su proceso
# maestro como root, y aquí no hace falta. Ésta corre entera como el usuario nginx (uid 101) y
# trae los permisos ya resueltos para escribir la configuración generada y los temporales.
FROM nginxinc/nginx-unprivileged:${VERSION_NGINX} AS final

# Origen del backend, visto desde dentro del contenedor. "web" es el nombre del servicio de la
# API en la red de Compose del repositorio hermano. Se pisa con una variable de entorno para
# apuntar a otro sitio sin reconstruir la imagen.
ENV API_ORIGEN=http://web:5209

# Acota envsubst a las variables que empiezan por API_. Sin este filtro sustituiría cualquier
# variable de entorno definida, y las plantillas de nginx usan su propia sintaxis con $.
ENV NGINX_ENVSUBST_FILTER=^API_

# El punto de entrada de la imagen procesa todo lo que haya aquí y escribe el resultado en
# /etc/nginx/conf.d, pisando la configuración de ejemplo que trae nginx.
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template

# Sólo el contenido de browser/: el builder de aplicación de Angular deja ahí los archivos que
# van al navegador. Copiar dist/ entero metería también carpetas que no se sirven.
COPY --from=compilacion /origen/dist/estacionamiento-front/browser/ /usr/share/nginx/html/

EXPOSE 8080

# Comprueba que nginx atiende, no que el backend esté vivo. Si la API cae con el front en pie,
# marcar el front como enfermo haría que el orquestador lo reiniciara sin arreglar nada; la
# aplicación ya avisa por su cuenta con la sonda de contacto de la cabecera.
#
# 127.0.0.1 y no localhost: dentro del contenedor localhost resuelve primero a ::1, y nginx
# escucha sólo en IPv4 (0.0.0.0:8080). Con el nombre, la sonda recibe «connection refused» y
# marca enfermo un contenedor que atiende perfectamente.
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --quiet --spider --tries=1 http://127.0.0.1:8080/index.html || exit 1
