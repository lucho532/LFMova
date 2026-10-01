import { useRef, useState, type DragEvent } from 'react'
import './ZonaArrastreArchivo.css'

interface PropiedadesZonaArrastreArchivo {
  archivo: File | null
  alElegir: (archivo: File) => void
  deshabilitada?: boolean
}

/** Zona para arrastrar y soltar un archivo Excel (.xlsx), o elegirlo con un clic. */
export function ZonaArrastreArchivo({ archivo, alElegir, deshabilitada }: PropiedadesZonaArrastreArchivo) {
  const entrada = useRef<HTMLInputElement>(null)
  const [encima, setEncima] = useState(false)

  function alSoltar(evento: DragEvent) {
    evento.preventDefault()
    setEncima(false)
    const soltado = evento.dataTransfer.files[0]
    if (soltado && !deshabilitada) alElegir(soltado)
  }

  return (
    <div
      className={`zona-arrastre${encima ? ' zona-arrastre--encima' : ''}${archivo ? ' zona-arrastre--con-archivo' : ''}`}
      onDragOver={(evento) => {
        evento.preventDefault()
        setEncima(true)
      }}
      onDragLeave={() => setEncima(false)}
      onDrop={alSoltar}
      onClick={() => entrada.current?.click()}
      role="button"
      tabIndex={0}
      onKeyDown={(evento) => {
        if (evento.key === 'Enter' || evento.key === ' ') entrada.current?.click()
      }}
    >
      <input
        ref={entrada}
        type="file"
        accept=".xlsx"
        hidden
        onChange={(evento) => {
          const elegido = evento.target.files?.[0]
          if (elegido) alElegir(elegido)
          evento.target.value = ''
        }}
      />
      <span className="zona-arrastre__icono" aria-hidden="true">
        {archivo ? '📄' : '⬆'}
      </span>
      {archivo ? (
        <>
          <strong>{archivo.name}</strong>
          <span>Haz clic o arrastra otro archivo para reemplazarlo</span>
        </>
      ) : (
        <>
          <strong>Arrastra aquí el Excel del día</strong>
          <span>o haz clic para elegirlo (.xlsx)</span>
        </>
      )}
    </div>
  )
}
