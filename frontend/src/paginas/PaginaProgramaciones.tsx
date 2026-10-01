import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { EnlaceBoton } from '../componentes/EnlaceBoton'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TablaDatos } from '../componentes/TablaDatos'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Empleado } from '../modelos/empleado'
import { NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Programacion } from '../modelos/operacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerEmpleados } from '../servicios/servicioEmpleados'
import { obtenerProgramaciones } from '../servicios/servicioOperacion'
import { obtenerSedes } from '../servicios/servicioSedes'

/**
 * Lista las programaciones de transporte de la empresa (fecha, hora,
 * empleado, sede y dirección de recogida). El filtro por fecha es local: la
 * API no ofrece filtros para este listado.
 */
export function PaginaProgramaciones() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const [programaciones, setProgramaciones] = useState<Programacion[]>([])
  const [empleados, setEmpleados] = useState<Empleado[]>([])
  const [sedes, setSedes] = useState<Sede[]>([])
  const [fechaFiltro, setFechaFiltro] = useState('')
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !empresaId) return
    const id = Number(empresaId)
    Promise.all([obtenerProgramaciones(id, token), obtenerEmpleados(id, token), obtenerSedes(id, token)])
      .then(([p, e, s]) => {
        setProgramaciones(p)
        setEmpleados(e)
        setSedes(s)
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las programaciones.'))
      .finally(() => setCargando(false))
  }, [empresaId, token])

  const filas = useMemo(
    () =>
      programaciones
        .filter((p) => !fechaFiltro || p.fecha === fechaFiltro)
        .sort((a, b) => (b.fecha + b.hora).localeCompare(a.fecha + a.hora)),
    [programaciones, fechaFiltro],
  )

  const nombreEmpleado = (id: number) => empleados.find((e) => e.empleadoId === id)?.nombreCompleto ?? `#${id}`
  const nombreSede = (id: number) => sedes.find((s) => s.sedeId === id)?.nombre ?? `#${id}`

  return (
    <ContenedorPagina>
      <EncabezadoPagina
        titulo="Programaciones"
        subtitulo="Necesidades de transporte de los empleados."
        acciones={<EnlaceBoton a={`/empresas/${empresaId}/programaciones/nueva`}>+ Nueva programación</EnlaceBoton>}
      />

      <div style={{ marginBottom: 20, maxWidth: 220 }}>
        <label htmlFor="filtroFecha" style={{ display: 'block', fontSize: 13, fontWeight: 600, marginBottom: 6 }}>
          Filtrar por fecha
        </label>
        <input id="filtroFecha" type="date" value={fechaFiltro} onChange={(e) => setFechaFiltro(e.target.value)} onClick={(e) => e.currentTarget.showPicker?.()} />
      </div>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : filas.length === 0 ? (
        <p className="contenedor-pagina__estado">No hay programaciones para mostrar.</p>
      ) : (
        <TablaDatos columnas={['Fecha', 'Hora', 'Tipo', 'Empleado', 'Sede', 'Recogida']}>
          {filas.map((p) => (
            <tr key={p.programacionTransporteId}>
              <td>
                <Link to={`/empresas/${empresaId}/programaciones/${p.programacionTransporteId}`}>{p.fecha}</Link>
              </td>
              <td>{formatearHora(p.hora)}</td>
              <td>{nombreDe(NOMBRES_TIPO_SERVICIO, p.tipo)}</td>
              <td>{nombreEmpleado(p.empleadoId)}</td>
              <td>{nombreSede(p.sedeId)}</td>
              <td>{p.direccionRecogida}</td>
            </tr>
          ))}
        </TablaDatos>
      )}
    </ContenedorPagina>
  )
}
