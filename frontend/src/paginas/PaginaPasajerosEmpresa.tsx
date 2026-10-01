import { useEffect, useMemo, useState } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { NOMBRES_ESTADO_PASAJERO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { PasajeroEmpresa } from '../modelos/estadisticas'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerPasajerosEmpresa } from '../servicios/servicioEstadisticas'
import '../estilos/paginas/PaginaRutasEmpresa.css'

/**
 * Pasajeros de las rutas de la empresa: todos los asignados o solo los ya
 * transportados, con búsqueda por cédula o nombre y filtro por fechas.
 */
export function PaginaPasajerosEmpresa() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const [parametros, setParametros] = useSearchParams()
  const soloTransportados = parametros.get('transportados') === '1'
  const [busqueda, setBusqueda] = useState('')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [pasajeros, setPasajeros] = useState<PasajeroEmpresa[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !empresaId) return
    setCargando(true)
    obtenerPasajerosEmpresa(Number(empresaId), soloTransportados, desde, hasta, token)
      .then(setPasajeros)
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar los pasajeros.'))
      .finally(() => setCargando(false))
  }, [empresaId, soloTransportados, desde, hasta, token])

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase()
    return texto ? pasajeros.filter((p) => p.cedula.toLowerCase().includes(texto) || p.nombreCompleto.toLowerCase().includes(texto)) : pasajeros
  }, [pasajeros, busqueda])

  return (
    <ContenedorPagina ancho="amplio" volverA={`/empresas/${empresaId}`} textoVolver="← Inicio">
      <EncabezadoPagina titulo="Pasajeros" subtitulo="Empleados asignados a las rutas y su avance." />

      <div className="rutas-empresa__filtros">
        <button type="button" className={`rutas-empresa__pestana${!soloTransportados ? ' rutas-empresa__pestana--activa' : ''}`} onClick={() => setParametros({})}>
          Todos los asignados
        </button>
        <button type="button" className={`rutas-empresa__pestana${soloTransportados ? ' rutas-empresa__pestana--activa' : ''}`} onClick={() => setParametros({ transportados: '1' })}>
          Transportados
        </button>
      </div>

      <div className="rutas-empresa__fechas">
        <CampoFormulario id="pasajerosBusqueda" etiqueta="Buscar por cédula o nombre" valor={busqueda} alCambiar={setBusqueda} />
        <CampoFormulario id="pasajerosDesde" etiqueta="Desde" tipo="date" valor={desde} alCambiar={setDesde} />
        <CampoFormulario id="pasajerosHasta" etiqueta="Hasta" tipo="date" valor={hasta} alCambiar={setHasta} />
        <p className="rutas-empresa__total">
          {filtrados.length} pasajeros{pasajeros.length >= 2000 ? ' (se muestran los 2000 más recientes)' : ''}
        </p>
      </div>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : filtrados.length === 0 ? (
        <p className="contenedor-pagina__estado">No hay pasajeros con este filtro.</p>
      ) : (
        <div className="tabla-datos">
          <table>
            <thead>
              <tr>
                <th>Empleado</th>
                <th>Teléfono</th>
                <th>Fecha</th>
                <th>Hora</th>
                <th>Ruta</th>
                <th>Conductor</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map((p, indice) => (
                <tr key={`${p.servicioId}-${p.cedula}-${indice}`}>
                  <td>
                    {p.nombreCompleto}
                    <div className="rutas-empresa__sub">CC {p.cedula}</div>
                  </td>
                  <td>{p.telefono}</td>
                  <td>{p.fecha}</td>
                  <td>{formatearHora(p.hora)}</td>
                  <td>
                    {nombreDe(NOMBRES_TIPO_SERVICIO, p.tipo)} {p.sede}
                  </td>
                  <td>{p.conductor ?? <span className="rutas-empresa__sin-unidad">Sin unidad</span>}</td>
                  <td>{nombreDe(NOMBRES_ESTADO_PASAJERO, p.estado)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </ContenedorPagina>
  )
}
