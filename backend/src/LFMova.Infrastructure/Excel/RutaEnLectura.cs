using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Ruta en proceso de lectura: va tomando su hora, tipo, fecha, sede y placa de las filas de sus
/// pasajeros (y, si las filas no los traen, de la línea que la abrió) y avisa cuando las filas se
/// contradicen. No agrupa filas ni valida contra la empresa.
/// </summary>
internal sealed class RutaEnLectura
{
    private readonly InterpretadorLineaInicioRuta.DatosLinea? _linea;
    private bool _tieneHorario;

    internal RutaEnLectura(string referencia, InterpretadorLineaInicioRuta.DatosLinea? linea = null)
    {
        Ruta = new RutaArmadaHoja { Referencia = referencia };
        _linea = linea;
    }

    internal RutaArmadaHoja Ruta { get; }

    /// <summary>Incorpora los datos de ruta de una fila de pasajero; ante una contradicción deja un error o un aviso y conserva el primer valor.</summary>
    internal void AgregarDatos(int fila, (TipoServicio Tipo, TimeOnly Hora)? horario, DateOnly? fecha, string sede, string placa, HojaRutasArmadas resultado)
    {
        if (horario is not null)
        {
            if (!_tieneHorario)
            {
                (Ruta.Tipo, Ruta.Hora) = horario.Value;
                _tieneHorario = true;
            }
            else if (Ruta.Tipo != horario.Value.Tipo || Ruta.Hora != horario.Value.Hora)
            {
                resultado.Errores.Add($"Fila {fila}: el horario no coincide con el de la ruta \"{Ruta.Referencia}\" ({Ruta.Hora:HH\\:mm}).");
            }
        }

        if (fecha is not null)
        {
            if (Ruta.Fecha is null)
            {
                Ruta.Fecha = fecha;
            }
            else if (Ruta.Fecha != fecha)
            {
                resultado.Errores.Add($"Fila {fila}: la fecha no coincide con la de la ruta \"{Ruta.Referencia}\" ({Ruta.Fecha:yyyy-MM-dd}).");
            }
        }

        Ruta.NombreSede = Tomar(Ruta.NombreSede, sede, fila, "sede", resultado);
        Ruta.Placa = Tomar(Ruta.Placa, placa, fila, "placa", resultado);
    }

    /// <summary>Completa con la línea de inicio lo que las filas no trajeron. Devuelve <c>false</c> si la ruta quedó sin hora y no se puede importar.</summary>
    internal bool CerrarYValidar(HojaRutasArmadas resultado)
    {
        if (!_tieneHorario && _linea?.Hora is not null)
        {
            Ruta.Hora = _linea.Hora.Value;
            Ruta.Tipo = _linea.Tipo ?? TipoServicio.ENTRADA;
            _tieneHorario = true;
            if (_linea.Tipo is null)
            {
                resultado.Advertencias.Add($"La ruta \"{Ruta.Referencia}\" no indica si es de entrada o de salida: se tomó como entrada.");
            }
        }
        else if (_tieneHorario && _linea is not null && (_linea.Hora != Ruta.Hora && _linea.Hora is not null || _linea.Tipo != Ruta.Tipo && _linea.Tipo is not null))
        {
            resultado.Advertencias.Add($"La línea \"{Ruta.Referencia}\" menciona un horario distinto al de sus pasajeros: se usó el de los pasajeros.");
        }

        if (Ruta.Placa.Length == 0 && _linea?.Placa is not null)
        {
            Ruta.Placa = _linea.Placa;
        }

        if (!_tieneHorario)
        {
            resultado.Errores.Add($"La ruta \"{Ruta.Referencia}\" no tiene hora de entrada ni de salida.");
        }

        return _tieneHorario;
    }

    private string Tomar(string actual, string nuevo, int fila, string nombre, HojaRutasArmadas resultado)
    {
        if (nuevo.Length == 0)
        {
            return actual;
        }

        if (actual.Length == 0)
        {
            return nuevo;
        }

        if (TextoNormalizador.Normalizar(actual) != TextoNormalizador.Normalizar(nuevo))
        {
            resultado.Advertencias.Add($"Fila {fila}: la {nombre} \"{nuevo}\" es distinta a la de la ruta \"{Ruta.Referencia}\" (\"{actual}\"): se usó la primera.");
        }

        return actual;
    }
}
