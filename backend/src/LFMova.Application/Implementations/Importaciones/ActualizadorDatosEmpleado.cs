using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Pone al día los datos de un empleado que ya existe con lo que trae el archivo importado: nombre, teléfono,
/// dirección y barrio. La cédula nunca cambia (es lo que identifica a la persona). Un dato que el archivo trae
/// vacío no borra el que ya está, y el teléfono nunca pasa de un celular a un fijo. De la cuenta de la persona
/// (<c>Usuario</c>) solo se actualiza nombre y teléfono mientras no la haya activado. Los datos históricos de
/// servicios y programaciones no se tocan. No guarda nada: quien lo llama persiste los cambios.
/// </summary>
public static class ActualizadorDatosEmpleado
{
    /// <summary>
    /// Aplica los datos del archivo al empleado y a su cuenta. Devuelve si algo cambió. Si cambió un dato que ya
    /// tenía valor (no uno vacío que se llenó) deja un solo aviso en <paramref name="resultado"/> con lo que
    /// cambió, para poder revisarlo.
    /// </summary>
    public static bool Aplicar(Empleado empleado, Usuario usuario, FilaHoja fila, ResultadoImportacionDto resultado)
    {
        var cambios = new List<string>();
        var hubo = false;

        hubo |= ActualizarTexto(empleado.NombreCompleto, fila.NombreCompleto, nuevo => empleado.NombreCompleto = nuevo, "nombre", cambios);
        hubo |= ActualizarTexto(empleado.Direccion, fila.Direccion, nuevo => empleado.Direccion = nuevo, "dirección", cambios);
        hubo |= ActualizarTexto(empleado.Barrio, fila.Barrio, nuevo => empleado.Barrio = nuevo, "barrio", cambios);

        if (SeleccionadorTelefono.DebeReemplazar(empleado.Telefono, fila.Celular))
        {
            if (SeleccionadorTelefono.EsCelular(empleado.Telefono))
            {
                cambios.Add($"celular {empleado.Telefono} → {fila.Celular}");
            }

            empleado.Telefono = fila.Celular;
            hubo = true;
        }

        if (usuario.PasswordHash is null)
        {
            hubo |= ActualizarTexto(usuario.NombreCompleto, fila.NombreCompleto, nuevo => usuario.NombreCompleto = nuevo, "nombre", new List<string>());
            if (SeleccionadorTelefono.DebeReemplazar(usuario.Telefono, fila.Celular))
            {
                usuario.Telefono = fila.Celular;
                hubo = true;
            }
        }

        if (cambios.Count > 0)
        {
            resultado.Advertencias.Add($"Se actualizaron los datos de {fila.NombreCompleto} (cédula {fila.Cedula}): {string.Join(", ", cambios.Distinct())}.");
        }

        return hubo;
    }

    /// <summary>
    /// Reemplaza el valor si el archivo trae uno distinto (ignorando espacios de más). Solo cuenta como cambio para
    /// el aviso si el valor anterior no estaba vacío y difiere de verdad, no apenas en mayúsculas o tildes.
    /// </summary>
    private static bool ActualizarTexto(string actual, string delArchivo, Action<string> asignar, string nombre, List<string> cambios)
    {
        var nuevo = Limpiar(delArchivo);
        if (nuevo.Length == 0 || nuevo == actual)
        {
            return false;
        }

        if (Limpiar(actual).Length > 0 && TextoNormalizador.Normalizar(Limpiar(actual)) != TextoNormalizador.Normalizar(nuevo))
        {
            cambios.Add(nombre);
        }

        asignar(nuevo);
        return true;
    }

    private static string Limpiar(string texto) => string.Join(' ', (texto ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
