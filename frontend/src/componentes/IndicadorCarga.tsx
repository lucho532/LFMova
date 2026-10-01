import './IndicadorCarga.css'

/** Ruedita de carga: se muestra junto a un texto ("Cargando…") mientras una acción está en curso. No recibe datos ni tiene lógica propia. */
export function IndicadorCarga() {
  return <span className="indicador-carga" aria-hidden="true" />
}
