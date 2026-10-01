import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { FilaTarjetasResumen } from '../componentes/FilaTarjetasResumen'
import { GraficaBarras } from '../componentes/GraficaBarras'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaResumen } from '../componentes/TarjetaResumen'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { ResumenEmpresa } from '../modelos/estadisticas'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerResumenEmpresa } from '../servicios/servicioEstadisticas'
import './PaginaInicioCoordinador.css'

/** Formatea una fecha ISO (AAAA-MM-DD) como día/mes sin depender de la zona horaria del navegador. */
function diaMes(fecha: string): string {
  const [, mes, dia] = fecha.split('-')
  return `${dia}/${mes}`
}

/**
 * Inicio del coordinador: histórico de las rutas programadas hasta la fecha,
 * acumulado de pasajeros transportados y otros indicadores de la operación.
 */
export function PaginaInicioCoordinador() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const [resumen, setResumen] = useState<ResumenEmpresa | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !empresaId) return
    obtenerResumenEmpresa(Number(empresaId), token)
      .then(setResumen)
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el resumen.'))
  }, [empresaId, token])

  if (mensajeError) {
    return (
      <ContenedorPagina ancho="amplio">
        <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>
      </ContenedorPagina>
    )
  }

  if (!resumen) {
    return (
      <ContenedorPagina ancho="amplio">
        <p className="contenedor-pagina__estado">Cargando…</p>
      </ContenedorPagina>
    )
  }

  const base = `/empresas/${empresaId}`
  const cumplimiento = resumen.rutasProgramadas === 0 ? 0 : Math.round((resumen.rutasRealizadas / resumen.rutasProgramadas) * 100)

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina titulo="Inicio" subtitulo="Resumen de la operación de tu empresa hasta la fecha." />

      {resumen.rutasProgramadas === 0 && (
        <div className="inicio-coordinador__vacio">
          <strong>Aún no hay rutas programadas.</strong>
          <span>
            Empieza cargando el Excel del día en <Link to={`/empresas/${empresaId}/programacion`}>Programación</Link>.
          </span>
        </div>
      )}

      <FilaTarjetasResumen>
        <TarjetaResumen titulo="Rutas programadas" valor={resumen.rutasProgramadas} detalle="Histórico hasta hoy" destacada a={`${base}/rutas?filtro=programadas`} />
        <TarjetaResumen titulo="Rutas realizadas" valor={resumen.rutasRealizadas} detalle={`${cumplimiento}% de cumplimiento`} a={`${base}/rutas?filtro=realizadas`} />
        <TarjetaResumen titulo="Rutas por realizar" valor={resumen.rutasPorRealizar} a={`${base}/rutas?filtro=por-realizar`} />
        <TarjetaResumen titulo="Pasajeros transportados" valor={resumen.pasajerosTransportados} detalle={`de ${resumen.pasajerosProgramados} programados`} destacada a={`${base}/pasajeros?transportados=1`} />
      </FilaTarjetasResumen>

      <FilaTarjetasResumen>
        <TarjetaResumen titulo="Conductores" valor={resumen.conductores} a={`${base}/conductores`} />
        <TarjetaResumen titulo="Sedes" valor={resumen.sedes} a={`${base}/sedes`} />
        <TarjetaResumen titulo="Rutas canceladas" valor={resumen.rutasCanceladas} a={`${base}/rutas?filtro=canceladas`} />
      </FilaTarjetasResumen>

      <div className="inicio-coordinador__graficas">
        <GraficaBarras
          titulo="Rutas de los últimos días"
          textoVacio="Todavía no hay rutas."
          barras={resumen.ultimosDias.map((d) => ({ etiqueta: diaMes(d.fecha), valor: d.rutas, detalle: `${d.rutas} rutas · ${d.pasajerosTransportados} pasajeros transportados` }))}
        />
        <GraficaBarras
          titulo="Rutas por sede"
          textoVacio="Todavía no hay rutas."
          barras={resumen.porSede.map((s) => ({ etiqueta: s.sede, valor: s.rutas, detalle: `${s.rutas} rutas · ${s.pasajerosTransportados} pasajeros transportados` }))}
        />
      </div>
    </ContenedorPagina>
  )
}
