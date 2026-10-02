import { Fragment, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { FormularioVehiculo } from '../componentes/FormularioVehiculo'
import { VigenciaDocumento } from '../componentes/VigenciaDocumento'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { EtiquetaEstado } from '../componentes/EtiquetaEstado'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TablaDatos } from '../componentes/TablaDatos'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Conductor, UnidadOperativa, Vehiculo } from '../modelos/conductor'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  activarUnidadOperativa,
  activarVehiculo,
  actualizarVehiculo,
  crearUnidadOperativa,
  crearVehiculo,
  desactivarUnidadOperativa,
  desactivarVehiculo,
  obtenerConductor,
  obtenerUnidadesOperativas,
  obtenerVehiculos,
} from '../servicios/servicioConductores'

/**
 * Ficha de un conductor: sus datos, sus vehículos y las unidades operativas
 * (conductor + vehículo) que un coordinador puede asignar a servicios. Cada
 * vehículo se puede editar, por ejemplo para renovar el SOAT o la
 * técnico-mecánica.
 */
export function PaginaDetalleConductor() {
  const { empresaId, conductorId } = useParams<{ empresaId: string; conductorId: string }>()
  const { token } = useAutenticacion()
  const idConductor = Number(conductorId)

  const [conductor, setConductor] = useState<Conductor | null>(null)
  const [vehiculos, setVehiculos] = useState<Vehiculo[]>([])
  const [unidades, setUnidades] = useState<UnidadOperativa[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [editando, setEditando] = useState<number | null>(null)

  async function cargar() {
    if (!token) return
    try {
      const [c, v, u] = await Promise.all([
        obtenerConductor(Number(empresaId), idConductor, token),
        obtenerVehiculos(idConductor, token),
        obtenerUnidadesOperativas(idConductor, token),
      ])
      setConductor(c)
      setVehiculos(v)
      setUnidades(u)
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

  const placaDe = (vehiculoId: number) => vehiculos.find((v) => v.vehiculoId === vehiculoId)?.placa ?? `#${vehiculoId}`
  const vehiculosSinUnidad = vehiculos.filter((v) => v.activo && !unidades.some((u) => u.vehiculoId === v.vehiculoId))

  return (
    <ContenedorPagina {...volver}>
      <EncabezadoPagina titulo={conductor.nombreCompleto} subtitulo={`Cédula ${conductor.cedula} · Tel. ${conductor.telefono}`} />

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      <h2>Vehículos</h2>
      {vehiculos.length === 0 ? (
        <p className="contenedor-pagina__estado">Este conductor todavía no tiene vehículos.</p>
      ) : (
        <TablaDatos columnas={['Placa', 'Marca', 'Modelo', 'Capacidad', 'SOAT', 'Técnico-mecánica', 'Estado', '']}>
          {vehiculos.map((vehiculo) => (
            <Fragment key={vehiculo.vehiculoId}>
            <tr>
              <td>{vehiculo.placa}</td>
              <td>{vehiculo.marca}</td>
              <td>{vehiculo.modelo}</td>
              <td>{vehiculo.capacidad}</td>
              <td>
                <VigenciaDocumento fecha={vehiculo.vigenciaSoat} />
              </td>
              <td>
                <VigenciaDocumento fecha={vehiculo.vigenciaTecnomecanica} />
              </td>
              <td>
                <EtiquetaEstado activo={vehiculo.activo} textoActivo="Activo" textoInactivo="Inactivo" />
              </td>
              <td style={{ whiteSpace: 'nowrap' }}>
                <BotonSecundario onClick={() => setEditando(editando === vehiculo.vehiculoId ? null : vehiculo.vehiculoId)}>
                  {editando === vehiculo.vehiculoId ? 'Cerrar' : 'Editar'}
                </BotonSecundario>{' '}
                <BotonSecundario
                  onClick={() =>
                    ejecutar(
                      () =>
                        vehiculo.activo
                          ? desactivarVehiculo(idConductor, vehiculo.vehiculoId, token)
                          : activarVehiculo(idConductor, vehiculo.vehiculoId, token),
                      'No se pudo cambiar el estado del vehículo.',
                    )
                  }
                >
                  {vehiculo.activo ? 'Desactivar' : 'Activar'}
                </BotonSecundario>
              </td>
            </tr>
            {editando === vehiculo.vehiculoId && (
              <tr>
                <td colSpan={8}>
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
                        await actualizarVehiculo(idConductor, vehiculo.vehiculoId, datos, token)
                      } catch (error) {
                        throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo guardar el vehículo.')
                      }
                      setEditando(null)
                      setMensajeExito(`Los datos del vehículo ${datos.placa} se guardaron.`)
                      await cargar()
                    }}
                  />
                </td>
              </tr>
            )}
            </Fragment>
          ))}
        </TablaDatos>
      )}

      <h2>Agregar vehículo</h2>
      <div style={{ padding: '20px 24px', border: '1px solid var(--border)', borderRadius: 14, background: 'var(--bg)', boxShadow: 'var(--shadow)' }}>
        <FormularioVehiculo
          textoBoton="Registrar vehículo"
          alGuardar={async (datos) => {
            try {
              await crearVehiculo(idConductor, datos, token)
            } catch (error) {
              throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo registrar el vehículo.')
            }
            await cargar()
          }}
        />
      </div>

      <h2>Unidades operativas</h2>
      <p className="contenedor-pagina__estado">Una unidad operativa es la combinación conductor + vehículo que se asigna a los servicios.</p>
      {unidades.length > 0 && (
        <TablaDatos columnas={['Vehículo', 'Estado', '']}>
          {unidades.map((unidad) => (
            <tr key={unidad.unidadOperativaId}>
              <td>{placaDe(unidad.vehiculoId)}</td>
              <td>
                <EtiquetaEstado activo={unidad.activa} textoActivo="Activa" textoInactivo="Inactiva" />
              </td>
              <td>
                <BotonSecundario
                  onClick={() =>
                    ejecutar(
                      () =>
                        unidad.activa
                          ? desactivarUnidadOperativa(idConductor, unidad.unidadOperativaId, token)
                          : activarUnidadOperativa(idConductor, unidad.unidadOperativaId, token),
                      'No se pudo cambiar el estado de la unidad.',
                    )
                  }
                >
                  {unidad.activa ? 'Desactivar' : 'Activar'}
                </BotonSecundario>
              </td>
            </tr>
          ))}
        </TablaDatos>
      )}
      {vehiculosSinUnidad.length > 0 && (
        <div style={{ marginTop: 14, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          {vehiculosSinUnidad.map((vehiculo) => (
            <BotonSecundario
              key={vehiculo.vehiculoId}
              onClick={() => ejecutar(() => crearUnidadOperativa(idConductor, vehiculo.vehiculoId, token), 'No se pudo crear la unidad operativa.')}
            >
              Crear unidad con {vehiculo.placa}
            </BotonSecundario>
          ))}
        </div>
      )}
    </ContenedorPagina>
  )
}
