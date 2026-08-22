# Padrón — front del estacionamiento

Interfaz web para el backend [`Estacionamiento`](../Estacionamiento), una API JSON de ASP.NET Core
que administra el acceso de vehículos a un estacionamiento de pago. La API resuelve las tarifas;
esta aplicación pone la pantalla donde se cobran.

Sin ella, el estacionamiento se opera desde Swagger o con `curl`. El objetivo es que un empleado
sin conocimiento de HTTP registre accesos y cierre el mes sin equivocarse.

## Las dos zonas

| Zona | Ruta | Para quién |
|---|---|---|
| **Operación** | `/caseta` | El operador en la pluma. Teclea la placa, registra entrada o salida, lee el importe. |
| **Administración** | `/panel` | Estado del estacionamiento de un vistazo. |
| | `/padron` | Padrón completo con filtros, altas, y ficha desplegable en el sitio. |
| | `/padron/:placa` | Ficha del vehículo con todo su historial de estancias. |
| | `/cierre` | Informe de pagos de residentes y el cierre de mes. |

La raíz redirige a `/caseta`: es la pantalla donde alguien pasa el turno entero. El panel se
visita, no se habita.

## Arrancar

Hace falta el backend levantado. Desde la carpeta hermana:

```bash
cd ../Estacionamiento
docker compose up -d --build          # API en http://localhost:5209
```

Para explorar con datos realistas —100 vehículos y estancias repartidas en 28 días— sembrá la
base al levantar:

```bash
SEMBRAR=100 docker compose up -d --build
```

```powershell
$env:SEMBRAR=100; docker compose up -d --build     # PowerShell
```

Después, el front:

```bash
npm install
npm start                              # http://localhost:4200
```

La cabecera lleva una sonda de contacto con la API. Si el backend no está, lo dice una vez y
arriba, en lugar de que cada pantalla enseñe su propio error.

## Docker

El backend tiene que estar levantado primero: este Compose se engancha a la red que crea el
suyo en lugar de definir una propia.

```bash
cd ../Estacionamiento && SEMBRAR=100 docker compose up -d --build
cd ../Estacionamiento-Front && docker compose up -d --build
```

El front queda en **http://localhost:4200**, la misma dirección que con `npm start`, para que no
cambie según cómo se ejecute.

```bash
docker compose logs -f front     # ver el arranque
docker compose ps                # estado y healthcheck
docker compose down              # parar
```

> No se puede tener `npm start` y el contenedor a la vez: los dos quieren el 4200. Y si el
> servidor de desarrollo queda huérfano, se lleva el puerto por IPv6 (`[::1]`) mientras Docker
> se queda con el IPv4 — `localhost` resuelve primero a IPv6 y termina respondiendo el que uno
> creía haber apagado. Con `PUERTO_FRONT=4300 docker compose up -d` conviven sin pelearse.

| Detalle | Cómo queda |
|---|---|
| Imagen | Compilación en dos etapas; en la final sólo nginx y el bundle (74 MB) |
| Usuario | No root (`nginx`, uid 101), con la imagen `nginx-unprivileged` |
| API | nginx reenvía `/api` y `/salud` a `http://web:5209` por la red de Compose |
| Red | `estacionamiento_default`, declarada como externa: `down` acá no la borra |
| Puerto | 4200 en el anfitrión; 8080 dentro, porque sin root no se puede escuchar por debajo de 1024 |
| Caché | `index.html` sin cachear; los archivos con hash, un año e `immutable` |
| Compresión | gzip sobre HTML, JS, CSS y JSON — el bundle baja de 94 kB a 31 kB |
| Fuentes | Angular las incrusta al compilar: la imagen no depende de fonts.googleapis.com |

Se puede pisar sin editar el `compose.yaml`, con variables de entorno o un `.env`:
`PUERTO_FRONT`, `API_ORIGEN` y `RED_API`.

## Por qué todo va por un proxy

`Program.cs` del backend no configura CORS, así que el navegador bloquea cualquier llamada a
`http://localhost:5209` hecha desde otro origen. La salida es no tener otro origen: la
aplicación pide siempre a rutas relativas —`/api/panel`, nunca la URL completa— y quien esté
sirviéndola las reenvía.

| Cómo se ejecuta | Quién reenvía |
|---|---|
| `npm start` | [proxy.conf.json](proxy.conf.json) → `http://localhost:5209` |
| Contenedor | [nginx](nginx/default.conf.template) → `$API_ORIGEN`, por omisión `http://web:5209` |

Alternativa descartada: agregar CORS al backend. Es otro repositorio, y tocarlo por una
necesidad de despliegue del front hubiera puesto una cabecera permisiva en producción para
resolver algo que el proxy ya resuelve sin abrir nada.

**No pongas una URL absoluta en un servicio**: rompe las dos filas de la tabla a la vez. Para
apuntar a otro backend se cambia el origen, sin recompilar la imagen:

```bash
API_ORIGEN=http://192.168.1.50:5209 docker compose up -d
```

## Cómo está armado

```
src/
  estilos/
    _fichas.scss        La única fuente de valores visuales. Ningún componente escribe un
                        color, un espacio o un tamaño a mano.
    _base.scss          Reajuste y superficies del navegador: selección, cursor, barra de
                        desplazamiento, anillo de foco, cifras tabulares.
    _piezas.scss        Vocabulario compartido de controles: botones, campos, hojas, registros.
  app/
    nucleo/             Contratos de la API, servicios HTTP, traducción de errores, reloj,
                        sonda de salud, formato es-MX.
    comun/              Placa, sello de clase, escala de permanencia, aviso, iconos.
    paginas/            Una carpeta por pantalla, cargada de forma diferida.
```

Angular 20 con componentes autónomos, señales y `OnPush` en todos lados. Cada pantalla es un
fragmento aparte: la caseta no carga el código del cierre de mes.

### Reglas que el front no implementa

Las tarifas, los minutos facturables y los redondeos los calcula el dominio del backend. Acá se
leen tal como llegan.

| Tipo | Tarifa | Cuándo paga |
|---|---|---|
| Oficial | — | Nunca |
| Residente | MXN$0.05 / min | A fin de mes |
| No residente | MXN$0.5 / min | Al salir |

Duplicar cualquiera de esas reglas en TypeScript sería crear una segunda verdad que con el
tiempo se desvía de la primera. [`nucleo/modelos.ts`](src/app/nucleo/modelos.ts) es el espejo
exacto de [`Contratos/`](../Estacionamiento/src/Estacionamiento.Web/Contratos/) del backend, y
es el único sitio que hay que tocar si ese contrato se mueve.

### Tres decisiones que conviene conocer

**La caseta propone la acción.** El registro de la derecha ya sabe quién está dentro, así que si
la placa tecleada está dentro, la única acción que cabe es una salida: se marca ese botón y la
tecla Intro lo dispara. El otro sigue a un clic, porque la deducción puede fallar y la decisión
es de quien está en la pluma.

**Un 409 no ofrece reintentar.** «Ya está dentro», «no está dentro» y «ya estaba dada de alta»
son estados del estacionamiento, no fallos transitorios: repetir la petición da lo mismo. Sólo
los 5xx muestran el botón de reintentar. Está en
[`nucleo/problema.ts`](src/app/nucleo/problema.ts).

**El cierre de mes se defiende.** Enumera lo que va a destruir con las cifras reales del informe
en pantalla, y el botón no se habilita hasta que el informe está a salvo —descargarlo o
guardarlo marca la casilla solo— y hasta que se teclea `COMENZAR`. Queda la casilla manual para
quien ya lo tenía: impedirle cerrar el mes a alguien que hizo bien su trabajo sería tratarlo
como sospechoso.

## Comandos

```bash
npm start        # servidor de desarrollo con el proxy puesto
npm run build    # compilación de producción
npm test         # pruebas unitarias (Karma)
```
