import { Fragment, useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { EstadoInvitacion, InvitacionEmpresa } from '../modelos/invitacion'
import type { Persona } from '../modelos/persona'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerInvitaciones } from '../servicios/servicioInvitaciones'
import { buscarPersona } from '../servicios/servicioPersonas'
import { BotonSecundario } from './BotonSecundario'
import { MensajeAlerta } from './MensajeAlerta'
import { PanelCambiarRolPersona } from './PanelCambiarRolPersona'
import { TablaDatos } from './TablaDatos'
import '../estilos/componentes/ListaInvitaciones.css'

interface PropiedadesListaInvitaciones {
  empresaId: number
  /** Cambia cuando hay que volver a consultar (por ejemplo, tras enviar una invitación). */
  version: number
  /** Cédulas que ya aparecen en la lista de empleados (ahí se les cambia el rol). */
  cedulasEnLista: Set<string>
  /** Avisa de un cambio de rol exitoso hecho desde esta lista. */
  alCambiarRol: (mensaje: string) => void
}

const ETIQUETAS_ESTADO: Record<EstadoInvitacion, string> = {
  PENDIENTE: 'Pendiente',
  ACEPTADA: 'Aceptada',
  VENCIDA: 'Vencida',
}

function fechaCorta(fechaUtc: string): string {
  return new Date(fechaUtc).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' })
}

/**
 * Invitaciones que envió la empresa y su estado. Una persona que aceptó ya
 * aparece como empleado en la lista principal; si no aparece ahí (porque ya
 * era empleado de otra empresa y no se la mueve), se le puede cambiar el rol
 * desde aquí.
 */
export function ListaInvitaciones({ empresaId, version, cedulasEnLista, alCambiarRol }: PropiedadesListaInvitaciones) {
  const { token } = useAutenticacion()
  const [invitaciones, setInvitaciones] = useState<InvitacionEmpresa[]>([])
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [abierta, setAbierta] = useState<number | null>(null)
  const [persona, setPersona] = useState<Persona | null>(null)
  const [refresco, setRefresco] = useState(0)

  useEffect(() => {
    if (!token) return
    obtenerInvitaciones(empresaId, token)
      .then(setInvitaciones)
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las invitaciones.'))
  }, [empresaId, token, version, refresco])

  async function alAlternar(invitacion: InvitacionEmpresa) {
    if (!token) return
    if (abierta === invitacion.invitacionEmpresaId) {
      setAbierta(null)
      return
    }
    setAbierta(invitacion.invitacionEmpresaId)
    setPersona(null)
    try {
      setPersona(await buscarPersona(invitacion.cedula, token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la cuenta de esta persona.')
      setAbierta(null)
    }
  }

  async function alTerminar(mensaje: string) {
    setAbierta(null)
    setRefresco((r) => r + 1)
    alCambiarRol(mensaje)
  }

  if (invitaciones.length === 0) return null

  return (
    <section className="lista-invitaciones">
      <h2>Invitaciones enviadas</h2>
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      <TablaDatos columnas={['Cédula', 'Persona', 'Estado', 'Enviada', '']}>
        {invitaciones.map((invitacion) => {
          const sePuedeGestionar = invitacion.estado === 'ACEPTADA' && !cedulasEnLista.has(invitacion.cedula)
          return (
            <Fragment key={invitacion.invitacionEmpresaId}>
              <tr>
                <td>{invitacion.cedula}</td>
                <td>{invitacion.nombreCompleto ?? <span className="lista-invitaciones__sin-dato">Se verá cuando acepte</span>}</td>
                <td>
                  <span className={`lista-invitaciones__estado lista-invitaciones__estado--${invitacion.estado.toLowerCase()}`}>
                    {ETIQUETAS_ESTADO[invitacion.estado]}
                  </span>
                  {invitacion.estado === 'PENDIENTE' && <div className="lista-invitaciones__sub">Vence el {fechaCorta(invitacion.fechaExpiracion)}</div>}
                </td>
                <td>{fechaCorta(invitacion.fechaCreacion)}</td>
                <td className="lista-invitaciones__celda-accion">
                  {sePuedeGestionar && (
                    <BotonSecundario type="button" onClick={() => alAlternar(invitacion)}>
                      {abierta === invitacion.invitacionEmpresaId ? 'Cerrar' : 'Cambiar rol'}
                    </BotonSecundario>
                  )}
                </td>
              </tr>
              {abierta === invitacion.invitacionEmpresaId && (
                <tr className="lista-invitaciones__detalle">
                  <td colSpan={5}>
                    {persona ? <PanelCambiarRolPersona persona={persona} empresaId={empresaId} alTerminar={alTerminar} /> : <span>Cargando…</span>}
                  </td>
                </tr>
              )}
            </Fragment>
          )
        })}
      </TablaDatos>
    </section>
  )
}
