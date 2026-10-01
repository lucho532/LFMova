import { Fragment, useEffect, useMemo, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Empleado } from '../modelos/empleado'
import type { Persona } from '../modelos/persona'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerEmpleados } from '../servicios/servicioEmpleados'
import { buscarPersona } from '../servicios/servicioPersonas'
import { BotonSecundario } from './BotonSecundario'
import { FormularioInvitarPersona } from './FormularioInvitarPersona'
import { ListaInvitaciones } from './ListaInvitaciones'
import { MensajeAlerta } from './MensajeAlerta'
import { PanelCambiarRolPersona } from './PanelCambiarRolPersona'
import { TablaDatos } from './TablaDatos'
import './ListaEmpleados.css'

interface PropiedadesListaEmpleados {
  empresaId: number
}

/**
 * Directorio de empleados de la empresa, con búsqueda por cédula, nombre o
 * correo. Cada fila se puede desplegar para asignarle el rol de coordinador
 * o de conductor. Solo muestra personas que ya son parte de la empresa: a
 * cualquier otra (por ejemplo, alguien que se registró para ser conductor)
 * se la invita por correo, y solo aparece aquí cuando acepta.
 */
export function ListaEmpleados({ empresaId }: PropiedadesListaEmpleados) {
  const { token } = useAutenticacion()
  const [empleados, setEmpleados] = useState<Empleado[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [busqueda, setBusqueda] = useState('')
  const [abierto, setAbierto] = useState<number | null>(null)
  const [persona, setPersona] = useState<Persona | null>(null)
  const [cargandoPersona, setCargandoPersona] = useState(false)
  const [version, setVersion] = useState(0)
  const [invitando, setInvitando] = useState(false)
  const [versionInvitaciones, setVersionInvitaciones] = useState(0)

  useEffect(() => {
    if (!token) return
    setCargando(true)
    obtenerEmpleados(empresaId, token)
      .then(setEmpleados)
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el directorio.'))
      .finally(() => setCargando(false))
  }, [empresaId, token, version])

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase()
    if (!texto) return empleados
    return empleados.filter(
      (e) => e.cedula.toLowerCase().includes(texto) || e.nombreCompleto.toLowerCase().includes(texto) || (e.email ?? '').toLowerCase().includes(texto),
    )
  }, [empleados, busqueda])

  const cedulasEnLista = useMemo(() => new Set(empleados.map((e) => e.cedula)), [empleados])

  async function alAlternar(empleado: Empleado) {
    if (!token) return
    if (abierto === empleado.empleadoId) {
      setAbierto(null)
      return
    }
    setAbierto(empleado.empleadoId)
    setPersona(null)
    setMensajeError(null)
    setMensajeExito(null)
    setCargandoPersona(true)
    try {
      setPersona(await buscarPersona(empleado.cedula, token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la cuenta de esta persona.')
      setAbierto(null)
    } finally {
      setCargandoPersona(false)
    }
  }

  async function alTerminar(mensaje: string) {
    setAbierto(null)
    setPersona(null)
    setMensajeExito(mensaje)
    setVersion((v) => v + 1)
  }

  // Lo escrito en la lupa se aprovecha para rellenar la invitación: una cédula o un correo.
  const textoBusqueda = busqueda.trim()
  const cedulaSugerida = /^\d+$/.test(textoBusqueda) ? textoBusqueda : ''
  const correoSugerido = textoBusqueda.includes('@') ? textoBusqueda : ''

  return (
    <section className="lista-empleados">
      <div className="lista-empleados__filtros">
        <label className="lista-empleados__lupa">
          <span aria-hidden="true">🔍</span>
          <input
            type="search"
            placeholder="Buscar por cédula, nombre o correo"
            aria-label="Buscar empleados por cédula, nombre o correo"
            value={busqueda}
            onChange={(evento) => setBusqueda(evento.target.value)}
          />
        </label>
        <p className="lista-empleados__total">{filtrados.length} empleados</p>
        <BotonSecundario type="button" onClick={() => setInvitando((valor) => !valor)}>
          {invitando ? 'Cerrar invitación' : '✉ Invitar por correo'}
        </BotonSecundario>
      </div>

      {invitando && (
        <FormularioInvitarPersona
          empresaId={empresaId}
          cedulaInicial={cedulaSugerida}
          correoInicial={correoSugerido}
          alInvitar={() => setVersionInvitaciones((v) => v + 1)}
        />
      )}

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : filtrados.length === 0 ? (
        <div className="lista-empleados__sin-resultados">
          <p className="contenedor-pagina__estado">
            No hay empleados de la empresa con este filtro. Si la persona se registró en la app pero todavía no es parte de la empresa,
            invítala por correo: aparecerá aquí cuando acepte.
          </p>
          {!invitando && (
            <BotonSecundario type="button" onClick={() => setInvitando(true)}>
              ✉ Invitar por correo
            </BotonSecundario>
          )}
        </div>
      ) : (
        <TablaDatos columnas={['Empleado', 'Teléfono', 'Barrio', '']}>
          {filtrados.map((empleado) => (
            <Fragment key={empleado.empleadoId}>
              <tr>
                <td>
                  {empleado.nombreCompleto}
                  <div className="lista-empleados__sub">
                    CC {empleado.cedula}
                    {empleado.email && ` · ${empleado.email}`}
                  </div>
                </td>
                <td>{empleado.telefono || '—'}</td>
                <td>{empleado.barrio || '—'}</td>
                <td className="lista-empleados__celda-accion">
                  <BotonSecundario type="button" onClick={() => alAlternar(empleado)}>
                    {abierto === empleado.empleadoId ? 'Cerrar' : 'Cambiar rol'}
                  </BotonSecundario>
                </td>
              </tr>
              {abierto === empleado.empleadoId && (
                <tr className="lista-empleados__detalle">
                  <td colSpan={4}>
                    {cargandoPersona || !persona ? <span>Cargando…</span> : <PanelCambiarRolPersona persona={persona} empresaId={empresaId} alTerminar={alTerminar} />}
                  </td>
                </tr>
              )}
            </Fragment>
          ))}
        </TablaDatos>
      )}

      <ListaInvitaciones
        empresaId={empresaId}
        version={versionInvitaciones}
        cedulasEnLista={cedulasEnLista}
        alCambiarRol={(mensaje) => {
          setMensajeExito(mensaje)
          setVersion((v) => v + 1)
        }}
      />
    </section>
  )
}
