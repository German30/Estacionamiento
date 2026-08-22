# Estacionamiento

Backend para administrar el acceso de vehículos a un estacionamiento de pago. Se registran las
entradas y salidas; el sistema calcula qué cobrar según el tipo de vehículo y guarda cada
estancia en una base de datos a través de Entity Framework Core.

Se usa por HTTP: `Estacionamiento.Web` es una API JSON y es lo único que se despliega.

## Reglas de negocio

| Tipo de vehículo | Tarifa | Cuándo paga | Qué se guarda |
|---|---|---|---|
| Oficial | — | Nunca | Sus estancias, para llevar el control |
| Residente | MXN$0.05 / min | A fin de mes | Minutos acumulados del mes en curso |
| No residente | MXN$0.5 / min | Al salir | El importe cobrado en cada estancia |

Sólo se dan de alta los vehículos oficiales y los de residentes. Cualquier otra placa que entra
se registra automáticamente como **no residente**.

## Estructura de la solución

```
src/
  Estacionamiento.Dominio/          Vehículos, estancias y reglas de cobro. Sin dependencias.
  Estacionamiento.Aplicacion/       Los seis casos de uso e interfaces de persistencia.
  Estacionamiento.Infraestructura/  Entity Framework Core (MySQL), repositorios, conexión.
  Estacionamiento.Web/              API HTTP. Único proyecto ejecutable.
tests/
  Estacionamiento.Pruebas/          68 pruebas: reglas de cobro, informe, BD y siembra.
db/
  crear-base-de-datos.sql           Creación de la base en MySQL (local y contenedor).
Dockerfile, compose.yaml            La API y MySQL en contenedores.
```

Las dependencias apuntan hacia dentro: la API conoce la infraestructura, la infraestructura
conoce la aplicación, la aplicación conoce el dominio, y el dominio no conoce a nadie. Por eso
cambiar el manejador de base de datos no toca ni una línea de las reglas de negocio, y por eso
las pruebas ejercitan las tarifas sin levantar un servidor web.

Las tres bibliotecas no son ejecutables: se compilan dentro de la API. El único `dotnet run`
del repositorio es el de `Estacionamiento.Web`.

Este repositorio es **sólo backend**: la API sirve JSON y no tiene vistas ni archivos estáticos.

## Ejecutar

Lo más rápido es levantar la API y su base de datos en contenedores (ver [Docker](#docker)):

```bash
docker compose up -d --build      # API en http://localhost:5209/swagger
```

O directamente sobre el anfitrión, contra un MySQL local
(ver [Ejecutar la API sin Docker](#ejecutar-la-api-sin-docker)):

```bash
dotnet run --project src/Estacionamiento.Web
```

Las tablas se crean solas en el primer arranque, aplicando las migraciones sobre MySQL.

Pruebas:

```bash
dotnet test
```

## Docker

Levanta MySQL y la API sin instalar nada más que Docker:

```bash
docker compose up -d --build
```

La API queda en **http://localhost:5209**, con la documentación navegable en
**http://localhost:5209/swagger** (la raíz redirige ahí). Las tablas se crean solas en el primer
arranque, aplicando las migraciones.

```bash
docker compose logs -f web        # ver el arranque y las migraciones
docker compose ps                 # estado y healthchecks
docker compose down               # parar, conservando los datos
docker compose down --volumes     # parar y borrar también los datos
```

| Detalle | Cómo queda |
|---|---|
| Imagen | Compilación en dos etapas; sólo el runtime de ASP.NET en la final (396 MB) |
| Usuario | No root (`app`, uid 1654) |
| Conexión | `Persistencia__CadenaDeConexion` apunta al servicio `mysql` de la red de Compose |
| Arranque | La API espera a que MySQL esté *healthy*; si no, moriría migrando contra un puerto cerrado |
| Puerto | La API en el **5209**; MySQL en el **3307** del anfitrión, para no chocar con un MySQL local en el 3306 (ver [Puertos](#puertos)) |
| Datos | Volumen `estacionamiento-datos-mysql`; sobreviven a `docker compose down` |
| Informes | `./informes` del anfitrión se monta en `/informes` del contenedor |
| Zona horaria | `America/Mexico_City`, con `tzdata` instalado: sin él el contenedor creería vivir en UTC y las horas de entrada y salida —que es lo que se factura— saldrían desplazadas seis horas |

Estos valores se pueden pisar sin editar el `compose.yaml`, con variables de entorno o un `.env`:
`PUERTO_WEB`, `PUERTO_MYSQL`, `MYSQL_ROOT_PASSWORD`, `ASPNETCORE_ENVIRONMENT` y `SEMBRAR`
(ver [Datos de demostración](#datos-de-demostración)).

Para conectar MySQL Workbench a la base del contenedor: `127.0.0.1:3307`, usuario `root`.

### Puertos

El mismo número a los dos lados del contenedor, y conviene saber por qué:

```
anfitrión :5209  ->  contenedor :5209
```

**Dentro, el 5209.** La imagen `mcr.microsoft.com/dotnet/aspnet:9.0` de Microsoft trae el 8080
en `ASPNETCORE_HTTP_PORTS` —hasta .NET 6 era el 80; cambió en .NET 8 porque desde entonces las
imágenes se ejecutan **sin privilegios**, y en Linux los puertos por debajo de 1024 requieren
root—, pero el `Dockerfile` lo pisa con el 5209 para que el puerto no dependa de cómo se lance
la API. Cualquier número por encima de 1024 vale, precisamente porque el proceso no es root.

> Cambiarlo son cuatro sitios, y los cuatro tienen que ir juntos: `ASPNETCORE_HTTP_PORTS`,
> `EXPOSE` y el `HEALTHCHECK` del `Dockerfile`, más el destino del mapeo de `compose.yaml`.
> Si el mapeo apunta a otro puerto el fallo es traicionero: el healthcheck comprueba el puerto
> desde dentro del contenedor, así que Docker marca el servicio como `healthy` mientras que
> desde fuera no responde nadie.

**Fuera, el 5209.** Es el mismo que usa `dotnet run` en el anfitrión, para que la URL no cambie
según cómo se ejecute la API. Se cambia sin reconstruir nada:

```bash
PUERTO_WEB=9000 docker compose up -d      # http://localhost:9000/swagger
```

**El 5209 de `launchSettings.json` es otra cosa.** Ese archivo lo leen `dotnet run` y Visual
Studio, nadie más: no es configuración de la aplicación sino del lanzador, y ni siquiera se
publica dentro de la imagen. En el contenedor no interviene.

| Cómo se ejecuta | URL | De dónde sale el puerto |
|---|---|---|
| `docker compose up` | `http://localhost:5209` | `PUERTO_WEB` en `compose.yaml` |
| `dotnet run` en el anfitrión | `http://localhost:5209` | `Properties/launchSettings.json` |
| Dentro del contenedor | `http://+:5209` | `ASPNETCORE_HTTP_PORTS` en el `Dockerfile` |

## La API

Todas las respuestas son JSON. Los errores siguen `application/problem+json` (RFC 9457).

| Método | Ruta | Qué hace |
|---|---|---|
| `POST` | `/api/accesos/entradas` | Registra una entrada. Placa desconocida ⇒ se crea como no residente |
| `POST` | `/api/accesos/salidas` | Registra una salida y devuelve qué cobrar |
| `GET` | `/api/accesos/dentro` | Vehículos dentro ahora mismo, el que más lleva primero |
| `GET` | `/api/vehiculos` | Padrón. Filtra con `?filtro=` (fragmento de placa) y `?tipo=` |
| `GET` | `/api/vehiculos/{placa}` | Ficha con el historial de estancias |
| `POST` | `/api/vehiculos/oficiales` | Alta de vehículo oficial |
| `POST` | `/api/vehiculos/residentes` | Alta de vehículo de residente |
| `GET` | `/api/panel` | Cuántos hay dentro, cobrado hoy, saldo de residentes |
| `GET` | `/api/cierre/informe` | Calcula el informe de pagos de residentes. No modifica nada |
| `GET` | `/api/cierre/informe/descargar` | El mismo informe como archivo `.txt` |
| `POST` | `/api/cierre/informe` | Escribe el informe en el disco del servidor |
| `POST` | `/api/cierre/mes` | Comienza mes. **Irreversible** |
| `GET` | `/salud` | Sonda del healthcheck |

Un recorrido completo:

```bash
# Alta de un residente. La placa se normaliza: "abc-1234" queda como ABC1234.
curl -X POST http://localhost:5209/api/vehiculos/residentes \
     -H "Content-Type: application/json" -d '{"placa":"abc-1234"}'

# Entra y sale.
curl -X POST http://localhost:5209/api/accesos/entradas \
     -H "Content-Type: application/json" -d '{"placa":"ABC1234"}'
curl -X POST http://localhost:5209/api/accesos/salidas \
     -H "Content-Type: application/json" -d '{"placa":"ABC1234"}'

# El informe de fin de mes, escrito en ./informes del anfitrión.
curl -X POST http://localhost:5209/api/cierre/informe \
     -H "Content-Type: application/json" -d '{"ruta":"/informes/pagos-agosto.txt"}'
```

Comenzar mes borra las estancias de los oficiales y pone a cero los minutos de los residentes.
Como no se puede deshacer, acertar la ruta no basta: hay que confirmar con la palabra.

```bash
curl -X POST http://localhost:5209/api/cierre/mes \
     -H "Content-Type: application/json" -d '{"confirmacion":"COMENZAR"}'
```

Descargue antes el informe, o lo que se cobra este mes se pierde.

### Códigos de estado

| Código | Cuándo |
|---|---|
| `201` | Alta o entrada registrada. `Location` apunta a la ficha del vehículo |
| `200` | Consulta, salida registrada o mes comenzado |
| `400` | La placa no es válida, falta un campo, o falta la confirmación del cierre |
| `404` | Esa placa no está registrada |
| `409` | La placa es buena pero el estacionamiento no está en el estado que hace falta: ya está dentro, no está dentro, o ya estaba dada de alta |

Un `409` nunca se arregla reintentando: hay que cambiar la petición. Por eso las reglas de
negocio no salen como `500`, que invitaría a un reintento que tampoco va a funcionar.

## Ejecutar la API sin Docker

```bash
dotnet run --project src/Estacionamiento.Web
```

Queda en `http://localhost:5209/swagger`, contra el MySQL de `appsettings.json`
(`127.0.0.1:3306`). Ajuste ahí la cadena de conexión, o písela con una variable de entorno:

```bash
Persistencia__CadenaDeConexion="Server=127.0.0.1;Port=3306;Database=estacionamiento;User Id=root;Password=root123;"
```

## Datos de demostración

Una API sin datos no se puede explorar. Para poblar la base con un juego realista, levante el
contenedor con `SEMBRAR`:

```bash
SEMBRAR=100 docker compose up -d --build
```

En PowerShell:

```powershell
$env:SEMBRAR=100; docker compose up -d --build
```

Genera 100 vehículos (15 oficiales, 35 residentes, 50 no residentes) con estancias repartidas
por los últimos 28 días, algunas de ellas todavía abiertas.

Es **idempotente**: si la base ya tiene vehículos no hace nada, así que dejar `SEMBRAR` puesto no
reconstruye los datos en cada reinicio. Para volver a empezar, `SEMBRAR_REINICIAR=true` vacía las
tablas antes de sembrar.

Fuera de Docker es la sección `Siembra` de `appsettings.json`, o su variable de entorno:

```bash
Siembra__Cantidad=100 dotnet run --project src/Estacionamiento.Web
```

> Va por configuración y no por un endpoint a propósito: sembrar con reinicio borra todos los
> vehículos, y eso no debe poder dispararlo cualquiera que alcance el puerto. Como variable de
> entorno, la decisión es de quien despliega.

Las estancias se crean llamando a `RegistrarEntrada` y `RegistrarSalida` del dominio, no
insertando filas a mano: los importes cobrados y los minutos acumulados los calculan las mismas
reglas que en producción, así que los datos sembrados no pueden contradecir a las tarifas. La
semilla del generador es fija, de modo que sembrar dos veces produce exactamente el mismo juego
de datos.

## Informe de pagos de residentes

`GET /api/cierre/informe/descargar` sirve el informe con el formato del enunciado
(UTF-8, columnas de ancho fijo, importes con dos decimales y punto decimal):

```
Núm. placa    Tiempo estacionado (min.)    Cantidad a pagar
S1234A                            20134             1006.70
4567ABC                            4896              244.80
XY99Z                                 0                0.00
MNO4567                          132480             6624.00
```

Aparecen **todos** los residentes dados de alta, ordenados por placa, incluidos los que no
usaron el estacionamiento (con 0 minutos y 0.00). Los totales no van en el archivo —el enunciado
fija el formato— pero `GET /api/cierre/informe` los devuelve aparte, en `totalDeMinutos` y
`totalAPagar`, junto con las líneas ya desglosadas por si el cliente prefiere pintarlas él.

Para dejarlo escrito en el disco del servidor en lugar de descargarlo, `POST /api/cierre/informe`
con la ruta deseada. En el contenedor, `/informes` está montado contra `./informes`.

## Persistencia

La aplicación usa **MySQL** a través de Entity Framework Core. El mapeo es **tabla por
jerarquía**: una tabla `Vehiculos` con la columna discriminadora `TipoDeVehiculo`, y una tabla
`Estancias` con borrado en cascada.

```
vehiculos                              estancias
  Id                 int AI PK           Id              int AI PK
  Placa              varchar(10) UQ      VehiculoId      int FK -> vehiculos.Id
  FechaDeAlta        datetime(6)         Entrada         datetime(6)
  TipoDeVehiculo     varchar(13)         Salida          datetime(6) NULL
  MinutosAcumulados  int NULL            ImporteCobrado  decimal(10,2)
```

`MinutosAcumulados` queda en `NULL` para los tipos que no acumulan tiempo (oficial y no
residente); un 0 se leería como "estuvo estacionado 0 minutos", que no es lo mismo que
"esta columna no le aplica".

### Preparar la base de datos

```bash
mysql --user=root --password < db/crear-base-de-datos.sql
```

Las **tablas no se crean ahí**: las genera Entity Framework Core la primera vez que arranca la
aplicación, aplicando las migraciones.

### Conexión

Se configura en la sección `Persistencia` de
[src/Estacionamiento.Web/appsettings.json](src/Estacionamiento.Web/appsettings.json):

```json
{
  "Persistencia": {
    "Proveedor": "MySql",
    "CadenaDeConexion": "Server=127.0.0.1;Port=3306;Database=estacionamiento;User Id=root;Password=root123;",
    "VersionDelServidor": "8.1.0",
    "EstrategiaDeEsquema": "Migraciones"
  }
}
```

- `Proveedor`: `MySql`, `Sqlite` o `SqlServer`.
- `VersionDelServidor`: versión de MySQL contra la que generar el SQL. Vacío o `auto` la detecta
  sola, a costa de abrir una conexión de sondeo en cada arranque.
- `EstrategiaDeEsquema`: `Migraciones` (aplica las pendientes al arrancar),
  `CrearSiNoExiste` (crea el esquema desde el modelo) o `Ninguna`.

La cadena de conexión se puede sobrescribir sin tocar el archivo, con una variable de entorno:

```bash
set Persistencia__CadenaDeConexion=Server=otro-host;Database=estacionamiento;User Id=app;Password=...
```

Es lo que hace `compose.yaml`: apunta la cadena al servicio `mysql` de la red de Compose y sube
`VersionDelServidor` a `8.4.0`, la de la imagen `mysql:8.4`, sin tocar el archivo. Si se cambia
la etiqueta de esa imagen, hay que cambiar también ese valor.

> La contraseña está en `appsettings.json` porque así se pidió para este entorno de desarrollo.
> Para cualquier equipo compartido, sáquela de ahí y use la variable de entorno o
> `dotnet user-secrets`: el archivo va a control de versiones.

### Cambiar de manejador

Las migraciones son específicas del proveedor y las incluidas son de MySQL. Para otro manejador
hay que generar las suyas, indicándolo tras el separador `--`:

```bash
dotnet ef migrations add EsquemaInicial \
  --project src/Estacionamiento.Infraestructura \
  --output-dir Persistencia/Migraciones \
  -- --proveedor Sqlite
```

Como alternativa rápida, con `"EstrategiaDeEsquema": "CrearSiNoExiste"` el esquema se crea
directamente desde el modelo en cualquier proveedor, sin migraciones.

## Añadir un tipo de vehículo nuevo

El enunciado pide que esto sea fácil. Son tres pasos y ningún cambio en los casos de uso:

**1.** Heredar de `Vehiculo` e implementar qué pasa al cerrar una estancia:

```csharp
public sealed class VehiculoDeEmpleado : Vehiculo
{
    public const string Discriminador = "Empleado";
    public const decimal Tarifa = 0.02m;

    private VehiculoDeEmpleado() { }
    public VehiculoDeEmpleado(Placa placa, DateTime fechaDeAlta) : base(placa, fechaDeAlta) { }

    public override string Tipo => "Empleado";
    public override decimal TarifaPorMinuto => Tarifa;
    public override MomentoDeCobro MomentoDeCobro => MomentoDeCobro.ALaSalida;

    protected override ResultadoSalida AlCerrarEstancia(Estancia estancia)
    {
        var importe = PoliticaDeImporte.Calcular(estancia.MinutosFacturables, TarifaPorMinuto);
        estancia.RegistrarImporte(importe);
        return ResultadoSalida.CobroInmediato(
            this, estancia.Entrada, estancia.Salida!.Value, estancia.MinutosFacturables, importe);
    }
}
```

Si además acumula saldo de mes, se sobrescribe `ComenzarMes()`.

**2.** Declarar su discriminador en `VehiculoConfiguracion`:

```csharp
.HasValue<VehiculoDeEmpleado>(VehiculoDeEmpleado.Discriminador)
```

**3.** Generar la migración:

```bash
dotnet ef migrations add AgregaVehiculoDeEmpleado \
  --project src/Estacionamiento.Infraestructura --output-dir Persistencia/Migraciones
```

`ServicioEstacionamiento`, los repositorios y los controladores no cambian. Sólo hace falta
añadir una ruta de alta en `VehiculosController` si el tipo nuevo se da de alta a mano, como el
oficial y el residente.

## Decisiones y supuestos

Cosas que el enunciado no fija y que hubo que decidir:

- **Fracciones de minuto.** Toda fracción se cobra como minuto completo (convención habitual en
  estacionamientos). La regla está aislada en `PoliticaDeTiempo`, así que cambiarla es una línea.
- **Importes.** `decimal` con dos decimales, mitad hacia arriba (`PoliticaDeImporte`).
- **Placas.** Se normalizan a mayúsculas sin espacios ni guiones, de modo que `abc-1234` y
  `ABC 1234` son el mismo vehículo. Se aceptan entre 5 y 10 caracteres alfanuméricos.
- **Comienza mes con vehículos dentro.** Se eliminan sólo las estancias ya cerradas: borrar la
  entrada de un coche que sigue dentro impediría registrar su salida.
- **Comienza mes y no residentes.** Su histórico no se toca; es el registro de lo ya cobrado.
- **Alta de una placa ya registrada.** Se rechaza indicando el tipo que ya tiene. Reclasificar un
  vehículo (por ejemplo, un no residente que pasa a residente) sería otro caso de uso: cambiar de
  tipo en una jerarquía TPH obliga a borrar y volver a insertar la fila.
- **Fechas.** `DateTime` en hora local del equipo, que es la del estacionamiento.

## Estado del proyecto web

`Estacionamiento.Web` es una **API HTTP, sin interfaz**. La aplicación web MVC anterior y su capa
de MongoDB se eliminaron: tenían un modelo de datos distinto, no estaban conectadas a este
backend, y MongoDB ya no se usa en ninguna parte.

El frontend se hará aparte y consumirá esta API. Por eso los contratos del JSON viven en
[src/Estacionamiento.Web/Contratos/](src/Estacionamiento.Web/Contratos/) como tipos propios, y no
se serializan las entidades del dominio: la forma del JSON es un compromiso con quien la
consume, y no debe moverse porque el dominio se reorganice por dentro.

## Requisitos

Con Docker basta con Docker Desktop. Para ejecutar en el anfitrión:

- .NET SDK 9.0 (lo fija `global.json`)
- MySQL 8.x en `127.0.0.1:3306`
- `dotnet-ef` sólo si se van a generar migraciones: `dotnet tool install --global dotnet-ef`
