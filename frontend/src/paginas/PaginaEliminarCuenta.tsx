import { Link } from 'react-router-dom'
import { DocumentoLegal } from '../componentes/DocumentoLegal'

const CORREO = 'luisrak25@gmail.com'

/**
 * Página pública que explica cómo eliminar la cuenta de LFMova y qué se
 * borra. Es la dirección que piden las tiendas de aplicaciones para la
 * eliminación de cuentas; el borrado en sí se hace desde «Mi cuenta».
 */
export function PaginaEliminarCuenta() {
  return (
    <DocumentoLegal titulo="Eliminar mi cuenta de LFMova" actualizado="5 de octubre de 2026">
      <p>Puedes eliminar tu cuenta de LFMova y los datos asociados en cualquier momento, sin costo, de cualquiera de estas dos formas.</p>

      <h2>Desde la aplicación (inmediato)</h2>
      <ol>
        <li>
          Abre LFMova (la app de Android o <Link to="/iniciar-sesion">la versión web</Link>) e inicia sesión.
        </li>
        <li>Pulsa la rueda de configuración, arriba a la derecha, y entra en «Mi cuenta».</li>
        <li>Baja hasta «Eliminar mi cuenta», escribe tu contraseña y confirma.</li>
      </ol>
      <p>La cuenta se borra en ese mismo momento y la sesión se cierra.</p>

      <h2>Por correo</h2>
      <p>
        Si no puedes entrar a tu cuenta, escribe a <a href={`mailto:${CORREO}?subject=Eliminar%20mi%20cuenta%20de%20LFMova`}>{CORREO}</a> desde el
        correo con el que te registraste, indicando tu nombre y tu número de cédula. La eliminamos en un máximo de quince (15) días hábiles.
      </p>

      <h2>Qué se elimina</h2>
      <ul>
        <li>Tu cuenta: nombre, cédula, correo, teléfono y contraseña.</li>
        <li>Tu dirección y tus ubicaciones de recogida guardadas.</li>
        <li>Las rutas en las que fuiste pasajero, con sus mensajes, incidencias y fotografías.</li>
        <li>Si eres conductor: tu ficha de conductor y tus vehículos.</li>
        <li>Tus notificaciones e invitaciones.</li>
      </ul>

      <h2>Qué se conserva</h2>
      <ul>
        <li>
          Las rutas que condujiste siguen existiendo para la empresa, pero ya sin tu nombre: quedan como rutas sin conductor asignado.
        </li>
        <li>
          Si fuiste conductor, se conserva el registro de facturación de las rutas que finalizaste (tu nombre, cédula, placa y la fecha de cada
          ruta), porque es el soporte del cobro del servicio a la empresa de transporte.
        </li>
        <li>
          Si fuiste coordinador, el registro de las importaciones e invitaciones que hiciste se conserva sin indicar quién las hizo, porque forman
          parte de la operación de la empresa.
        </li>
        <li>Las copias de seguridad del servidor pueden conservar la información por un tiempo limitado antes de sobrescribirse.</li>
      </ul>

      <h2>Cuándo no se puede</h2>
      <p>
        No puedes eliminar tu cuenta mientras tengas una ruta en curso: espera a que finalice. Eliminar la cuenta no se puede deshacer; si más
        adelante quieres volver a usar LFMova, tendrás que registrarte de nuevo.
      </p>

      <p>
        Más información en la <Link to="/privacidad">Política de privacidad</Link>.
      </p>
    </DocumentoLegal>
  )
}
