import type { UnidadDeTrabajo } from '../modelos/conductor'
import { EtiquetaEstado } from './EtiquetaEstado'
import { VigenciaDocumento } from './VigenciaDocumento'
import './TarjetaUnidadTrabajo.css'

/**
 * Ficha de solo lectura de la unidad de trabajo de un conductor: su vehículo
 * (placa, marca, modelo, capacidad) y la vigencia de sus documentos.
 */
export function TarjetaUnidadTrabajo({ unidad }: { unidad: UnidadDeTrabajo }) {
  const { vehiculo } = unidad

  return (
    <article className="tarjeta-unidad-trabajo">
      <header>
        <strong className="tarjeta-unidad-trabajo__placa">{vehiculo.placa}</strong>
        <EtiquetaEstado activo={unidad.activa} textoActivo="Unidad activa" textoInactivo="Unidad inactiva" />
      </header>
      <dl>
        <div>
          <dt>Marca</dt>
          <dd>{vehiculo.marca}</dd>
        </div>
        <div>
          <dt>Modelo</dt>
          <dd>{vehiculo.modelo}</dd>
        </div>
        <div>
          <dt>Capacidad</dt>
          <dd>{vehiculo.capacidad} pasajeros</dd>
        </div>
        <div>
          <dt>SOAT</dt>
          <dd>
            <VigenciaDocumento fecha={vehiculo.vigenciaSoat} />
          </dd>
        </div>
        <div>
          <dt>Técnico-mecánica</dt>
          <dd>
            <VigenciaDocumento fecha={vehiculo.vigenciaTecnomecanica} />
          </dd>
        </div>
        <div>
          <dt>Vehículo</dt>
          <dd>{vehiculo.activo ? 'Activo' : 'Inactivo'}</dd>
        </div>
      </dl>
    </article>
  )
}
