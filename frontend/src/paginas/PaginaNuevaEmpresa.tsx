import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { crearEmpresa } from '../servicios/servicioEmpresas'
import './PaginaNuevaEmpresa.css'

/**
 * Formulario de alta de una empresa junto con su primer coordinador (usuario
 * y contraseña, que el administrador le entrega luego). Requiere rol
 * ADMINISTRADOR_PLATAFORMA en el backend. Al crear, vuelve al listado de
 * empresas.
 */
export function PaginaNuevaEmpresa() {
  const { token } = useAutenticacion()
  const navegar = useNavigate()

  const [nombre, setNombre] = useState('')
  const [cif, setCif] = useState('')
  const [direccion, setDireccion] = useState('')
  const [cedulaCoordinador, setCedulaCoordinador] = useState('')
  const [nombreCoordinador, setNombreCoordinador] = useState('')
  const [telefonoCoordinador, setTelefonoCoordinador] = useState('')
  const [correoCoordinador, setCorreoCoordinador] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [creando, setCreando] = useState(false)

  async function alCrearEmpresa(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return

    setCreando(true)
    setMensajeError(null)

    try {
      await crearEmpresa({ nombre, cif, direccion, cedulaCoordinador, nombreCoordinador, telefonoCoordinador, correoCoordinador }, token)
      navegar('/empresas')
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la empresa.')
    } finally {
      setCreando(false)
    }
  }

  return (
    <main className="pagina-nueva-empresa">
      <header className="pagina-nueva-empresa__encabezado">
        <h1>Nueva empresa</h1>
        <p className="pagina-nueva-empresa__subtitulo">
          Se crea la empresa junto con su primer coordinador. Le llegará un correo con un enlace para que establezca su propia contraseña.
        </p>
      </header>

      <form className="pagina-nueva-empresa__formulario" onSubmit={alCrearEmpresa}>
        <div className="pagina-nueva-empresa__campos">
          <CampoFormulario id="nombreEmpresa" etiqueta="Nombre de la empresa" valor={nombre} alCambiar={setNombre} requerido />
          <CampoFormulario id="cifEmpresa" etiqueta="CIF" valor={cif} alCambiar={setCif} requerido />
          <CampoFormulario id="direccionEmpresa" etiqueta="Dirección" valor={direccion} alCambiar={setDireccion} requerido />
          <CampoFormulario id="cedulaCoordinador" etiqueta="Cédula del coordinador" valor={cedulaCoordinador} alCambiar={setCedulaCoordinador} requerido />
          <CampoFormulario id="nombreCoordinador" etiqueta="Nombre completo del coordinador" valor={nombreCoordinador} alCambiar={setNombreCoordinador} requerido />
          <CampoFormulario id="telefonoCoordinador" etiqueta="Teléfono del coordinador" valor={telefonoCoordinador} alCambiar={setTelefonoCoordinador} requerido />
          <CampoFormulario id="correoCoordinador" etiqueta="Correo del coordinador" tipo="email" valor={correoCoordinador} alCambiar={setCorreoCoordinador} requerido />
        </div>

        {mensajeError && (
          <p className="pagina-nueva-empresa__error" role="alert">
            {mensajeError}
          </p>
        )}

        <BotonPrimario disabled={creando}>{creando ? 'Creando…' : 'Crear empresa'}</BotonPrimario>
      </form>
    </main>
  )
}
