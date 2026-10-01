import { useState, type DragEvent } from 'react'
import type { Zona } from '../modelos/zona'
import { BotonSecundario } from './BotonSecundario'
import { ModalConfirmacion } from './ModalConfirmacion'
import { TablaDatos } from './TablaDatos'
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

/** Lo que se está arrastrando: un barrio suelto o una zona entera (sin `barrio`). */
interface Arrastre {
  zonaId: number
  barrio?: string
}

/** Operación que espera confirmación antes de ejecutarse, porque borra una zona. */
type Pendiente = { tipo: 'unir'; origen: Zona; destino: Zona } | { tipo: 'eliminar'; zona: Zona }

/**
 * Tabla de las zonas registradas que permite reorganizarlas con el ratón:
 * arrastrar un barrio a otra zona lo mueve, y arrastrar una zona (por su
 * nombre) sobre otra las une. No llama a la API: avisa a la página con sus
 * `al*` y solo pide confirmación para lo que elimina una zona.
 */
export function ListaZonasRegistradas({ zonas, zonaIdEnCurso, alEditar, alCambiarEstado, alMoverBarrio, alUnir, alEliminar }: PropiedadesListaZonasRegistradas) {
  const [arrastre, setArrastre] = useState<Arrastre | null>(null)
  const [zonaDestinoId, setZonaDestinoId] = useState<number | null>(null)
  const [pendiente, setPendiente] = useState<Pendiente | null>(null)

  function alEmpezar(evento: DragEvent, nuevo: Arrastre) {
    evento.stopPropagation()
    evento.dataTransfer.effectAllowed = 'move'
    // Firefox no inicia el arrastre si no se guarda algún dato.
    evento.dataTransfer.setData('text/plain', nuevo.barrio ?? String(nuevo.zonaId))
    setArrastre(nuevo)
  }

  function alTerminar() {
    setArrastre(null)
    setZonaDestinoId(null)
  }

  function alPasarSobre(evento: DragEvent, zona: Zona) {
    if (!arrastre || arrastre.zonaId === zona.zonaId) return
    evento.preventDefault()
    setZonaDestinoId(zona.zonaId)
  }

  function alSalir(evento: DragEvent<HTMLTableRowElement>) {
    // Pasar de una celda a otra de la misma fila también dispara "dragleave": solo cuenta salir de la fila.
    if (!evento.currentTarget.contains(evento.relatedTarget as Node | null)) setZonaDestinoId(null)
  }

  function alSoltar(evento: DragEvent, destino: Zona) {
    evento.preventDefault()
    const origen = zonas.find((z) => z.zonaId === arrastre?.zonaId)
    if (arrastre && origen && origen.zonaId !== destino.zonaId) {
      if (arrastre.barrio) alMoverBarrio(origen, arrastre.barrio, destino)
      else setPendiente({ tipo: 'unir', origen, destino })
    }
    alTerminar()
  }

  function alConfirmar() {
    if (pendiente?.tipo === 'unir') alUnir(pendiente.origen, pendiente.destino)
    if (pendiente?.tipo === 'eliminar') alEliminar(pendiente.zona)
    setPendiente(null)
  }

  return (
    <>
      <p className="lista-zonas__ayuda">
        Arrastra un barrio a otra zona para moverlo, o arrastra una zona por su nombre sobre otra para unirlas. Una zona que se queda sin barrios se elimina sola.
      </p>
      <TablaDatos columnas={['Nombre', 'Macrozona', 'Corredor vial', 'Barrios', 'Estado', '']}>
        {zonas.map((zona) => (
          <tr
            key={zona.zonaId}
            className={zonaDestinoId === zona.zonaId ? 'lista-zonas__fila--destino' : undefined}
            onDragOver={(evento) => alPasarSobre(evento, zona)}
            onDragLeave={alSalir}
            onDrop={(evento) => alSoltar(evento, zona)}
          >
            <td>
              <span className="lista-zonas__asa" draggable onDragStart={(evento) => alEmpezar(evento, { zonaId: zona.zonaId })} onDragEnd={alTerminar} title="Arrastra sobre otra zona para unirlas">
                <span aria-hidden="true">⠿</span> {zona.nombre}
              </span>
            </td>
            <td>{zona.macroZonaNombre ?? '—'}</td>
            <td>{zona.corredorVialNombre ?? '—'}</td>
            <td>
              {zona.barrios.length === 0 ? '—' : (
                <ul className="lista-zonas__barrios">
                  {zona.barrios.map((barrio) => (
                    <li key={barrio} draggable onDragStart={(evento) => alEmpezar(evento, { zonaId: zona.zonaId, barrio })} onDragEnd={alTerminar} title="Arrastra a otra zona para moverlo">
                      {barrio}
                    </li>
                  ))}
                </ul>
              )}
            </td>
            <td>{zona.activa ? 'Activa' : 'Inactiva'}</td>
            <td className="lista-zonas__acciones">
              <BotonSecundario onClick={() => alEditar(zona)}>Editar</BotonSecundario>{' '}
              <BotonSecundario onClick={() => alCambiarEstado(zona)} disabled={zonaIdEnCurso === zona.zonaId}>
                {zona.activa ? 'Desactivar' : 'Activar'}
              </BotonSecundario>{' '}
              <BotonSecundario onClick={() => setPendiente({ tipo: 'eliminar', zona })} disabled={zonaIdEnCurso === zona.zonaId}>Eliminar</BotonSecundario>
            </td>
          </tr>
        ))}
      </TablaDatos>

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
