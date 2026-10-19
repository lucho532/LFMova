import '../estilos/componentes/VigenciaDocumento.css'

const DIAS_AVISO = 30

/**
 * Muestra la fecha de vigencia de un documento del vehículo (SOAT,
 * técnico-mecánica) con un aviso visual cuando está vencido o próximo a
 * vencer. Es solo informativo: la plataforma no bloquea nada por esta fecha.
 */
export function VigenciaDocumento({ fecha }: { fecha: string | null }) {
  if (!fecha) return <span className="vigencia-documento">—</span>

  const hoy = new Date()
  hoy.setHours(0, 0, 0, 0)
  const dias = Math.round((new Date(`${fecha}T00:00:00`).getTime() - hoy.getTime()) / 86_400_000)

  if (dias < 0) {
    return (
      <span className="vigencia-documento vigencia-documento--vencido">
        {fecha} · × Vencido
      </span>
    )
  }

  if (dias <= DIAS_AVISO) {
    return (
      <span className="vigencia-documento vigencia-documento--proximo">
        {fecha} · ! Vence en {dias} d
      </span>
    )
  }

  return <span className="vigencia-documento">{fecha}</span>
}
