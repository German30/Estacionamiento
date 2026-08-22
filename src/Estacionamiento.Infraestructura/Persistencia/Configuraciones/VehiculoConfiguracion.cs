using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estacionamiento.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de la jerarquía de vehículos con estrategia "tabla por jerarquía" (TPH): una sola
/// tabla <c>Vehiculos</c> y una columna discriminadora que dice de qué tipo es cada fila.
/// </summary>
public sealed class VehiculoConfiguracion : IEntityTypeConfiguration<Vehiculo>
{
    /// <summary>Nombre de la columna discriminadora.</summary>
    public const string ColumnaDiscriminadora = "TipoDeVehiculo";

    public void Configure(EntityTypeBuilder<Vehiculo> vehiculo)
    {
        vehiculo.ToTable("Vehiculos");

        vehiculo.HasKey(v => v.Id);
        vehiculo.Property(v => v.Id).ValueGeneratedOnAdd();

        // La placa es un objeto de valor: se guarda como texto y se reconstruye al leer.
        vehiculo.Property(v => v.Placa)
            .HasColumnName("Placa")
            .HasConversion(placa => placa.Valor, valor => Placa.DesdeAlmacenamiento(valor))
            .HasMaxLength(Placa.LongitudMaxima)
            .IsRequired();

        // Identidad de negocio: no puede haber dos vehículos con la misma placa.
        vehiculo.HasIndex(v => v.Placa).IsUnique();

        vehiculo.Property(v => v.FechaDeAlta).IsRequired();

        // Propiedades derivadas: se calculan en el dominio, no se persisten.
        vehiculo.Ignore(v => v.Tipo);
        vehiculo.Ignore(v => v.TarifaPorMinuto);
        vehiculo.Ignore(v => v.MomentoDeCobro);
        vehiculo.Ignore(v => v.EstanciaAbierta);

        vehiculo.HasMany(v => v.Estancias)
            .WithOne(estancia => estancia.Vehiculo)
            .HasForeignKey(estancia => estancia.VehiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        // La colección se expone como sólo lectura, así que EF escribe sobre el campo.
        vehiculo.Metadata
            .FindNavigation(nameof(Vehiculo.Estancias))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Punto de extensión: un tipo de vehículo nuevo es una línea más en esta lista.
        vehiculo.HasDiscriminator<string>(ColumnaDiscriminadora)
            .HasValue<VehiculoOficial>(VehiculoOficial.Discriminador)
            .HasValue<VehiculoResidente>(VehiculoResidente.Discriminador)
            .HasValue<VehiculoNoResidente>(VehiculoNoResidente.Discriminador);
    }
}

/// <summary>Lo que sólo tiene el residente: el tiempo acumulado del mes en curso.</summary>
public sealed class VehiculoResidenteConfiguracion : IEntityTypeConfiguration<VehiculoResidente>
{
    public void Configure(EntityTypeBuilder<VehiculoResidente> residente)
    {
        // Sin valor por omisión a propósito: en una jerarquía de tabla única la columna queda
        // nula para los tipos que no la usan, y un 0 se leería como "estuvo estacionado 0 min"
        // en un vehículo oficial, que ni siquiera acumula tiempo.
        residente.Property(r => r.MinutosAcumulados)
            .HasColumnName("MinutosAcumulados");

        residente.Ignore(r => r.SaldoPendiente);
    }
}
