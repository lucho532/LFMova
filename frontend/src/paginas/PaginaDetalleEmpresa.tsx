import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { EtiquetaEstado } from '../componentes/EtiquetaEstado'
import { FilaTarjetasResumen } from '../componentes/FilaTarjetasResumen'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TablaDatos } from '../componentes/TablaDatos'
import { PanelAsignarCoordinador } from '../componentes/PanelAsignarCoordinador'
import { TarjetaResumen } from '../componentes/TarjetaResumen'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Coordinador, Empresa } from '../modelos/empresa'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  activarEmpresa,
  desactivarCoordinador,
  desactivarEmpresa,
  obtenerCoordinadores,
  obtenerEmpresa,
  reactivarCoordinador,
} from '../servicios/servicioEmpresas'
import { obtenerRolesDelToken } from '../servicios/tokenJwt'
import './PaginaDetalleEmpresa.css'

/**
 * Ficha de una empresa. El administrador de plataforma ve los datos
 * completos de cada coordinador, puede activar/desactivar la empresa y a sus
 * coordinadores, y asignar un coordinador (obligatorio cuando la empresa no
 * tiene ninguno activo). Un coordinador de la empresa ve la ficha y los
 * accesos a su operación.
 */
export function PaginaDetalleEmpresa() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const esAdministrador = token ? obtenerRolesDelToken(token).some((claim) => claim.rol === 'ADMINISTRADOR_PLATAFORMA') : false

  const [empresa, setEmpresa] = useState<Empresa | null>(null)
  const [coordinadores, setCoordinadores] = useState<Coordinador[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [accionEnCurso, setAccionEnCurso] = useState(false)
  const [formularioAbierto, setFormularioAbierto] = useState(false)

  async function cargarDatos() {
    if (!token || !empresaId) return
    try {
      const [empresaCargada, coordinadoresCargados] = await Promise.all([
        obtenerEmpresa(Number(empresaId), token),
        obtenerCoordinadores(Number(empresaId), token),
      ])
      setEmpresa(empresaCargada)
      setCoordinadores(coordinadoresCargados)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la empresa.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarDatos()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, token])

  async function ejecutar(accion: () => Promise<void>, mensajeFallo: string) {
    setAccionEnCurso(true)
    setMensajeError(null)
    setMensajeExito(null)
    try {
      await accion()
      await cargarDatos()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : mensajeFallo)
    } finally {
      setAccionEnCurso(false)
    }
  }

  async function alCoordinadorAsignado(mensaje: string) {
    setFormularioAbierto(false)
    setMensajeError(null)
    await cargarDatos()
    setMensajeExito(mensaje)
  }

  const volver = esAdministrador ? { volverA: '/empresas', textoVolver: '← Volver a empresas' } : {}

  if (cargando) {
    return (
      <ContenedorPagina {...volver}>
        <p>Cargando…</p>
      </ContenedorPagina>
    )
  }

  if (!empresa || !token) {
    return (
      <ContenedorPagina {...volver}>
        <MensajeAlerta tipo="error">{mensajeError ?? 'La empresa no existe.'}</MensajeAlerta>
      </ContenedorPagina>
    )
  }

  const activos = coordinadores.filter((c) => c.activo)
  const sinCoordinadorActivo = activos.length === 0
  const mostrarFormulario = esAdministrador && (sinCoordinadorActivo || formularioAbierto)

  return (
    <ContenedorPagina {...volver}>
      <EncabezadoPagina
        titulo={empresa.nombre}
        subtitulo={`CIF ${empresa.cif} · ${empresa.direccion}`}
        acciones={
          <>
            <EtiquetaEstado activo={empresa.activa} />
            {esAdministrador && (
              <BotonSecundario
                disabled={accionEnCurso}
                onClick={() => ejecutar(() => (empresa.activa ? desactivarEmpresa(empresa.empresaId, token) : activarEmpresa(empresa.empresaId, token)), 'No se pudo cambiar el estado de la empresa.')}
              >
                {empresa.activa ? 'Desactivar empresa' : 'Activar empresa'}
              </BotonSecundario>
            )}
          </>
        }
      />

      <FilaTarjetasResumen>
        <TarjetaResumen titulo="Coordinadores activos" valor={activos.length} destacada={sinCoordinadorActivo} detalle={sinCoordinadorActivo ? 'Asigna uno para operar' : undefined} />
        <TarjetaResumen titulo="Coordinadores en total" valor={coordinadores.length} />
      </FilaTarjetasResumen>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      <div className="pagina-detalle-empresa__titulo-seccion">
        <h2>Coordinadores</h2>
        {esAdministrador && !sinCoordinadorActivo && !formularioAbierto && (
          <BotonSecundario onClick={() => setFormularioAbierto(true)}>+ Agregar coordinador</BotonSecundario>
        )}
      </div>

      {coordinadores.length > 0 && (
        <TablaDatos columnas={['Nombre', 'Cédula', 'Correo', 'Teléfono', 'Estado', ...(esAdministrador ? [''] : [])]}>
          {coordinadores.map((coordinador) => (
            <tr key={coordinador.usuarioRolId}>
              <td>{coordinador.nombreCompleto}</td>
              <td>{coordinador.cedula}</td>
              <td>{coordinador.email ?? '—'}</td>
              <td>{coordinador.telefono || '—'}</td>
              <td>
                <EtiquetaEstado activo={coordinador.activo} textoActivo="Activo" textoInactivo="Inactivo" />
              </td>
              {esAdministrador && (
                <td className="pagina-detalle-empresa__celda-accion">
                  <BotonSecundario
                    disabled={accionEnCurso}
                    onClick={() =>
                      ejecutar(
                        () => (coordinador.activo ? desactivarCoordinador(empresa.empresaId, coordinador.usuarioRolId, token) : reactivarCoordinador(empresa.empresaId, coordinador.usuarioRolId, token)),
                        'No se pudo cambiar el estado del coordinador.',
                      )
                    }
                  >
                    {coordinador.activo ? 'Desactivar' : 'Activar'}
                  </BotonSecundario>
                </td>
              )}
            </tr>
          ))}
        </TablaDatos>
      )}

      {mostrarFormulario && (
        <>
          {sinCoordinadorActivo && <MensajeAlerta tipo="error">Esta empresa no tiene un coordinador activo. Asígnale uno para que pueda operar.</MensajeAlerta>}
          <PanelAsignarCoordinador empresaId={empresa.empresaId} alAsignado={alCoordinadorAsignado} />
        </>
      )}

      {!esAdministrador && (
        <>
          <h2>Operación</h2>
          <ul className="pagina-detalle-empresa__accesos">
            <li><Link to={`/empresas/${empresa.empresaId}/programacion`}>Programación</Link></li>
            <li><Link to={`/empresas/${empresa.empresaId}/programaciones`}>Programaciones</Link></li>
            <li><Link to={`/empresas/${empresa.empresaId}/empleados`}>Empleados</Link></li>
            <li><Link to={`/empresas/${empresa.empresaId}/conductores`}>Conductores</Link></li>
            <li><Link to={`/empresas/${empresa.empresaId}/sedes`}>Sedes</Link></li>
          </ul>
        </>
      )}
    </ContenedorPagina>
  )
}
