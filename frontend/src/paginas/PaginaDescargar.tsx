import { Link } from 'react-router-dom'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import '../estilos/paginas/PaginaDescargar.css'

/** Enlace fijo a la última APK publicada (no cambia entre versiones). */
const URL_APK = 'https://github.com/lucho532/LFMova/releases/latest/download/lfmova.apk'

/** Familia del dispositivo desde el que se abre la página, para destacar la opción que le sirve. */
function tipoDeDispositivo(): 'android' | 'iphone' | 'otro' {
  const agente = navigator.userAgent
  if (/android/i.test(agente)) return 'android'
  if (/iphone|ipad|ipod/i.test(agente)) return 'iphone'
  return 'otro'
}

/**
 * Página pública a la que lleva el código QR de descarga: ofrece la APK de
 * Android con los pasos para instalarla y, para iPhone o computador, usar la
 * versión web. No requiere sesión ni consulta la API.
 */
export function PaginaDescargar() {
  const dispositivo = tipoDeDispositivo()

  return (
    <TarjetaAutenticacion titulo="Descarga LFMova" subtitulo="La app para conductores, empleados y coordinadores.">
      <div className="pagina-descargar">
        {dispositivo !== 'iphone' && (
          <section>
            <h2>📱 Android</h2>
            {dispositivo === 'otro' && <p className="pagina-descargar__nota">Abre esta página desde tu teléfono Android para instalar la app.</p>}
            <a className="pagina-descargar__boton" href={URL_APK}>
              ⬇ Descargar la app
            </a>
            <ol className="pagina-descargar__pasos">
              <li>Pulsa <strong>Descargar la app</strong> y espera a que termine la descarga.</li>
              <li>Abre el archivo <strong>lfmova.apk</strong> desde el aviso de descarga o desde la carpeta Descargas.</li>
              <li>
                Si el teléfono dice que no puede instalar apps de este origen, pulsa <strong>Ajustes</strong>, activa <strong>Permitir de esta fuente</strong> y vuelve atrás.
              </li>
              <li>
                Pulsa <strong>Instalar</strong>. Si aparece un aviso de Play Protect, elige <strong>Instalar de todas formas</strong>: sale porque la app no viene de la tienda.
              </li>
              <li>Abre LFMova, crea tu cuenta con tu cédula y confirma tu correo.</li>
            </ol>
            <p className="pagina-descargar__nota">¿Ya la tienes instalada? Descárgala de nuevo para actualizarla: no pierdes tu sesión.</p>
          </section>
        )}

        <section>
          <h2>{dispositivo === 'iphone' ? '🍎 iPhone' : '💻 iPhone o computador'}</h2>
          <p>
            No hace falta instalar nada: LFMova funciona desde el navegador. {dispositivo === 'iphone' && 'En Safari puedes dejarla en tu pantalla de inicio con Compartir → "Añadir a pantalla de inicio". '}
            {dispositivo === 'otro' && 'En Chrome o Edge puedes instalarla como aplicación desde el icono de la barra de direcciones. '}
          </p>
          <Link className="pagina-descargar__boton pagina-descargar__boton--secundario" to="/iniciar-sesion">
            Usar la versión web
          </Link>
        </section>
      </div>
    </TarjetaAutenticacion>
  )
}
