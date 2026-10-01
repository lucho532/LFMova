using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Servicio"/> y sus relaciones con
/// <see cref="Jornada"/>, <see cref="Sede"/> y <see cref="UnidadOperativa"/>.
/// No define <c>EmpresaId</c> propio: se obtiene a través de la jornada.
/// <c>UnidadOperativaId</c> sí pertenece a <see cref="Servicio"/> (asignación
/// individual, opcional hasta que se asigne).
/// </summary>
public class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Servicio"/>.</summary>
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.HasKey(s => s.ServicioId);

        builder.HasOne(s => s.Jornada)
            .WithMany(j => j.Servicios)
            .HasForeignKey(s => s.JornadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.UnidadOperativa)
            .WithMany(u => u.Servicios)
            .HasForeignKey(s => s.UnidadOperativaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Sede)
            .WithMany(sede => sede.Servicios)
            .HasForeignKey(s => s.SedeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.JornadaId);
        builder.HasIndex(s => s.UnidadOperativaId);
        builder.HasIndex(s => s.SedeId);
        builder.HasIndex(s => s.Fecha);
        builder.HasIndex(s => s.Estado);
    }
}
