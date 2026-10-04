import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import '../estilos/paginas/PaginaProbar.css'

/** Grupo de Google abierto: quien se une queda autorizado como probador en Google Play. */
const URL_GRUPO = 'https://groups.google.com/g/lfmova-probadores'
/** Vínculo de participación de la prueba cerrada en Google Play. */
const URL_PARTICIPAR = 'https://play.google.com/apps/testing/com.lfmova.app'
/** Ficha de la app en Play Store (solo la ve quien ya aceptó participar). */
const URL_TIENDA = 'https://play.google.com/store/apps/details?id=com.lfmova.app'

/**
 * Página pública a la que lleva el código QR de la prueba cerrada de Google
 * Play: explica los tres pasos para ser probador (unirse al grupo, aceptar
 * la prueba e instalar) con un botón para cada uno. No requiere sesión ni
 * consulta la API; la inscripción la gestiona Google.
 */
export function PaginaProbar() {
  return (
    <TarjetaAutenticacion titulo="Prueba LFMova" subtitulo="Ayúdanos a probar la app antes de su lanzamiento en Google Play. Son tres pasos y toman un minuto.">
      <div className="pagina-probar">
        <p className="pagina-probar__nota">
          Hazlo desde tu teléfono Android, con la misma cuenta de Google que usas en Play Store, y en este orden.
        </p>

        <ol className="pagina-probar__pasos">
          <li>
            <h2>Únete al grupo de probadores</h2>
            <p>Se abre Grupos de Google: pulsa «Unirse al grupo» y confirma.</p>
            <a className="pagina-probar__boton" href={URL_GRUPO} target="_blank" rel="noreferrer">
              1 · Unirme al grupo
            </a>
          </li>
          <li>
            <h2>Acepta ser probador</h2>
            <p>Se abre Google Play: pulsa «Convertirme en verificador».</p>
            <a className="pagina-probar__boton" href={URL_PARTICIPAR} target="_blank" rel="noreferrer">
              2 · Aceptar la prueba
            </a>
          </li>
          <li>
            <h2>Instala la app</h2>
            <p>Si acabas de aceptar, puede tardar unos minutos en aparecer el botón «Instalar».</p>
            <a className="pagina-probar__boton" href={URL_TIENDA} target="_blank" rel="noreferrer">
              3 · Instalar LFMova
            </a>
          </li>
        </ol>

        <p className="pagina-probar__nota">
          Por favor, deja la app instalada y sigue en la prueba al menos 14 días: Google lo exige para poder publicarla. ¿Algo no funciona? Escríbenos a{' '}
          <a href="mailto:luisrak25@gmail.com">luisrak25@gmail.com</a>.
        </p>
      </div>
    </TarjetaAutenticacion>
  )
}
