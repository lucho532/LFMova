import { useState, type DragEvent } from 'react'
import { useDesplazamientoAlArrastrar } from '../hooks/useDesplazamientoAlArrastrar'
import type { Zona } from '../modelos/zona'
import { BotonSecundario } from './BotonSecundario'
import { ModalConfirmacion } from './ModalConfirmacion'
import { TarjetaZona, type SeleccionZona } from './TarjetaZona'
import '../estilos/componentes/ListaZonasRegistradas.css'

interface PropiedadesListaZonasRegistradas {
  zonas: Zona[]
  /** Zona con una operación en curso: sus botones quedan deshabilitados. */
  zonaIdEnCurso: number | null
  alEditar: (zona: Zona) => void
  alCambiarEstado: (zona: Zona) => void
  alMoverBarrio: (origen: Zona, barrio: string, destino: Zona) => void
  alUnir: (origen: Zona, destino: Zona) => void
  alEliminar: (zona: Zona) => void
}

/** Operación que espera confirmación antes de ejecutarse, porque borra una zona. */
type Pendiente = { tipo: 'unir'; origen: Zona; destino: Zona } | { tipo: 'eliminar'; zona: Zona }

/**
 * Zonas registradas, como tarjetas que se pueden reorganizar: un barrio se
 * mueve a otra zona y una zona entera se une con otra, ya sea arrastrando con
 * el ratón o, en pantallas táctiles, tocando el barrio (o "Unir con…") y luego
 * la zona de destino. No llama a la API: avisa a la página con sus `al*` y
 * solo pide confirmación para lo que elimina una zona.
 */
export function ListaZonasRegistradas({ zonas, zonaIdEnCurso, alEditar, alCambiarEstado, alMoverBarrio, alUnir, alEliminar }: PropiedadesListaZonasRegistradas) {
  const [seleccion, setSeleccion] = useState<SeleccionZona | null>(null)
  const [arrastrando, setArrastrando] = useState(false)
  const [zonaDestinoId, setZonaDestinoId] = useState<number | null>(null)
  const [pendiente, setPendiente] = useState<Pendiente | null>(null)
  useDesplazamientoAlArrastrar(arrastrando)

  function limpiar() {
    setSeleccion(null)
    setArrastrando(false)
    setZonaDestinoId(null)
  }

  /** Tocar lo mismo otra vez lo deselecciona. */
  function alSeleccionar(nueva: SeleccionZona) {
    const esLaMisma = seleccion?.zonaId === nueva.zonaId && seleccion.barrio === nueva.barrio
    setSeleccion(esLaMisma ? null : nueva)
  }

  function alEmpezarArrastre(evento: DragEvent, nueva: SeleccionZona) {
    evento.dataTransfer.effectAllowed = 'move'
    // Firefox no inicia el arrastre si no se guarda algún dato.
    evento.dataTransfer.setData('text/plain', nueva.barrio ?? String(nueva.zonaId))
    setSeleccion(nueva)
    setArrastrando(true)
  }

  function alPasarSobre(evento: DragEvent, zona: Zona) {
    if (!arrastrando || seleccion?.zonaId === zona.zonaId) return
    evento.preventDefault()
    setZonaDestinoId(zona.zonaId)
  }

  function alSalir(evento: DragEvent<HTMLElement>) {
    // Pasar a un elemento interior de la misma tarjeta también dispara "dragleave": solo cuenta salir de ella.
    if (!evento.currentTarget.contains(evento.relatedTarget as Node | null)) setZonaDestinoId(null)
  }

  function alSoltar(destino: Zona) {
    const origen = zonas.find((z) => z.zonaId === seleccion?.zonaId)
    if (seleccion && origen && origen.zonaId !== destino.zonaId) {
      if (seleccion.barrio) alMoverBarrio(origen, seleccion.barrio, destino)
      else setPendiente({ tipo: 'unir', origen, destino })
    }
    limpiar()
  }

  function alConfirmar() {
    if (pendiente?.tipo === 'unir') alUnir(pendiente.origen, pendiente.destino)
    if (pendiente?.tipo === 'eliminar') alEliminar(pendiente.zona)
    setPendiente(null)
  }

  const zonaElegida = zonas.find((z) => z.zonaId === seleccion?.zonaId)

  return (
    <>
      <p className="lista-zonas__ayuda">
        Arrastra un barrio a otra zona para moverlo, o arrastra una zona por su nombre sobre otra para unirlas. También puedes tocar un barrio y luego elegir la zona de destino. Una zona
        que se queda sin barrios se elimina sola.
      </p>

      {seleccion && !arrastrando && zonaElegida && (
        <div className="lista-zonas__aviso" role="status">
          <span>
            {seleccion.barrio ? <>Moviendo el barrio <strong>{seleccion.barrio}</strong>: elige la zona de destino.</> : <>Uniendo la zona <strong>{zonaElegida.nombre}</strong>: elige con cuál.</>}
          </span>
          <BotonSecundario onClick={limpiar}>Cancelar</BotonSecundario>
        </div>
      )}

      <ul className="lista-zonas">
        {zonas.map((zona) => (
          <TarjetaZona
            key={zona.zonaId}
            zona={zona}
            seleccion={arrastrando ? null : seleccion}
            esDestinoDelArrastre={zonaDestinoId === zona.zonaId}
            ocupada={zonaIdEnCurso === zona.zonaId}
            alSeleccionar={alSeleccionar}
            alEmpezarArrastre={alEmpezarArrastre}
            alTerminarArrastre={limpiar}
            alPasarSobre={alPasarSobre}
            alSalir={alSalir}
            alSoltar={alSoltar}
            alEditar={alEditar}
            alCambiarEstado={alCambiarEstado}
            alEliminar={(z) => setPendiente({ tipo: 'eliminar', zona: z })}
          />
        ))}
      </ul>

      <ModalConfirmacion
        abierto={pendiente !== null}
        titulo={pendiente?.tipo === 'unir' ? 'Unir zonas' : 'Eliminar zona'}
        mensaje={
          pendiente?.tipo === 'unir'
            ? `Todos los barrios de "${pendiente.origen.nombre}" pasarán a "${pendiente.destino.nombre}" y la zona "${pendiente.origen.nombre}" se eliminará.`
            : pendiente?.tipo === 'eliminar'
              ? `Se eliminará la zona "${pendiente.zona.nombre}" y sus barrios quedarán sin zona.`
              : ''
        }
        textoConfirmar={pendiente?.tipo === 'unir' ? 'Unir' : 'Eliminar'}
        alConfirmar={alConfirmar}
        alCancelar={() => setPendiente(null)}
      />
    </>
  )
}
