import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Persona } from '../modelos/persona'
import { ErrorApi } from '../servicios/clienteHttp'
import { buscarPersona } from '../servicios/servicioPersonas'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/BuscadorPersona.css'

interface PropiedadesBuscadorPersona {
  /** Botones de acción que se ofrecen una vez confirmada la identidad de la persona encontrada. */
  acciones: (persona: Persona, limpiar: () => void) => ReactNode
  /** Contenido alternativo cuando no existe ninguna cuenta con esa cédula (por ejemplo, un formulario de invitación). */
  cuandoNoExiste?: (cedula: string, limpiar: () => void) => ReactNode
  /** Texto de ayuda bajo el título. */
  ayuda?: string
  /** Si cambia a un texto no vacío, se busca esa cédula de inmediato (por ejemplo, desde un filtro externo). */
  cedulaInicial?: string
}

/**
 * Busca a una persona por cédula (al pulsar Enter o "Buscar") y muestra sus
 * datos completos para que quien la busca confirme que es la persona
 * correcta. Buscar nunca asigna nada: las acciones solo aparecen después, en
 * la tarjeta del resultado.
 */
export function BuscadorPersona({ acciones, cuandoNoExiste, ayuda, cedulaInicial }: PropiedadesBuscadorPersona) {
  const { token } = useAutenticacion()
  const [cedula, setCedula] = useState('')
  const [buscando, setBuscando] = useState(false)
  const [persona, setPersona] = useState<Persona | null>(null)
  const [noExiste, setNoExiste] = useState<string | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  function limpiar() {
    setCedula('')
    setPersona(null)
    setNoExiste(null)
    setMensajeError(null)
  }

  async function buscar(texto: string) {
    if (!token || !texto.trim()) return
    setBuscando(true)
    setPersona(null)
    setNoExiste(null)
    setMensajeError(null)
    try {
      setPersona(await buscarPersona(texto, token))
    } catch (error) {
      if (error instanceof ErrorApi && error.status === 404) {
        setNoExiste(texto.trim())
      } else {
        setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo realizar la búsqueda.')
      }
    } finally {
      setBuscando(false)
    }
  }

  function alBuscar(evento: FormEvent) {
    evento.preventDefault()
    buscar(cedula)
  }

  useEffect(() => {
    if (cedulaInicial?.trim()) {
      setCedula(cedulaInicial)
      buscar(cedulaInicial)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cedulaInicial])

  return (
    <section className="buscador-persona">
      {ayuda && <p className="buscador-persona__ayuda">{ayuda}</p>}

      <form className="buscador-persona__formulario" onSubmit={alBuscar}>
        <CampoFormulario id="cedulaBusqueda" etiqueta="Cédula" valor={cedula} alCambiar={setCedula} requerido />
        <BotonSecundario type="submit" disabled={buscando}>
          {buscando ? 'Buscando…' : 'Buscar'}
        </BotonSecundario>
      </form>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {persona && (
        <div className="buscador-persona__resultado">
          <h3>{persona.nombreCompleto}</h3>
          <dl>
            <div>
              <dt>Cédula</dt>
              <dd>{persona.cedula}</dd>
            </div>
            <div>
              <dt>Correo</dt>
              <dd>{persona.email ?? '—'}</dd>
            </div>
            <div>
              <dt>Teléfono</dt>
              <dd>{persona.telefono || '—'}</dd>
            </div>
          </dl>
          <p className="buscador-persona__roles">
            {persona.esConductor && <span>Ya es conductor</span>}
            {persona.esCoordinador && <span>Ya es coordinador</span>}
            {!persona.esConductor && !persona.esCoordinador && <span>Cuenta sin roles de gestión</span>}
          </p>
          <ul className="buscador-persona__empresas">
            {persona.empresaCoordinada && <li>Coordinador de <strong>{persona.empresaCoordinada.nombre}</strong></li>}
            {persona.empresasConductor.map((e) => (
              <li key={e.empresaId}>
                Conductor en <strong>{e.nombre}</strong>
                {persona.placas.length > 0 && ` · vehículos: ${persona.placas.join(', ')}`}
              </li>
            ))}
            {persona.empresaEmpleado && <li>Empleado de <strong>{persona.empresaEmpleado.nombre}</strong></li>}
            {!persona.empresaCoordinada && persona.empresasConductor.length === 0 && !persona.empresaEmpleado && <li>Sin empresa asignada</li>}
          </ul>
          <p className="buscador-persona__confirmar">Verifica que es la persona que buscas antes de continuar.</p>
          <div className="buscador-persona__acciones">
            {acciones(persona, limpiar)}
            <BotonSecundario onClick={limpiar}>Buscar otra</BotonSecundario>
          </div>
        </div>
      )}

      {noExiste && (
        <div className="buscador-persona__resultado">
          <p>No encontramos a ninguna persona con la cédula <strong>{noExiste}</strong>.</p>
          {cuandoNoExiste ? cuandoNoExiste(noExiste, limpiar) : <p>La persona debe registrarse primero en la plataforma.</p>}
        </div>
      )}
    </section>
  )
}
