import { useState } from 'react'
import type { NumeroInternoUnidad, UnidadOperativa, Vehiculo } from '../modelos/conductor'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  activarUnidadOperativa,
  activarVehiculo,
  actualizarVehiculo,
  crearUnidadOperativa,
  desactivarUnidadOperativa,
  desactivarVehiculo,
} from '../servicios/servicioConductores'
import { BotonSecundario } from './BotonSecundario'
import { CampoNumeroInternoUnidad } from './CampoNumeroInternoUnidad'
import { EtiquetaEstado } from './EtiquetaEstado'
import { FormularioVehiculo } from './FormularioVehiculo'
import { VigenciaDocumento } from './VigenciaDocumento'
import '../estilos/componentes/TarjetaVehiculoConductor.css'

interface PropiedadesTarjetaVehiculoConductor {
  empresaId: number
  conductorId: number
  token: string
  vehiculo: Vehiculo
  /** Unidad operativa del vehículo, o undefined si todavía no tiene. */
  unidad: UnidadOperativa | undefined
  /** Números internos que la empresa le dio a sus unidades. */
  numeros: NumeroInternoUnidad[]
  /** Ejecuta una acción y recarga la ficha; si falla, muestra el error de la pantalla. */
  ejecutar: (accion: () => Promise<unknown>, mensajeFallo: string) => Promise<void>
  /** Recarga la ficha tras guardar desde el formulario. */
  alRecargar: () => Promise<void>
  /** Muestra un aviso de éxito en la pantalla. */
  alAvisar: (mensaje: string) => void
}

/**
 * Todo un vehículo del conductor en una sola tarjeta: sus datos, la vigencia de sus documentos, el número
 * interno que le dio la empresa y el estado de su unidad operativa. El lápiz abre la edición en la misma
 * tarjeta (datos del vehículo y número interno). Solo presenta y delega las acciones en la pantalla.
 */
export function TarjetaVehiculoConductor({ empresaId, conductorId, token, vehiculo, unidad, numeros, ejecutar, alRecargar, alAvisar }: PropiedadesTarjetaVehiculoConductor) {
  const [editando, setEditando] = useState(false)
  const numeroInterno = unidad ? numeros.find((n) => n.unidadOperativaId === unidad.unidadOperativaId)?.numero : undefined

  return (
    <article className="tarjeta-vehiculo-conductor">
      <header>
        <strong className="tarjeta-vehiculo-conductor__placa">{vehiculo.placa}</strong>
        <EtiquetaEstado activo={vehiculo.activo} textoActivo="Vehículo activo" textoInactivo="Vehículo inactivo" />
        <button
          type="button"
          className="tarjeta-vehiculo-conductor__lapiz"
          aria-label={editando ? 'Cerrar edición' : 'Editar vehículo'}
          title={editando ? 'Cerrar edición' : 'Editar vehículo'}
          onClick={() => setEditando((abierto) => !abierto)}
        >
          {editando ? '✕' : '✎'}
        </button>
      </header>

      {editando ? (
        <div className="tarjeta-vehiculo-conductor__edicion">
          <FormularioVehiculo
            idBase={`editar${vehiculo.vehiculoId}`}
            inicial={{
              placa: vehiculo.placa,
              marca: vehiculo.marca,
              modelo: vehiculo.modelo,
              capacidad: vehiculo.capacidad,
              vigenciaSoat: vehiculo.vigenciaSoat ?? '',
              vigenciaTecnomecanica: vehiculo.vigenciaTecnomecanica ?? '',
            }}
            textoBoton="Guardar cambios"
            alGuardar={async (datos) => {
              try {
                await actualizarVehiculo(conductorId, vehiculo.vehiculoId, datos, token)
              } catch (error) {
                throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo guardar el vehículo.')
              }
              setEditando(false)
              alAvisar(`Los datos del vehículo ${datos.placa} se guardaron.`)
              await alRecargar()
            }}
          />
          {unidad && (
            <div className="tarjeta-vehiculo-conductor__numero-edicion">
              <span>N.º interno de la empresa</span>
              <CampoNumeroInternoUnidad empresaId={empresaId} unidadOperativaId={unidad.unidadOperativaId} numeros={numeros} token={token} alGuardar={alRecargar} />
            </div>
          )}
        </div>
      ) : (
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
            <dt>N.º interno de la empresa</dt>
            <dd>{numeroInterno ?? <span className="tarjeta-vehiculo-conductor__vacio">Sin número</span>}</dd>
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
        </dl>
      )}

      <footer>
        {unidad ? (
          <>
            <span>
              Unidad operativa: <EtiquetaEstado activo={unidad.activa} textoActivo="Activa" textoInactivo="Inactiva" />
            </span>
            <BotonSecundario
              onClick={() =>
                ejecutar(
                  () => (unidad.activa ? desactivarUnidadOperativa(conductorId, unidad.unidadOperativaId, token) : activarUnidadOperativa(conductorId, unidad.unidadOperativaId, token)),
                  'No se pudo cambiar el estado de la unidad.',
                )
              }
            >
              {unidad.activa ? 'Desactivar unidad' : 'Activar unidad'}
            </BotonSecundario>
          </>
        ) : (
          vehiculo.activo && (
            <BotonSecundario onClick={() => ejecutar(() => crearUnidadOperativa(conductorId, vehiculo.vehiculoId, token), 'No se pudo crear la unidad operativa.')}>
              Crear unidad operativa
            </BotonSecundario>
          )
        )}
        <BotonSecundario
          onClick={() =>
            ejecutar(
              () => (vehiculo.activo ? desactivarVehiculo(conductorId, vehiculo.vehiculoId, token) : activarVehiculo(conductorId, vehiculo.vehiculoId, token)),
              'No se pudo cambiar el estado del vehículo.',
            )
          }
        >
          {vehiculo.activo ? 'Desactivar vehículo' : 'Activar vehículo'}
        </BotonSecundario>
      </footer>
    </article>
  )
}
