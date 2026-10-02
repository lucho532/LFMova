import { useEffect, useState, type FormEvent } from 'react'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { EnlaceBoton } from '../componentes/EnlaceBoton'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { PanelMiVehiculo } from '../componentes/PanelMiVehiculo'
import { PreferenciaNavegacion } from '../componentes/PreferenciaNavegacion'
import { TarjetaFormulario } from '../componentes/TarjetaFormulario'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Cuenta } from '../modelos/cuenta'
import { ErrorApi } from '../servicios/clienteHttp'
import { actualizarCuenta, cambiarContrasena, obtenerCuenta } from '../servicios/servicioCuenta'
import { obtenerRolesDelToken, rutaInicialSegunRoles } from '../servicios/tokenJwt'

/**
 * Datos de la propia cuenta: la cédula y el correo se muestran pero no se
 * editan (identifican la cuenta); el nombre y el teléfono sí, y se puede
 * cambiar la contraseña. Disponible para cualquier rol.
 */
export function PaginaMiCuenta() {
  const { token, actualizarNombre } = useAutenticacion()
  const [cuenta, setCuenta] = useState<Cuenta | null>(null)
  const [nombre, setNombre] = useState('')
  const [telefono, setTelefono] = useState('')
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [errorDatos, setErrorDatos] = useState<string | null>(null)
  const [exitoDatos, setExitoDatos] = useState<string | null>(null)
  const [errorContrasena, setErrorContrasena] = useState<string | null>(null)
  const [exitoContrasena, setExitoContrasena] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState(false)
  const roles = token ? obtenerRolesDelToken(token) : []
  const esConductor = roles.some((claim) => claim.rol === 'CONDUCTOR')
  // Esta pantalla no está en el menú de ningún rol: sin este enlace no habría cómo volver.
  const rutaDeInicio = rutaInicialSegunRoles(roles)

  useEffect(() => {
    if (!token) return
    obtenerCuenta(token)
      .then((c) => {
        setCuenta(c)
        setNombre(c.nombreCompleto)
        setTelefono(c.telefono)
      })
      .catch((error) => setErrorDatos(error instanceof ErrorApi ? error.message : 'No se pudo cargar tu cuenta.'))
  }, [token])

  async function alGuardarDatos(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    setOcupado(true)
    setErrorDatos(null)
    setExitoDatos(null)
    try {
      const actualizada = await actualizarCuenta({ nombreCompleto: nombre, telefono }, token)
      setCuenta(actualizada)
      actualizarNombre(actualizada.nombreCompleto)
      setExitoDatos('Tus datos se guardaron.')
    } catch (error) {
      setErrorDatos(error instanceof ErrorApi ? error.message : 'No se pudieron guardar los datos.')
    } finally {
      setOcupado(false)
    }
  }

  async function alCambiarContrasena(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    setOcupado(true)
    setErrorContrasena(null)
    setExitoContrasena(null)
    try {
      await cambiarContrasena({ contrasenaActual: actual, nuevaContrasena: nueva, confirmacionContrasena: confirmacion }, token)
      setActual('')
      setNueva('')
      setConfirmacion('')
      setExitoContrasena('Tu contraseña se actualizó.')
    } catch (error) {
      setErrorContrasena(error instanceof ErrorApi ? error.message : 'No se pudo cambiar la contraseña.')
    } finally {
      setOcupado(false)
    }
  }

  return (
    <ContenedorPagina ancho="estrecho">
      <EncabezadoPagina
        titulo="Mi cuenta"
        subtitulo={esConductor ? "Tus datos personales, los de tu vehículo y tu contraseña." : "Tus datos personales y tu contraseña."}
        acciones={<EnlaceBoton a={rutaDeInicio}>Ir al inicio</EnlaceBoton>}
      />

      {cuenta && (
        <TarjetaFormulario alEnviar={alGuardarDatos}>
          <CampoFormulario id="cuentaCedula" etiqueta="Cédula (no se puede modificar)" valor={cuenta.cedula} alCambiar={() => {}} deshabilitado />
          <CampoFormulario id="cuentaCorreo" etiqueta="Correo electrónico" valor={cuenta.email ?? 'Sin correo registrado'} alCambiar={() => {}} deshabilitado />
          <CampoFormulario id="cuentaNombre" etiqueta="Nombre completo" valor={nombre} alCambiar={setNombre} requerido />
          <CampoFormulario id="cuentaTelefono" etiqueta="Teléfono" tipo="tel" valor={telefono} alCambiar={setTelefono} requerido />
          {errorDatos && <MensajeAlerta tipo="error">{errorDatos}</MensajeAlerta>}
          {exitoDatos && <MensajeAlerta tipo="exito">{exitoDatos}</MensajeAlerta>}
          <BotonPrimario disabled={ocupado}>Guardar datos</BotonPrimario>
        </TarjetaFormulario>
      )}
      {!cuenta && errorDatos && <MensajeAlerta tipo="error">{errorDatos}</MensajeAlerta>}

      {esConductor && <PanelMiVehiculo />}
      {esConductor && <PreferenciaNavegacion />}

      <h2>Cambiar contraseña</h2>
      <TarjetaFormulario alEnviar={alCambiarContrasena}>
        <CampoFormulario id="contrasenaActual" etiqueta="Contraseña actual" tipo="password" valor={actual} alCambiar={setActual} autoCompletar="current-password" requerido />
        <CampoFormulario id="contrasenaNueva" etiqueta="Nueva contraseña" tipo="password" valor={nueva} alCambiar={setNueva} autoCompletar="new-password" requerido />
        <CampoFormulario id="contrasenaConfirmacion" etiqueta="Confirmar nueva contraseña" tipo="password" valor={confirmacion} alCambiar={setConfirmacion} autoCompletar="new-password" requerido />
        {errorContrasena && <MensajeAlerta tipo="error">{errorContrasena}</MensajeAlerta>}
        {exitoContrasena && <MensajeAlerta tipo="exito">{exitoContrasena}</MensajeAlerta>}
        <BotonPrimario disabled={ocupado}>Cambiar contraseña</BotonPrimario>
      </TarjetaFormulario>
    </ContenedorPagina>
  )
}
