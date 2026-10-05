using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Repositorio de facturación en memoria para las pruebas unitarias: guarda
/// los registros de uso y los cierres en listas y calcula los resúmenes con
/// los datos que cada prueba le cargue. No contiene aserciones.
/// </summary>
internal class FacturacionRepositorioEnMemoria : IFacturacionRepositorio
{
    public readonly List<UsoConductor> Usos = new();
    public readonly List<CierreMensual> Cierres = new();
    public readonly List<(int EmpresaId, string Nombre)> Empresas = new();

    public Task AgregarUsoAsync(UsoConductor uso)
    {
        Usos.Add(uso);
        return Task.CompletedTask;
    }

    public Task<List<ResumenFacturacionEmpresaDto>> ObtenerResumenAsync(DateOnly desde, DateOnly hasta)
        => Task.FromResult(Empresas.Select(e =>
        {
            var delMes = Usos.Where(u => u.EmpresaId == e.EmpresaId && u.Fecha >= desde && u.Fecha <= hasta).ToList();
            return new ResumenFacturacionEmpresaDto
            {
                EmpresaId = e.EmpresaId,
                NombreEmpresa = e.Nombre,
                ConductoresActivos = delMes.Select(u => u.Cedula).Distinct().Count(),
                RutasFinalizadas = delMes.Count,
                PasajerosTransportados = delMes.Sum(u => u.PasajerosTransportados)
            };
        }).ToList());

    public Task<List<ConductorFacturableDto>> ObtenerConductoresAsync(int empresaId, DateOnly desde, DateOnly hasta)
        => Task.FromResult(Usos
            .Where(u => u.EmpresaId == empresaId && u.Fecha >= desde && u.Fecha <= hasta)
            .GroupBy(u => u.Cedula)
            .Select(g => new ConductorFacturableDto { Cedula = g.Key, NombreConductor = g.Last().NombreConductor, RutasFinalizadas = g.Count() })
            .ToList());

    public Task<List<CierreMensual>> ObtenerCierresAsync(int anio, int mes)
        => Task.FromResult(Cierres.Where(c => c.Anio == anio && c.Mes == mes).ToList());

    public Task AgregarCierreAsync(CierreMensual cierre)
    {
        Cierres.Add(cierre);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync() => Task.CompletedTask;
}

/// <summary>Repositorio de vehículos sin datos, para las pruebas que no necesitan la placa.</summary>
internal class VehiculoRepositorioVacio : IVehiculoRepositorio
{
    public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId) => Task.FromResult<Vehiculo?>(null);
    public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(new List<Vehiculo>());
    public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa) => Task.FromResult<Vehiculo?>(null);
    public Task AgregarAsync(Vehiculo vehiculo) => Task.CompletedTask;
    public Task GuardarCambiosAsync() => Task.CompletedTask;
}
