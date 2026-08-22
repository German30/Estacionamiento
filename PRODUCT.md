# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Dos roles usan la misma aplicación, en dos zonas separadas:

- **Operador de caseta.** Está en la pluma durante todo el turno. Le dictan una placa, la teclea
  y necesita saber en el acto si el vehículo entra o sale y cuánto cobrar. Trabaja de pie, con
  prisa y con un coche esperando: cada segundo de latencia percibida es un coche detenido.
- **Administrador.** Consulta el padrón, abre la ficha de un vehículo, revisa cuánto deben los
  residentes, descarga el informe de pagos y ejecuta el cierre de mes. Trabaja sentado, sin
  prisa, y sus acciones sí tienen consecuencias irreversibles.

Escena principal: monitor de escritorio en caseta u oficina. Debe seguir siendo usable en
teléfono, pero no se optimiza para táctil.

## Product Purpose

Poner interfaz al backend `Estacionamiento` (carpeta hermana `../Estacionamiento`), una API JSON
de ASP.NET Core que hoy sólo se puede operar desde Swagger o `curl`. El éxito es que un empleado
sin conocimiento de HTTP pueda registrar accesos y cerrar el mes sin equivocarse.

## Positioning

El backend ya resuelve las reglas de cobro y las expone como un contrato JSON estable y
versionado aparte del dominio. El front no reimplementa ninguna tarifa ni ningún redondeo: los
lee. Su valor está en el momento del cobro y en volver reversible-por-diseño lo que en la API es
un `POST` irreversible.

## Operating Context

- La API vive en `http://localhost:5209`. Documentación navegable en `/swagger`.
- **No tiene CORS configurado.** El navegador no puede llamarla directo desde `ng serve`; el
  front usa el proxy de desarrollo de Angular y rutas relativas `/api/...`.
- Se levanta con `docker compose up -d --build` desde `../Estacionamiento`. Con `SEMBRAR=100`
  puebla 100 vehículos (15 oficiales, 35 residentes, 50 no residentes) con estancias repartidas
  en los últimos 28 días, algunas todavía abiertas. La semilla es fija: los datos de demo son
  reproducibles.
- Zona horaria `America/Mexico_City`, cultura `es-MX`. Las fechas viajan como `DateTime` local
  sin offset.

## Capabilities and Constraints

Reglas de negocio, que el front muestra pero nunca calcula:

| Tipo | Tarifa | Cuándo paga |
|---|---|---|
| Oficial | — | Nunca |
| Residente | MXN$0.05/min | A fin de mes |
| No residente | MXN$0.5/min | Al salir |

- Sólo se dan de alta oficiales y residentes. **Una placa desconocida que entra se crea sola
  como no residente**, y la API lo avisa con `vehiculoRecienCreado: true`. Casi siempre eso
  significa que el operador tecleó mal la placa de un residente: es la advertencia más
  importante de toda la interfaz.
- Las placas se normalizan en el dominio: `abc-1234`, ` ABC 1234 ` y `ABC1234` son el mismo
  vehículo. Se aceptan de 5 a 10 alfanuméricos. El front no debe duplicar esa normalización
  como validación dura; sí puede previsualizarla.
- Toda fracción de minuto se cobra como minuto completo.
- **Comenzar mes es irreversible**: borra las estancias cerradas de oficiales y pone a cero los
  minutos de los residentes. La API exige teclear la palabra `COMENZAR`. Quien no descargue el
  informe antes pierde lo que se cobra ese mes.
- Los errores llegan como `application/problem+json` (RFC 9457). Un `409` nunca se arregla
  reintentando: significa "ya está dentro", "no está dentro" o "ya estaba dada de alta", y hay
  que cambiar la petición. El front no debe ofrecer reintentar ante un 409.
- No hay autenticación en la API. No hay concepto de sesión ni de usuario.

Endpoints (13): `POST /api/accesos/entradas`, `POST /api/accesos/salidas`,
`GET /api/accesos/dentro`, `GET /api/vehiculos` (con `?filtro=` y `?tipo=`),
`GET /api/vehiculos/{placa}`, `POST /api/vehiculos/oficiales`,
`POST /api/vehiculos/residentes`, `GET /api/panel`, `GET /api/cierre/informe`,
`GET /api/cierre/informe/descargar`, `POST /api/cierre/informe`, `POST /api/cierre/mes`,
`GET /salud`.

## Brand Commitments

Todo el proyecto nombra su código, sus rutas y su documentación en español. La interfaz también
va en español de México: moneda MXN, fechas locales. No hay logotipo, nombre comercial ni
paleta heredada.

## Evidence on Hand

- Contratos JSON exactos en `../Estacionamiento/src/Estacionamiento.Web/Contratos/`
  (`Peticiones.cs`, `Respuestas.cs`). Son la fuente de verdad de la forma del JSON.
- Formato fijo del informe de pagos (columnas de ancho fijo, dos decimales) en el README del
  backend. Lo impone el enunciado del ejercicio; el front no lo reformatea.
- Datos de demostración reproducibles vía `SEMBRAR`.
- **No hay** clientes reales, testimonios, métricas de uso, precios de licencia ni marca. Nada
  de eso debe inventarse en la interfaz.

## Product Principles

1. **El importe a cobrar es el momento de la verdad.** Todo lo demás en la pantalla de caseta
   está subordinado a que el operador lea la cifra correcta sin dudar.
2. **No recalcular lo que la API ya decidió.** Tarifas, minutos y redondeos se muestran tal como
   llegan. Duplicar la regla en TypeScript es crear una segunda verdad que se va a desviar.
3. **Lo irreversible se defiende, no se esconde.** El cierre de mes se puede encontrar, pero
   exige leer qué se va a destruir y tener el informe a salvo antes.
4. **Un 409 es una respuesta, no una falla.** Los conflictos de estado se explican en el
   lenguaje del estacionamiento, nunca como un error técnico ni con un botón de reintentar.
5. **Español de México en todo,** del identificador al mensaje de error.

## Accessibility & Inclusion

Sin requisito normativo establecido por el usuario. Restricción del contexto: la zona de caseta
se opera contra reloj y con la vista puesta en el coche, no en la pantalla, así que el foco de
teclado y los mensajes de estado deben ser audibles para lectores de pantalla y visibles sin
buscarlos.
