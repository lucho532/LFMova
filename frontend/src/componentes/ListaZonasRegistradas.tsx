import { useState } from 'react'
import { useArrastrePuntero } from '../hooks/useArrastrePuntero'
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
  /** Recibe los identificadores de todas las zonas en su nuevo orden. */
  alReordenar: (zonaIds: number[]) => void
  alEliminar: (zona: Zona) => void
}

/** Operación que espera confirmación antes de ejecutarse, porque borra una zona. */
type Pendiente = { tipo: 'unir'; origen: Zona; destino: Zona } | { tipo: 'eliminar'; zona: Zona }

/**
 * Zonas registradas, como tarjetas que se pueden reorganizar: un barrio se
 * mueve a otra zona, una zona se une con otra y las tarjetas se cambian de
 * lugar. Con el ratón todo se hace arrastrando; en pantallas táctiles, mover
 * y unir se hacen tocando el barrio (o "Unir con…") y luego la zona de
 * destino. No llama a la API: avisa a la página con sus `al*` y solo pide
 * confirmación para lo que elimina una zona.
 */
export function ListaZonasRegistradas({ zonas, zonaIdEnCurso, alEditar, alCambiarEstado, alMoverBarrio, alUnir, alReordenar, alEliminar }: PropiedadesListaZonasRegistradas) {
  const [seleccion, setSeleccion] = useState<SeleccionZona | null>(null)
  const [pendiente, setPendiente] = useState<Pendiente | null>(null)
  const { arrastrado, destinoId, etiquetaRef, alPresionar } = useArrastrePuntero<SeleccionZona>('data-zona-id', (dato, zonaId) => aplicar(dato, zonaId))

  /** Ejecuta lo que corresponde a lo movido (`dato`) al caer sobre la zona de destino. */
  function aplicar(dato: SeleccionZona, zonaDestinoId: number) {
    const origen = zonas.find((z) => z.zonaId === dato.zonaId)
    const destino = zonas.find((z) => z.zonaId === zonaDestinoId)
    setSeleccion(null)
    if (!origen || !destino || origen.zonaId === destino.zonaId) return

    if (dato.barrio) alMoverBarrio(origen, dato.barrio, destino)
    else if (dato.ordenar) {
      // La tarjeta arrastrada pasa a ocupar el lugar de la tarjeta sobre la que se soltó.
      const ids = zonas.map((z) => z.zonaId).filter((id) => id !== origen.zonaId)
      ids.splice(zonas.indexOf(destino), 0, origen.zonaId)
      alReordenar(ids)
    } else setPendiente({ tipo: 'unir', origen, destino })
  }

  /** Tocar lo mismo otra vez lo deselecciona. */
  function alSeleccionar(nueva: SeleccionZona) {
    const esLaMisma = seleccion?.zonaId === nueva.zonaId && seleccion.barrio === nueva.barrio
    setSeleccion(esLaMisma ? null : nueva)
  }

  function alConfirmar() {
    if (pendiente?.tipo === 'unir') alUnir(pendiente.origen, pendiente.destino)
    if (pendiente?.tipo === 'eliminar') alEliminar(pendiente.zona)
    setPendiente(null)
  }

  const zonaElegida = zonas.find((z) => z.zonaId === seleccion?.zonaId)
  const zonaArrastrada = zonas.find((z) => z.zonaId === arrastrado?.zonaId)

  return (
    <>
      <p className="lista-zonas__ayuda">
        Arrastra un barrio a otra zona para moverlo, el nombre de una zona sobre otra para unirlas, o la tarjeta desde cualquier parte vacía para cambiarla de lugar. También puedes
        tocar un barrio y luego elegir la zona de destino. Una zona que se queda sin barrios se elimina sola.
      </p>

      {seleccion && !arrastrado && zonaElegida && (
        <div className="lista-zonas__aviso" role="status">
          <span>
            {seleccion.barrio ? <>Moviendo el barrio <strong>{seleccion.barrio}</strong>: elige la zona de destino.</> : <>Uniendo la zona <strong>{zonaElegida.nombre}</strong>: elige con cuál.</>}
          </span>
          <BotonSecundario onClick={() => setSeleccion(null)}>Cancelar</BotonSecundario>
        </div>
      )}

      <ul className={`lista-zonas${arrastrado ? ' lista-zonas--arrastrando' : ''}`}>
        {zonas.map((zona) => (
          <TarjetaZona
            key={zona.zonaId}
            zona={zona}
            seleccion={arrastrado ? null : seleccion}
            arrastrado={arrastrado}
            esDestinoDelArrastre={arrastrado !== null && destinoId === zona.zonaId && arrastrado.zonaId !== zona.zonaId}
            ocupada={zonaIdEnCurso === zona.zonaId}
            alSeleccionar={alSeleccionar}
            alPresionar={alPresionar}
            alElegirDestino={(destino) => seleccion && aplicar(seleccion, destino.zonaId)}
            alEditar={alEditar}
            alCambiarEstado={alCambiarEstado}
            alEliminar={(z) => setPendiente({ tipo: 'eliminar', zona: z })}
          />
        ))}
      </ul>

      {arrastrado && zonaArrastrada && (
        <div ref={etiquetaRef} className="lista-zonas__etiqueta">
          {arrastrado.barrio ?? (arrastrado.ordenar ? `Mover: ${zonaArrastrada.nombre}` : `Unir: ${zonaArrastrada.nombre}`)}
        </div>
      )}

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
