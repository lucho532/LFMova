import { useCallback, useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { CrearVehiculoDatos, UnidadDeTrabajo } from '../modelos/conductor'
import { ErrorApi } from '../servicios/clienteHttp'
import { actualizarVehiculo } from '../servicios/servicioConductores'
import { obtenerMisUnidades } from '../servicios/servicioConductorPropio'
import { FormularioVehiculo } from './FormularioVehiculo'
import { MensajeAlerta } from './MensajeAlerta'
import './PanelMiVehiculo.css'

/**
 * Datos del vehículo del propio conductor, editables como sus datos
 * personales: placa, marca, modelo, capacidad y vigencias del SOAT y de la
 * técnico-mecánica. Un conductor con varios vehículos ve un formulario por
 * cada uno. El backend valida los datos y que la placa no esté repetida.
 */
export function PanelMiVehiculo() {
  const { token } = useAutenticacion()
  const [unidades, setUnidades] = useState<UnidadDeTrabajo[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  const cargar = useCallback(async () => {
    if (!token) return
    try {
      setUnidades(await obtenerMisUnidades(token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar tus vehículos.')
    } finally {
      setCargando(false)
    }
  }, [token])

  useEffect(() => {
    cargar()
  }, [cargar])

  async function guardar(unidad: UnidadDeTrabajo, datos: CrearVehiculoDatos) {
    if (!token) return
    setMensajeExito(null)
    try {
      await actualizarVehiculo(unidad.vehiculo.conductorId, unidad.vehiculo.vehiculoId, datos, token)
    } catch (error) {
      throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo guardar el vehículo.')
    }
    setMensajeExito(`Los datos del vehículo ${datos.placa} se guardaron.`)
    await cargar()
  }

  if (cargando) return null

  return (
    <section className="panel-mi-vehiculo">
      <h2>{unidades.length > 1 ? 'Mis vehículos' : 'Mi vehículo'}</h2>
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}
      {unidades.length === 0 && !mensajeError && <p className="panel-mi-vehiculo__vacio">Todavía no tienes un vehículo registrado.</p>}
      {unidades.map((unidad) => (
        <div key={unidad.vehiculo.vehiculoId} className="panel-mi-vehiculo__tarjeta">
          <FormularioVehiculo
            key={`${unidad.vehiculo.vehiculoId}-${unidad.vehiculo.placa}-${unidad.vehiculo.capacidad}`}
            idBase={`vehiculo${unidad.vehiculo.vehiculoId}`}
            inicial={{
              placa: unidad.vehiculo.placa,
              marca: unidad.vehiculo.marca,
              modelo: unidad.vehiculo.modelo,
              capacidad: unidad.vehiculo.capacidad,
              vigenciaSoat: unidad.vehiculo.vigenciaSoat ?? '',
              vigenciaTecnomecanica: unidad.vehiculo.vigenciaTecnomecanica ?? '',
            }}
            textoBoton="Guardar datos del vehículo"
            textoEnviando="Guardando…"
            alGuardar={(datos) => guardar(unidad, datos)}
          />
        </div>
      ))}
    </section>
  )
}
