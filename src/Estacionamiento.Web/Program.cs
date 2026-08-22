using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using Estacionamiento.Aplicacion;
using Estacionamiento.Infraestructura;
using Estacionamiento.Infraestructura.Persistencia;
using Estacionamiento.Web.Infraestructura;
using Microsoft.OpenApi.Models;

var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AgregarInfraestructura(constructor.Configuration);
constructor.Services.AgregarAplicacion();

// Sólo lo usa la siembra de demostración, que es opcional y se decide al desplegar.
constructor.Services.AddScoped<SembradorDeDatos>();

constructor.Services.AddControllers()
    .AddJsonOptions(json =>
    {
        // Los enumerados viajan como su nombre ("AFinDeMes"), no como el ordinal: insertar un
        // valor nuevo en medio del enum no debe cambiar el significado de un 2 ya publicado.
        json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Errores en formato application/problem+json, incluidos los 404 y 405 que produce el enrutador.
constructor.Services.AddProblemDetails();
constructor.Services.AddExceptionHandler<ManejadorDeExcepcionesDelDominio>();

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Estacionamiento",
        Description =
            "Backend del estacionamiento: entradas, salidas, padrón de vehículos e informes de fin de mes.\n\n" +
            "**Tarifas** — Oficial: no paga. Residente: MXN$0.05/min, liquida a fin de mes. " +
            "No residente: MXN$0.5/min, paga al salir.\n\n" +
            "Las placas se normalizan solas: `abc-1234`, ` ABC1234 ` y `ABC1234` son el mismo vehículo."
    });

    // Los comentarios XML de los controladores son la documentación de la API: se generan en el
    // .csproj y se leen aquí para no mantener dos descripciones que se contradigan con el tiempo.
    var documentacion = Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    if (File.Exists(documentacion))
    {
        swagger.IncludeXmlComments(documentacion, includeControllerXmlComments: true);
    }
});

var aplicacion = constructor.Build();

// Fechas e importes en formato mexicano, sin depender de la configuración del servidor
// ni de la cabecera Accept-Language del cliente: el estacionamiento está donde está.
var culturaMexico = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = culturaMexico;
CultureInfo.DefaultThreadCurrentUICulture = culturaMexico;

// Deja la base de datos al día antes de atender la primera petición.
using (var ambito = aplicacion.Services.CreateScope())
{
    var inicializador = ambito.ServiceProvider.GetRequiredService<InicializadorBaseDeDatos>();
    await inicializador.InicializarAsync();
}

// Datos de demostración, si "Siembra:Cantidad" lo pide. Desactivado por omisión.
await aplicacion.SembrarSiSePideAsync();

aplicacion.UseExceptionHandler();
aplicacion.UseStatusCodePages();

// Se deja puesto también fuera de desarrollo: es el backend de un estacionamiento en una red
// interna, y quien lo integra necesita el contrato a mano más de lo que aquí estorba tenerlo.
aplicacion.UseSwagger();
aplicacion.UseSwaggerUI(interfaz =>
{
    interfaz.SwaggerEndpoint("/swagger/v1/swagger.json", "Estacionamiento v1");
    interfaz.DocumentTitle = "API del estacionamiento";
});

aplicacion.MapControllers();

// La raíz manda a la documentación: entrar a "/" y ver un 404 parece que el contenedor no arrancó.
aplicacion.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Sonda para el healthcheck del contenedor. No toca la base de datos a propósito: responde si el
// proceso puede atender peticiones, que es lo que Compose necesita saber para dar por arrancado
// el servicio. El arranque ya falla solo si la base no está.
aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok" })).ExcludeFromDescription();

aplicacion.Run();
