using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Empleado"/>: relación 1:0..1 con
/// <see cref="Usuario"/> y relación con la <see cref="Empresa"/> actual.
/// </summary>
public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Empleado"/>.</summary>
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.HasKey(e => e.EmpleadoId);

        builder.HasOne(e => e.Usuario)
            .WithOne(u => u.Empleado)
            .HasForeignKey<Empleado>(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Empresa)
            .WithMany(emp => emp.Empleados)
            .HasForeignKey(e => e.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UsuarioId).IsUnique();
        builder.HasIndex(e => e.EmpresaId);
    }
}
