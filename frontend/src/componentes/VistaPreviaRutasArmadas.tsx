import { NOMBRES_TIPO_SERVICIO } from '../modelos/enumeraciones'
import type { RutaArmadaPrevia } from '../modelos/importacion'
import '../estilos/componentes/VistaPreviaRutasArmadas.css'

interface PropiedadesVistaPreviaRutasArmadas {
  rutas: RutaArmadaPrevia[]
}

const ROTULOS_SITUACION: Record<string, string> = {
  NUEVO: 'Nuevo',
  CUENTA_REGISTRADA: 'Ya registrado',
  EXISTENTE: 'Ya es de la empresa',
  CAMBIA_DE_EMPRESA: 'Cambia de empresa',
}

/** Rutas de un Excel con rutas armadas tal como se crearían: hora, placa y conductor de cada una y, al abrirla, sus pasajeros en el orden del archivo. Solo muestra; no cambia nada. */
export function VistaPreviaRutasArmadas({ rutas }: PropiedadesVistaPreviaRutasArmadas) {
  return (
    <div className="vista-rutas-armadas">
      {rutas.map((ruta, indice) => (
        <details key={`${ruta.referencia}-${indice}`} className="vista-rutas-armadas__ruta">
          <summary>
            <span className="vista-rutas-armadas__titulo">Ruta {ruta.referencia}</span>
            <span>
              {NOMBRES_TIPO_SERVICIO[ruta.tipo]} {ruta.hora.slice(0, 5)}
              {ruta.fecha ? ` · ${ruta.fecha}` : ''}
            </span>
            <span className={ruta.conductor ? 'vista-rutas-armadas__conductor' : 'vista-rutas-armadas__sin-conductor'}>
              {ruta.conductor
                ? `${ruta.conductor} · ${ruta.placa}${ruta.numeroInterno && ruta.numeroInterno !== ruta.placa ? ` · N.º interno ${ruta.numeroInterno}` : ''}`
                : ruta.placa
                  ? `«${ruta.placa}»: sin conductor`
                  : 'Sin placa'}
            </span>
            <span className="vista-rutas-armadas__cantidad">
              {ruta.pasajeros.length} pasajero{ruta.pasajeros.length === 1 ? '' : 's'}
              {ruta.capacidad ? ` de ${ruta.capacidad}` : ''}
            </span>
          </summary>
          <ol className="vista-rutas-armadas__pasajeros">
            {ruta.pasajeros.map((pasajero, posicion) => (
              <li key={`${pasajero.cedula}-${posicion}`}>
                <strong>{pasajero.nombreCompleto}</strong>
                <span>
                  {pasajero.cedula} · {pasajero.barrio} · {pasajero.direccion}
                </span>
                <em>{ROTULOS_SITUACION[pasajero.situacion] ?? pasajero.situacion}</em>
              </li>
            ))}
          </ol>
        </details>
      ))}
    </div>
  )
}
