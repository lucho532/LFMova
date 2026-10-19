using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IFacturacionServicio"/>. Los totales de un mes
/// abierto se calculan del registro de rutas finalizadas; los de un mes
/// cerrado son los que quedaron guardados en su cierre. No accede
/// directamente a Entity Framework Core ni conoce precios.
/// </summary>
public class FacturacionServicio : IFacturacionServicio
{
    private readonly IFacturacionRepositorio _facturacionRepositorio;
    private readonly IGeneradorCierreMensual _generador;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public FacturacionServicio(IFacturacionRepositorio facturacionRepositorio, IGeneradorCierreMensual generador)
    {
        _facturacionRepositorio = facturacionRepositorio;
        _generador = generador;
    }

    /// <inheritdoc />
    public async Task<List<ResumenFacturacionEmpresaDto>> ObtenerResumenAsync(int anio, int mes)
    {
        ValidarMes(anio, mes);
        var (desde, hasta) = ReglasFacturacion.RangoDelMes(anio, mes);
        var resumen = await _facturacionRepositorio.ObtenerResumenAsync(desde, hasta);
        var cierres = (await _facturacionRepositorio.ObtenerCierresAsync(anio, mes)).ToDictionary(c => c.EmpresaId);

        foreach (var fila in resumen)
        {
            if (cierres.TryGetValue(fila.EmpresaId, out var cierre))
            {
                AplicarCierre(fila, cierre);
            }
        }

        return resumen;
    }

    /// <inheritdoc />
    public async Task<DetalleFacturacionDto> ObtenerDetalleAsync(int empresaId, int anio, int mes)
    {
        var resumen = (await ObtenerResumenAsync(anio, mes)).FirstOrDefault(r => r.EmpresaId == empresaId)
            ?? throw new InvalidOperationException("La empresa indicada no existe.");
        var (desde, hasta) = ReglasFacturacion.RangoDelMes(anio, mes);

        return new DetalleFacturacionDto
        {
            Resumen = resumen,
            Anio = anio,
            Mes = mes,
            Conductores = await _facturacionRepositorio.ObtenerConductoresAsync(empresaId, desde, hasta)
        };
    }

    /// <inheritdoc />
    public async Task<DetalleFacturacionDto> CerrarMesAsync(int empresaId, int anio, int mes)
    {
        var detalle = await ObtenerDetalleAsync(empresaId, anio, mes);
        if (detalle.Resumen.Cerrado)
        {
            throw new InvalidOperationException("Este mes ya está cerrado para la empresa.");
        }

        if (!ReglasFacturacion.MesTerminado(anio, mes, DateTime.UtcNow))
        {
            throw new InvalidOperationException("El mes todavía no ha terminado: solo se puede cerrar un mes ya finalizado.");
        }

        var cierre = new CierreMensual
        {
            EmpresaId = empresaId,
            Anio = anio,
            Mes = mes,
            FechaCierre = DateTime.UtcNow,
            ConductoresActivos = detalle.Resumen.ConductoresActivos,
            RutasFinalizadas = detalle.Resumen.RutasFinalizadas,
            PasajerosTransportados = detalle.Resumen.PasajerosTransportados
        };
        await _facturacionRepositorio.AgregarCierreAsync(cierre);
        await _facturacionRepositorio.GuardarCambiosAsync();

        AplicarCierre(detalle.Resumen, cierre);
        return detalle;
    }

    /// <inheritdoc />
    public async Task<ArchivoSoporte> DescargarAsync(int empresaId, int anio, int mes)
    {
        var detalle = await ObtenerDetalleAsync(empresaId, anio, mes);
        return new ArchivoSoporte($"facturacion-{anio:0000}-{mes:00}-empresa-{empresaId}.xlsx", _generador.Generar(detalle));
    }

    private static void ValidarMes(int anio, int mes)
    {
        if (!ReglasFacturacion.EsMesValido(anio, mes))
        {
            throw new InvalidOperationException("El mes indicado no es válido.");
        }
    }

    /// <summary>Sustituye los totales calculados por los que quedaron fijos en el cierre.</summary>
    private static void AplicarCierre(ResumenFacturacionEmpresaDto fila, CierreMensual cierre)
    {
        fila.Cerrado = true;
        fila.FechaCierre = cierre.FechaCierre;
        fila.ConductoresActivos = cierre.ConductoresActivos;
        fila.RutasFinalizadas = cierre.RutasFinalizadas;
        fila.PasajerosTransportados = cierre.PasajerosTransportados;
    }
}
