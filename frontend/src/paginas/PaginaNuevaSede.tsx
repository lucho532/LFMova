import { useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { crearSede } from '../servicios/servicioSedes'
import './PaginaNuevaSede.css'

/**
 * Formulario de alta de una sede para la empresa. Requiere rol COORDINADOR de
 * esa empresa en el backend. Al crear, vuelve al listado de sedes.
 */
export function PaginaNuevaSede() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const navegar = useNavigate()

  const [nombre, setNombre] = useState('')
  const [direccion, setDireccion] = useState('')
  const [ciudad, setCiudad] = useState('')
  const [barrio, setBarrio] = useState('')
  const [latitud, setLatitud] = useState('')
  const [longitud, setLongitud] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [creando, setCreando] = useState(false)

  async function alCrearSede(evento: FormEvent) {
    evento.preventDefault()
    if (!token || !empresaId) return

    setCreando(true)
    setMensajeError(null)

    try {
      await crearSede(
        Number(empresaId),
        {
          nombre,
          direccion,
          ciudad,
          barrio,
          latitud: latitud.trim() === '' ? null : Number(latitud),
          longitud: longitud.trim() === '' ? null : Number(longitud),
        },
        token,
      )
      navegar(`/empresas/${empresaId}/sedes`)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la sede.')
    } finally {
      setCreando(false)
    }
  }

  return (
    <main className="pagina-nueva-sede">
      <header className="pagina-nueva-sede__encabezado">
        <h1>Nueva sede</h1>
      </header>

      <form className="pagina-nueva-sede__formulario" onSubmit={alCrearSede}>
        <div className="pagina-nueva-sede__campos">
          <CampoFormulario id="nombreSede" etiqueta="Nombre" valor={nombre} alCambiar={setNombre} requerido />
          <CampoFormulario id="direccionSede" etiqueta="Dirección" valor={direccion} alCambiar={setDireccion} requerido />
          <CampoFormulario id="ciudadSede" etiqueta="Ciudad" valor={ciudad} alCambiar={setCiudad} requerido />
          <CampoFormulario id="barrioSede" etiqueta="Barrio" valor={barrio} alCambiar={setBarrio} requerido />
          <CampoFormulario id="latitudSede" etiqueta="Latitud (opcional)" tipo="number" valor={latitud} alCambiar={setLatitud} />
          <CampoFormulario id="longitudSede" etiqueta="Longitud (opcional)" tipo="number" valor={longitud} alCambiar={setLongitud} />
        </div>

        {mensajeError && (
          <p className="pagina-nueva-sede__error" role="alert">
            {mensajeError}
          </p>
        )}

        <BotonPrimario disabled={creando}>{creando ? 'Creando…' : 'Crear sede'}</BotonPrimario>
      </form>
    </main>
  )
}
