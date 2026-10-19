import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { FormularioVehiculo } from '../componentes/FormularioVehiculo'
import { TarjetaVehiculoConductor } from '../componentes/TarjetaVehiculoConductor'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Conductor, NumeroInternoUnidad, UnidadOperativa, Vehiculo } from '../modelos/conductor'
import '../estilos/paginas/PaginaDetalleConductor.css'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  crearVehiculo,
  obtenerConductor,
  obtenerNumerosInternos,
  obtenerUnidadesOperativas,
  obtenerVehiculos,
} from '../servicios/servicioConductores'

/**
 * Ficha de un conductor: sus datos, sus vehículos y las unidades operativas
 * (conductor + vehículo) que un coordinador puede asignar a servicios. Cada
 * vehículo es una tarjeta con todos sus datos, que se edita con el lápiz (por
 * ejemplo, para renovar el SOAT o la técnico-mecánica); agregar otro vehículo
 * es un botón que muestra el formulario solo mientras se necesita.
 */
export function PaginaDetalleConductor() {
  const { empresaId, conductorId } = useParams<{ empresaId: string; conductorId: string }>()
  const { token } = useAutenticacion()
  const idConductor = Number(conductorId)

  const [conductor, setConductor] = useState<Conductor | null>(null)
  const [vehiculos, setVehiculos] = useState<Vehiculo[]>([])
  const [unidades, setUnidades] = useState<UnidadOperativa[]>([])
  const [numerosInternos, setNumerosInternos] = useState<NumeroInternoUnidad[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [agregando, setAgregando] = useState(false)

  async function cargar() {
    if (!token) return
    try {
      const [c, v, u, n] = await Promise.all([
        obtenerConductor(Number(empresaId), idConductor, token),
        obtenerVehiculos(idConductor, token),
        obtenerUnidadesOperativas(idConductor, token),
        obtenerNumerosInternos(Number(empresaId), token),
      ])
      setConductor(c)
      setVehiculos(v)
      setUnidades(u)
      setNumerosInternos(n)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el conductor.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, conductorId, token])

  async function ejecutar(accion: () => Promise<unknown>, mensajeFallo: string) {
    setMensajeError(null)
    try {
      await accion()
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : mensajeFallo)
    }
  }

  const volver = { volverA: `/empresas/${empresaId}/conductores`, textoVolver: '← Volver a conductores' }

  if (cargando) {
    return (
      <ContenedorPagina {...volver}>
        <p>Cargando…</p>
      </ContenedorPagina>
    )
  }

  if (!conductor || !token) {
    return (
      <ContenedorPagina {...volver}>
        <MensajeAlerta tipo="error">{mensajeError ?? 'El conductor no existe.'}</MensajeAlerta>
      </ContenedorPagina>
    )
  }


  return (
    <ContenedorPagina {...volver}>
      <EncabezadoPagina titulo={conductor.nombreCompleto} subtitulo={`Cédula ${conductor.cedula} · Tel. ${conductor.telefono}`} />

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      <div className="detalle-conductor__titulo-seccion">
        <h2>Vehículos</h2>
        <BotonSecundario onClick={() => setAgregando((visible) => !visible)}>{agregando ? 'Cancelar' : '+ Agregar vehículo'}</BotonSecundario>
      </div>

      {agregando && (
        <div className="detalle-conductor__formulario-alta">
          <FormularioVehiculo
            textoBoton="Registrar vehículo"
            alGuardar={async (datos) => {
              try {
                await crearVehiculo(idConductor, datos, token)
              } catch (error) {
                throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo registrar el vehículo.')
              }
              setAgregando(false)
              await cargar()
            }}
          />
        </div>
      )}

      {vehiculos.length === 0 ? (
        <p className="contenedor-pagina__estado">Este conductor todavía no tiene vehículos.</p>
      ) : (
        <div className="detalle-conductor__vehiculos">
          {vehiculos.map((vehiculo) => (
            <TarjetaVehiculoConductor
              key={vehiculo.vehiculoId}
              empresaId={Number(empresaId)}
              conductorId={idConductor}
              token={token!}
              vehiculo={vehiculo}
              unidad={unidades.find((u) => u.vehiculoId === vehiculo.vehiculoId)}
              numeros={numerosInternos}
              ejecutar={ejecutar}
              alRecargar={cargar}
              alAvisar={setMensajeExito}
            />
          ))}
        </div>
      )}
    </ContenedorPagina>
  )
}
