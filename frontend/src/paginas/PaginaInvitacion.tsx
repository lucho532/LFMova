import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { DetalleInvitacion } from '../modelos/invitacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { aceptarInvitacion, obtenerDetalleInvitacion } from '../servicios/servicioInvitaciones'
import '../estilos/paginas/PaginaInvitacion.css'

/**
 * Destino del enlace de invitación a una empresa (`?token=`). Muestra quién
 * invita y a qué empresa. Si la persona no tiene cuenta, la lleva a crearla
 * desde la invitación; si la tiene, le pide iniciar sesión (volviendo aquí
 * después) y aceptar. Hasta que acepta, la empresa no ve sus datos.
 */
export function PaginaInvitacion() {
  const [parametros] = useSearchParams()
  const tokenInvitacion = parametros.get('token') ?? ''
  const { token } = useAutenticacion()
  const [detalle, setDetalle] = useState<DetalleInvitacion | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(tokenInvitacion ? null : 'El enlace de invitación no es válido.')
  const [aceptando, setAceptando] = useState(false)
  const [aceptada, setAceptada] = useState(false)

  useEffect(() => {
    if (!tokenInvitacion) return
    let cancelado = false
    obtenerDetalleInvitacion(tokenInvitacion)
      .then((d) => {
        if (!cancelado) setDetalle(d)
      })
      .catch((error) => {
        if (!cancelado) setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la invitación.')
      })
    return () => {
      cancelado = true
    }
  }, [tokenInvitacion])

  async function alAceptar() {
    if (!token) return
    setAceptando(true)
    setMensajeError(null)
    try {
      await aceptarInvitacion(tokenInvitacion, token)
      setAceptada(true)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo aceptar la invitación.')
    } finally {
      setAceptando(false)
    }
  }

  const volverAqui = `/invitacion?token=${encodeURIComponent(tokenInvitacion)}`

  return (
    <TarjetaAutenticacion titulo="Invitación">
      {!detalle && !mensajeError && <p>Cargando la invitación…</p>}
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {detalle && (
        <div className="pagina-invitacion">
          <p className="pagina-invitacion__texto">
            <strong>{detalle.nombreInvitador}</strong> te invita a formar parte de <strong>{detalle.nombreEmpresa}</strong> en TransportApp.
          </p>

          {aceptada ? (
            <MensajeAlerta tipo="exito">
              Listo: ya formas parte de {detalle.nombreEmpresa}. Quien te invitó ya puede asignarte tu rol. Cierra sesión y vuelve a entrar para ver
              los cambios.
            </MensajeAlerta>
          ) : detalle.estado === 'ACEPTADA' ? (
            <MensajeAlerta tipo="exito">Esta invitación ya fue aceptada.</MensajeAlerta>
          ) : detalle.estado === 'VENCIDA' ? (
            <MensajeAlerta tipo="error">Esta invitación venció. Pide a quien te invitó que te envíe una nueva.</MensajeAlerta>
          ) : detalle.requiereRegistro ? (
            <>
              <p className="pagina-invitacion__nota">Todavía no tienes cuenta: créala desde aquí y quedarás unido a la empresa de una vez.</p>
              <Link className="pagina-invitacion__boton" to={`/crear-cuenta?invitacion=${encodeURIComponent(tokenInvitacion)}`}>
                Crear mi cuenta
              </Link>
            </>
          ) : token ? (
            <>
              <p className="pagina-invitacion__nota">Al aceptar, la empresa podrá ver tus datos de contacto y asignarte un rol.</p>
              <BotonPrimario type="button" disabled={aceptando} onClick={alAceptar}>
                {aceptando ? 'Aceptando…' : 'Aceptar invitación'}
              </BotonPrimario>
            </>
          ) : (
            <>
              <p className="pagina-invitacion__nota">Inicia sesión con tu cuenta (cédula {detalle.cedula}) para aceptar.</p>
              <Link className="pagina-invitacion__boton" to={`/iniciar-sesion?volverA=${encodeURIComponent(volverAqui)}`}>
                Iniciar sesión para aceptar
              </Link>
            </>
          )}
        </div>
      )}
    </TarjetaAutenticacion>
  )
}
