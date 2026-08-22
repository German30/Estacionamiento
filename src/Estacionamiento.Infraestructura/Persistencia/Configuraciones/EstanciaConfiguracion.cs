using Estacionamiento.Dominio.Estancias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estacionamiento.Infraestructura.Persistencia.Configuraciones;

/// <summary>Mapeo de las estancias: el registro de entradas y salidas que pide el enunciado.</summary>
public sealed class EstanciaConfiguracion : IEntityTypeConfiguration<Estancia>
{
    public void Configure(EntityTypeBuilder<Estancia> estancia)
    {
        estancia.ToTable("Estancias");

        estancia.HasKey(e => e.Id);
        estancia.Property(e => e.Id).ValueGeneratedOnAdd();

        estancia.Property(e => e.Entrada).IsRequired();

        // Nula mientras el vehículo sigue dentro.
        estancia.Property(e => e.Salida);

        estancia.Property(e => e.ImporteCobrado)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        // Derivadas de Entrada y Salida.
        estancia.Ignore(e => e.EstaAbierta);
        estancia.Ignore(e => e.Duracion);
        estancia.Ignore(e => e.MinutosFacturables);

        // Acelera "¿tiene este vehículo una estancia abierta?", la consulta más frecuente.
        // Que no haya dos abiertas a la vez lo garantiza el dominio: un índice único filtrado
        // exigiría SQL distinto en cada proveedor y ataría el modelo a uno concreto.
        estancia.HasIndex(e => new { e.VehiculoId, e.Salida });
    }
}
