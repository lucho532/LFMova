import { useState, type ChangeEvent, type MouseEvent } from 'react'
import '../estilos/componentes/CampoFormulario.css'

interface PropiedadesCampoFormulario {
  id: string
  etiqueta: string
  valor: string
  alCambiar: (valor: string) => void
  tipo?: string
  autoCompletar?: string
  requerido?: boolean
  deshabilitado?: boolean
}

/**
 * Par etiqueta + input con el estilo estándar de los formularios de la
 * aplicación. Por defecto pide al navegador no autocompletar (`off`): sin
 * esto, el navegador puede rellenar campos como "cédula/contraseña del
 * coordinador" con las credenciales guardadas del usuario que inició sesión,
 * por simple coincidencia de forma con un campo de login.
 */
export function CampoFormulario({ id, etiqueta, valor, alCambiar, tipo = 'text', autoCompletar = 'off', requerido, deshabilitado }: PropiedadesCampoFormulario) {
  const [visible, setVisible] = useState(false)
  const esContrasena = tipo === 'password'

  function alCambiarInput(evento: ChangeEvent<HTMLInputElement>) {
    alCambiar(evento.target.value)
  }

  /** En los campos de fecha y hora, un clic en cualquier parte del campo despliega el selector nativo (no solo en su icono). */
  function abrirSelector(evento: MouseEvent<HTMLInputElement>) {
    try {
      evento.currentTarget.showPicker()
    } catch {
      // Navegadores sin showPicker: queda el icono nativo del calendario.
    }
  }

  return (
    <div className="campo-formulario">
      <label htmlFor={id}>{etiqueta}</label>
      <div className={esContrasena ? 'campo-formulario__contrasena' : undefined}>
        <input
          id={id}
          type={esContrasena && visible ? 'text' : tipo}
          value={valor}
          onChange={alCambiarInput}
          onClick={tipo === 'date' || tipo === 'time' ? abrirSelector : undefined}
          autoComplete={autoCompletar}
          required={requerido}
          disabled={deshabilitado}
        />
        {esContrasena && (
          <button
            type="button"
            className="campo-formulario__ojo"
            onClick={() => setVisible((actual) => !actual)}
            aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            aria-pressed={visible}
            title={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            disabled={deshabilitado}
          >
            {visible ? (
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
                <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" />
                <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24" />
                <line x1="1" y1="1" x2="23" y2="23" />
              </svg>
            ) : (
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <circle cx="12" cy="12" r="3" />
              </svg>
            )}
          </button>
        )}
      </div>
    </div>
  )
}
