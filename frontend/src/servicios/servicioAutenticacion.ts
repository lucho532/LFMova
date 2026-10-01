import type {
  IniciarSesionDatos,
  RegistrarUsuarioDatos,
  RespuestaAutenticacion,
  RestablecerContrasenaDatos,
  ResultadoRegistro,
} from '../modelos/autenticacion'
import { solicitarApi } from './clienteHttp'

/** Consume POST /api/autenticacion/iniciar-sesion (cédula o correo y contraseña). */
export function iniciarSesion(datos: IniciarSesionDatos): Promise<RespuestaAutenticacion> {
  return solicitarApi<RespuestaAutenticacion>('/api/autenticacion/iniciar-sesion', {
    metodo: 'POST',
    cuerpo: datos,
  })
}

/**
 * Consume POST /api/autenticacion/registrarse. Crea una cuenta con rol EMPLEADO y envía el correo de
 * confirmación, salvo que se registre desde una invitación con el mismo correo (ya queda confirmada).
 */
export function registrarse(datos: RegistrarUsuarioDatos): Promise<ResultadoRegistro> {
  return solicitarApi<ResultadoRegistro>('/api/autenticacion/registrarse', { metodo: 'POST', cuerpo: datos })
}

/** Consume POST /api/autenticacion/confirmar-correo con el token recibido por correo. */
export function confirmarCorreo(token: string): Promise<void> {
  return solicitarApi<void>('/api/autenticacion/confirmar-correo', { metodo: 'POST', cuerpo: { token } })
}

/** Consume POST /api/autenticacion/solicitar-recuperacion. Siempre responde sin error, exista o no la cuenta. */
export function solicitarRecuperacion(identificador: string): Promise<void> {
  return solicitarApi<void>('/api/autenticacion/solicitar-recuperacion', { metodo: 'POST', cuerpo: { identificador } })
}

/** Consume POST /api/autenticacion/restablecer-contrasena (recuperación o contraseña inicial de una cuenta invitada). */
export function restablecerContrasena(datos: RestablecerContrasenaDatos): Promise<void> {
  return solicitarApi<void>('/api/autenticacion/restablecer-contrasena', { metodo: 'POST', cuerpo: datos })
}
