import './SelectorFormulario.css'

export interface OpcionSelector {
  valor: string
  texto: string
  /** Si se indica, colorea la opción: true = disponible (verde), false = ya ocupada (rosado). Sin indicar, se muestra sin color. */
  disponible?: boolean
}

interface PropiedadesSelectorFormulario {
  id: string
  etiqueta: string
  valor: string
  opciones: OpcionSelector[]
  alCambiar: (valor: string) => void
  requerido?: boolean
  textoVacio?: string
}

/** Par etiqueta + lista desplegable con el estilo estándar de los formularios. */
export function SelectorFormulario({ id, etiqueta, valor, opciones, alCambiar, requerido, textoVacio = 'Selecciona…' }: PropiedadesSelectorFormulario) {
  return (
    <div className="selector-formulario">
      <label htmlFor={id}>{etiqueta}</label>
      <select id={id} value={valor} onChange={(evento) => alCambiar(evento.target.value)} required={requerido}>
        <option value="">{textoVacio}</option>
        {opciones.map((opcion) => (
          <option
            key={opcion.valor}
            value={opcion.valor}
            className={
              opcion.disponible === true
                ? 'selector-formulario__opcion--disponible'
                : opcion.disponible === false
                  ? 'selector-formulario__opcion--ocupada'
                  : undefined
            }
          >
            {opcion.texto}
          </option>
        ))}
      </select>
    </div>
  )
}
