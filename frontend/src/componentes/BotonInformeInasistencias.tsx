import { useState } from 'react'
import { BotonSecundario } from './BotonSecundario'
import { VistaPreviaInasistencias } from './VistaPreviaInasistencias'

interface PropiedadesBotonInformeInasistencias {
  empresaId: number
  token: string
}

/**
 * Botón del coordinador que abre la vista previa de los pasajeros que no asistieron (por defecto, los de
 * hoy), desde donde se descarga el Excel. Solo abre la vista: no consulta ni descarga por sí solo.
 */
export function BotonInformeInasistencias({ empresaId, token }: PropiedadesBotonInformeInasistencias) {
  const [abierto, setAbierto] = useState(false)

  return (
    <>
      <BotonSecundario onClick={() => setAbierto(true)}>📋 Informe de inasistencias</BotonSecundario>
      <VistaPreviaInasistencias abierto={abierto} empresaId={empresaId} token={token} alCerrar={() => setAbierto(false)} />
    </>
  )
}
