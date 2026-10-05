import { Fragment, useEffect, useState } from 'react'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { DetalleFacturacionEmpresa } from '../componentes/DetalleFacturacionEmpresa'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TablaDatos } from '../componentes/TablaDatos'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { ResumenFacturacionEmpresa } from '../modelos/facturacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerResumenFacturacion } from '../servicios/servicioFacturacion'
import '../estilos/paginas/PaginaFacturacion.css'

/** Mes de Colombia (UTC−5) con el desplazamiento indicado, en formato "AAAA-MM" (el de un campo de tipo mes). */
function mesColombia(desplazamiento: number): string {
  const ahora = new Date(Date.now() - 5 * 60 * 60 * 1000)
  const fecha = new Date(Date.UTC(ahora.getUTCFullYear(), ahora.getUTCMonth() + desplazamiento, 1))
  return `${fecha.getUTCFullYear()}-${String(fecha.getUTCMonth() + 1).padStart(2, '0')}`
}

/**
 * Facturación del administrador de plataforma: cuántos conductores
 * finalizaron rutas en cada empresa en el mes elegido (la base del cobro
 * mensual), con el detalle por conductor, el cierre del mes y su Excel.
 * Abre en el mes anterior, que es el que se factura, y permite ver el mes
 * en curso para seguir cómo va. No maneja precios.
 */
export function PaginaFacturacion() {
  const { token } = useAutenticacion()
  const [mesElegido, setMesElegido] = useState(() => mesColombia(-1))
  const [resumen, setResumen] = useState<ResumenFacturacionEmpresa[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [abierta, setAbierta] = useState<number | null>(null)
  const [version, setVersion] = useState(0)

  const [anio, mes] = mesElegido.split('-').map(Number)
  const mesTerminado = mesElegido < mesColombia(0)

  useEffect(() => {
    if (!token || !anio || !mes) return
    let cancelado = false
    setCargando(true)
    obtenerResumenFacturacion(anio, mes, token)
      .then((datos) => {
        if (cancelado) return
        setResumen(datos)
        setMensajeError(null)
      })
      .catch((error) => {
        if (!cancelado) setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la facturación.')
      })
      .finally(() => {
        if (!cancelado) setCargando(false)
      })
    return () => {
      cancelado = true
    }
  }, [anio, mes, token, version])

  const totalConductores = resumen.reduce((suma, fila) => suma + fila.conductoresActivos, 0)

  return (
    <ContenedorPagina>
      <EncabezadoPagina
        titulo="Facturación"
        subtitulo="Conductores que finalizaron al menos una ruta en el mes, por empresa. Es la base del cobro mensual."
        acciones={
          <div className="pagina-facturacion__selector">
            <div className="pagina-facturacion__atajos">
              {[
                { texto: 'Mes en curso', valor: mesColombia(0) },
                { texto: 'Mes anterior', valor: mesColombia(-1) },
              ].map((atajo) => (
                <button
                  key={atajo.valor}
                  type="button"
                  className={`pagina-facturacion__atajo${mesElegido === atajo.valor ? ' pagina-facturacion__atajo--activo' : ''}`}
                  onClick={() => {
                    setMesElegido(atajo.valor)
                    setAbierta(null)
                  }}
                >
                  {atajo.texto}
                </button>
              ))}
            </div>
          <label className="pagina-facturacion__mes">
            Mes
            <input
              type="month"
              value={mesElegido}
              max={mesColombia(0)}
              onChange={(evento) => {
                if (!evento.target.value) return
                setMesElegido(evento.target.value)
                setAbierta(null)
              }}
            />
          </label>
          </div>
        }
      />

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {!mesTerminado && (
        <p className="pagina-facturacion__nota">
          Mes en curso: las cifras se actualizan a medida que los conductores finalizan rutas (usa el botón de actualizar de arriba). Se podrá cerrar
          cuando termine.
        </p>
      )}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : (
        <>
          <p className="pagina-facturacion__total">
            <strong>{totalConductores}</strong> {totalConductores === 1 ? 'conductor' : 'conductores'} a facturar entre todas las empresas.
          </p>
          <TablaDatos columnas={['Empresa', 'Conductores a facturar', 'Rutas finalizadas', 'Pasajeros', 'Estado', '']}>
            {resumen.map((fila) => (
              <Fragment key={fila.empresaId}>
                <tr>
                  <td>
                    {fila.nombreEmpresa}
                    <div className="pagina-facturacion__sub">{fila.conductoresVinculados} conductores vinculados hoy</div>
                  </td>
                  <td className="pagina-facturacion__cifra">{fila.conductoresActivos}</td>
                  <td>{fila.rutasFinalizadas}</td>
                  <td>{fila.pasajerosTransportados}</td>
                  <td>
                    <span className={`pagina-facturacion__estado${fila.cerrado ? ' pagina-facturacion__estado--cerrado' : ''}`}>
                      {fila.cerrado ? 'Cerrado' : 'Abierto'}
                    </span>
                  </td>
                  <td className="pagina-facturacion__accion">
                    <BotonSecundario type="button" onClick={() => setAbierta(abierta === fila.empresaId ? null : fila.empresaId)}>
                      {abierta === fila.empresaId ? 'Cerrar' : 'Ver detalle'}
                    </BotonSecundario>
                  </td>
                </tr>
                {abierta === fila.empresaId && (
                  <tr className="pagina-facturacion__detalle">
                    <td colSpan={6}>
                      <DetalleFacturacionEmpresa
                        empresaId={fila.empresaId}
                        anio={anio}
                        mes={mes}
                        mesTerminado={mesTerminado}
                        alCerrar={() => setVersion((v) => v + 1)}
                      />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </TablaDatos>
        </>
      )}
    </ContenedorPagina>
  )
}
