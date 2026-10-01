using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="ServicioPasajero"/> y sus
/// relaciones con <see cref="Servicio"/>, <see cref="ProgramacionTransporte"/>
/// (1:0..1, única) y <see cref="Empleado"/>.
/// </summary>
public class ServicioPasajeroConfiguration : IEntityTypeConfiguration<ServicioPasajero>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="ServicioPasajero"/>.</summary>
    public void Configure(EntityTypeBuilder<ServicioPasajero> builder)
    {
        builder.HasKey(sp => sp.ServicioPasajeroId);

        builder.HasOne(sp => sp.Servicio)
            .WithMany(s => s.ServiciosPasajero)
            .HasForeignKey(sp => sp.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sp => sp.ProgramacionTransporte)
            .WithOne(p => p.ServicioPasajero)
            .HasForeignKey<ServicioPasajero>(sp => sp.ProgramacionTransporteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sp => sp.Empleado)
            .WithMany(e => e.ServiciosPasajero)
            .HasForeignKey(sp => sp.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sp => sp.ServicioId);
        builder.HasIndex(sp => sp.ProgramacionTransporteId).IsUnique();
        builder.HasIndex(sp => sp.EmpleadoId);
    }
}
