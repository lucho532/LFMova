using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="ProgramacionTransporte"/> y sus
/// relaciones con <see cref="Empresa"/>, <see cref="Empleado"/> y
/// <see cref="Sede"/>.
/// </summary>
public class ProgramacionTransporteConfiguration : IEntityTypeConfiguration<ProgramacionTransporte>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="ProgramacionTransporte"/>.</summary>
    public void Configure(EntityTypeBuilder<ProgramacionTransporte> builder)
    {
        builder.HasKey(p => p.ProgramacionTransporteId);

        builder.HasOne(p => p.Empresa)
            .WithMany(e => e.ProgramacionesTransporte)
            .HasForeignKey(p => p.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Empleado)
            .WithMany(e => e.ProgramacionesTransporte)
            .HasForeignKey(p => p.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Sede)
            .WithMany(s => s.ProgramacionesTransporte)
            .HasForeignKey(p => p.SedeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.EmpresaId);
        builder.HasIndex(p => p.EmpleadoId);
        builder.HasIndex(p => p.SedeId);
        builder.HasIndex(p => p.Fecha);

        // Identifica una misma solicitud de transporte (mismo empleado, sede, fecha, hora y tipo): sin
        // esta restricción, dos importaciones concurrentes del mismo Excel (por ejemplo, un reintento por
        // doble clic mientras la primera todavía procesaba) podían crear dos programaciones duplicadas
        // para el mismo viaje en vez de reutilizar la ya creada.
        builder.HasIndex(p => new { p.EmpleadoId, p.SedeId, p.Fecha, p.Hora, p.Tipo }).IsUnique();
    }
}
