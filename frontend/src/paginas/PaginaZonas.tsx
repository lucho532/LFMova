import { useEffect, useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { ListaZonasRegistradas } from '../componentes/ListaZonasRegistradas'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { SeccionBarrerasGeograficas } from '../componentes/SeccionBarrerasGeograficas'
import { SeccionCorredoresViales } from '../componentes/SeccionCorredoresViales'
import { SeccionMacroZonas } from '../componentes/SeccionMacroZonas'
import { SelectorFormulario } from '../componentes/SelectorFormulario'
import { TarjetaFormulario } from '../componentes/TarjetaFormulario'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { CorredorVial } from '../modelos/corredorVial'
import type { MacroZona } from '../modelos/macroZona'
import type { Zona } from '../modelos/zona'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarZona, actualizarZona, crearZona, desactivarZona, eliminarZona, moverBarrioDeZona, obtenerZonas, reordenarZonas, unirZonas } from '../servicios/servicioZonas'

/** Convierte el texto del campo de barrios ("La Enea, Palermo") en la lista que espera el backend. */
function barriosDesdeTexto(texto: string): string[] {
  return texto
    .split(',')
    .map((b) => b.trim())
    .filter((b) => b.length > 0)
}

/**
 * Administra las zonas geográficas de recogida de la empresa (por ejemplo
 * "Zona Norte" con sus barrios). Al repartir una ruta entre unidades, cada
 * zona presente en un mismo horario necesita su propio conductor, porque un
 * solo carro no alcanza a cubrir toda la ciudad a tiempo.
 */
export function PaginaZonas() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const idEmpresa = Number(empresaId)

  const [zonas, setZonas] = useState<Zona[]>([])
  const [macroZonas, setMacroZonas] = useState<MacroZona[]>([])
  const [corredores, setCorredores] = useState<CorredorVial[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [zonaIdEnCurso, setZonaIdEnCurso] = useState<number | null>(null)

  const [formularioAbierto, setFormularioAbierto] = useState(false)
  const [editandoZonaId, setEditandoZonaId] = useState<number | null>(null)
  const [nombre, setNombre] = useState('')
  const [barriosTexto, setBarriosTexto] = useState('')
  const [macroZonaId, setMacroZonaId] = useState('')
  const [corredorVialId, setCorredorVialId] = useState('')
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    if (!token || !empresaId) return
    setCargando(true)
    setMensajeError(null)
    try {
      setZonas(await obtenerZonas(idEmpresa, token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las zonas.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, token])

  function alAbrirNueva() {
    setEditandoZonaId(null)
    setNombre('')
    setBarriosTexto('')
    setMacroZonaId('')
    setCorredorVialId('')
    setFormularioAbierto(true)
  }

  function alEditar(zona: Zona) {
    setEditandoZonaId(zona.zonaId)
    setNombre(zona.nombre)
    setBarriosTexto(zona.barrios.join(', '))
    setMacroZonaId(zona.macroZonaId ? String(zona.macroZonaId) : '')
    setCorredorVialId(zona.corredorVialId ? String(zona.corredorVialId) : '')
    setFormularioAbierto(true)
  }

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    setGuardando(true)
    setMensajeError(null)
    try {
      const datos = {
        nombre,
        barrios: barriosDesdeTexto(barriosTexto),
        macroZonaId: macroZonaId ? Number(macroZonaId) : null,
        corredorVialId: corredorVialId ? Number(corredorVialId) : null,
      }
      if (editandoZonaId) {
        await actualizarZona(idEmpresa, editandoZonaId, datos, token)
      } else {
        await crearZona(idEmpresa, datos, token)
      }
      setFormularioAbierto(false)
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo guardar la zona.')
    } finally {
      setGuardando(false)
    }
  }

  /** Ejecuta una operación sobre una zona ya registrada y recarga la lista, mostrando el error si falla. */
  async function ejecutarSobreZona(zonaId: number, operacion: (token: string) => Promise<void>, mensajeSiFalla: string) {
    if (!token) return
    setZonaIdEnCurso(zonaId)
    setMensajeError(null)
    try {
      await operacion(token)
      // Se refresca sin pasar por "Cargando…" para que la tabla no parpadee al reorganizar varias zonas seguidas.
      setZonas(await obtenerZonas(idEmpresa, token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : mensajeSiFalla)
    } finally {
      setZonaIdEnCurso(null)
    }
  }

  const alCambiarEstado = (zona: Zona) =>
    ejecutarSobreZona(zona.zonaId, (t) => (zona.activa ? desactivarZona(idEmpresa, zona.zonaId, t) : activarZona(idEmpresa, zona.zonaId, t)), 'No se pudo cambiar el estado de la zona.')

  const alMoverBarrio = (origen: Zona, barrio: string, destino: Zona) =>
    ejecutarSobreZona(origen.zonaId, (t) => moverBarrioDeZona(idEmpresa, origen.zonaId, barrio, destino.zonaId, t), 'No se pudo mover el barrio.')

  const alUnir = (origen: Zona, destino: Zona) =>
    ejecutarSobreZona(origen.zonaId, (t) => unirZonas(idEmpresa, origen.zonaId, destino.zonaId, t), 'No se pudieron unir las zonas.')

  /** Muestra el nuevo orden de inmediato y luego lo guarda; si falla, vuelve a cargar el orden real. */
  async function alReordenar(zonaIds: number[]) {
    if (!token) return
    setZonas((actuales) => zonaIds.flatMap((id) => actuales.filter((z) => z.zonaId === id)))
    try {
      await reordenarZonas(idEmpresa, zonaIds, token)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo guardar el orden de las zonas.')
      await cargar()
    }
  }

  const alEliminar = (zona: Zona) => ejecutarSobreZona(zona.zonaId, (t) => eliminarZona(idEmpresa, zona.zonaId, t), 'No se pudo eliminar la zona.')

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina
        titulo="Zonas"
        subtitulo="Agrupa los barrios de la ciudad por zona. Al repartir una ruta, cada zona presente en un mismo horario recibe su propio conductor."
        acciones={<BotonPrimario type="button" onClick={alAbrirNueva}>+ Nueva zona</BotonPrimario>}
      />

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {formularioAbierto && (
        <TarjetaFormulario alEnviar={alEnviar} columnas={2}>
          <CampoFormulario id="nombreZona" etiqueta="Nombre de la zona" valor={nombre} alCambiar={setNombre} requerido />
          <CampoFormulario id="barriosZona" etiqueta="Barrios (separados por coma)" valor={barriosTexto} alCambiar={setBarriosTexto} />
          <SelectorFormulario
            id="macroZonaZona"
            etiqueta="Macrozona (opcional)"
            valor={macroZonaId}
            alCambiar={setMacroZonaId}
            opciones={macroZonas.map((m) => ({ valor: String(m.macroZonaId), texto: m.nombre }))}
            textoVacio="Sin macrozona"
          />
          <SelectorFormulario
            id="corredorVialZona"
            etiqueta="Corredor vial (opcional)"
            valor={corredorVialId}
            alCambiar={setCorredorVialId}
            opciones={corredores.map((c) => ({ valor: String(c.corredorVialId), texto: c.nombre }))}
            textoVacio="Sin corredor: exige su propio conductor"
          />
          <BotonPrimario disabled={guardando}>{guardando ? 'Guardando…' : editandoZonaId ? 'Guardar cambios' : 'Crear zona'}</BotonPrimario>
          <BotonSecundario type="button" onClick={() => setFormularioAbierto(false)}>Cancelar</BotonSecundario>
        </TarjetaFormulario>
      )}

      <h2>Zonas registradas</h2>
      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : zonas.length === 0 ? (
        <p className="contenedor-pagina__estado">Todavía no hay zonas registradas.</p>
      ) : (
        <ListaZonasRegistradas
          zonas={zonas}
          zonaIdEnCurso={zonaIdEnCurso}
          alEditar={alEditar}
          alCambiarEstado={alCambiarEstado}
          alMoverBarrio={alMoverBarrio}
          alUnir={alUnir}
          alReordenar={alReordenar}
          alEliminar={alEliminar}
        />
      )}

      {token && <SeccionMacroZonas empresaId={idEmpresa} token={token} alCambiarMacroZonas={setMacroZonas} />}
      {token && <SeccionCorredoresViales empresaId={idEmpresa} token={token} alCambiarCorredores={setCorredores} />}
      {token && <SeccionBarrerasGeograficas empresaId={idEmpresa} token={token} />}
    </ContenedorPagina>
  )
}
