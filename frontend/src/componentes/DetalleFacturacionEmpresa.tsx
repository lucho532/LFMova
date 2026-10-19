import { useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { DetalleFacturacion } from '../modelos/facturacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { cerrarMesFacturacion, descargarExcelFacturacion, obtenerDetalleFacturacion } from '../servicios/servicioFacturacion'
import { BotonSecundario } from './BotonSecundario'
import { MensajeAlerta } from './MensajeAlerta'
import { ModalConfirmacion } from './ModalConfirmacion'
import { TablaDatos } from './TablaDatos'
import '../estilos/componentes/DetalleFacturacionEmpresa.css'

interface PropiedadesDetalleFacturacionEmpresa {
  empresaId: number
  anio: number
  mes: number
  /** Indica si el mes ya terminó (solo entonces se ofrece cerrarlo). */
  mesTerminado: boolean
  /** Avisa de que el mes quedó cerrado, para refrescar el resumen. */
  alCerrar: () => void
}

/** Entrega un archivo ya descargado al navegador para que lo guarde con el nombre indicado. */
function guardarArchivo(nombre: string, archivo: Blob) {
  const url = URL.createObjectURL(archivo)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = nombre
  document.body.appendChild(enlace)
  enlace.click()
  enlace.remove()
  URL.revokeObjectURL(url)
}

/**
 * Detalle de facturación de una empresa en un mes: los conductores que
 * finalizaron rutas (el soporte del cobro), con las acciones de descargar el
 * Excel y cerrar el mes. Solo consulta y cierra: no calcula valores en dinero.
 */
export function DetalleFacturacionEmpresa({ empresaId, anio, mes, mesTerminado, alCerrar }: PropiedadesDetalleFacturacionEmpresa) {
  const { token } = useAutenticacion()
  const [detalle, setDetalle] = useState<DetalleFacturacion | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [confirmando, setConfirmando] = useState(false)
  const [ocupado, setOcupado] = useState(false)

  useEffect(() => {
    if (!token) return
    let cancelado = false
    obtenerDetalleFacturacion(empresaId, anio, mes, token)
      .then((datos) => {
        if (!cancelado) setDetalle(datos)
      })
      .catch((error) => {
        if (!cancelado) setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el detalle.')
      })
    return () => {
      cancelado = true
    }
  }, [empresaId, anio, mes, token])

  async function alDescargar() {
    if (!token) return
    setOcupado(true)
    setMensajeError(null)
    try {
      const { nombre, archivo } = await descargarExcelFacturacion(empresaId, anio, mes, token)
      guardarArchivo(nombre, archivo)
    } catch (error) {
      setMensajeError(error instanceof Error ? error.message : 'No se pudo descargar el soporte.')
    } finally {
      setOcupado(false)
    }
  }

  async function alConfirmarCierre() {
    setConfirmando(false)
    if (!token) return
    setOcupado(true)
    setMensajeError(null)
    try {
      setDetalle(await cerrarMesFacturacion(empresaId, anio, mes, token))
      alCerrar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cerrar el mes.')
    } finally {
      setOcupado(false)
    }
  }

  if (!detalle) {
    return mensajeError ? <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta> : <span>Cargando…</span>
  }

  return (
    <div className="detalle-facturacion-empresa">
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      <div className="detalle-facturacion-empresa__acciones">
        <BotonSecundario type="button" disabled={ocupado} onClick={alDescargar}>
          ⬇ Descargar Excel
        </BotonSecundario>
        {!detalle.resumen.cerrado && (
          <BotonSecundario
            type="button"
            disabled={ocupado || !mesTerminado}
            title={mesTerminado ? undefined : 'El mes todavía no ha terminado.'}
            onClick={() => setConfirmando(true)}
          >
            🔒 Cerrar mes
          </BotonSecundario>
        )}
      </div>

      {detalle.conductores.length === 0 ? (
        <p className="detalle-facturacion-empresa__vacio">Ningún conductor finalizó rutas en este mes.</p>
      ) : (
        <TablaDatos columnas={['Conductor', 'Placas', 'Rutas', 'Pasajeros', 'Primera ruta', 'Última ruta']}>
          {detalle.conductores.map((conductor) => (
            <tr key={conductor.cedula}>
              <td>
                {conductor.nombreConductor}
                <div className="detalle-facturacion-empresa__sub">
                  CC {conductor.cedula}
                  {conductor.eliminado && <span className="detalle-facturacion-empresa__eliminado"> · cuenta eliminada</span>}
                </div>
              </td>
              <td>{conductor.placas.join(', ') || '—'}</td>
              <td>{conductor.rutasFinalizadas}</td>
              <td>{conductor.pasajerosTransportados}</td>
              <td>{conductor.primeraRuta}</td>
              <td>{conductor.ultimaRuta}</td>
            </tr>
          ))}
        </TablaDatos>
      )}

      <ModalConfirmacion
        abierto={confirmando}
        titulo="¿Cerrar el mes?"
        mensaje={`Los totales de ${detalle.resumen.nombreEmpresa} quedarán fijos: ${detalle.resumen.conductoresActivos} conductores y ${detalle.resumen.rutasFinalizadas} rutas. No se puede deshacer.`}
        textoConfirmar="Cerrar mes"
        alConfirmar={alConfirmarCierre}
        alCancelar={() => setConfirmando(false)}
      />
    </div>
  )
}
