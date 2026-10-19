import { Link } from 'react-router-dom'
import { DocumentoLegal } from '../componentes/DocumentoLegal'

const CORREO = 'luisrak25@gmail.com'

/**
 * Política de privacidad y tratamiento de datos personales de LFMova.
 * Página pública (sin sesión), enlazada desde la ficha de la tienda de
 * aplicaciones y desde "Mi cuenta". Es solo texto: no consulta la API.
 */
export function PaginaPrivacidad() {
  return (
    <DocumentoLegal titulo="Política de privacidad" actualizado="5 de octubre de 2026">
      <p>
        LFMova es una plataforma para organizar y operar el transporte de empleados de una empresa: el coordinador programa las rutas, el conductor las
        ejecuta y el empleado sabe quién lo recoge y cuándo. Esta política explica qué datos personales trata la aplicación (versión web y app de
        Android), para qué y cómo puedes ejercer tus derechos, conforme a la Ley 1581 de 2012 y sus normas reglamentarias de la República de Colombia.
      </p>

      <h2>1. Responsable del tratamiento</h2>
      <p>
        Luis Fernando Ramírez Castaño, Colombia. Contacto para cualquier asunto de datos personales: <a href={`mailto:${CORREO}`}>{CORREO}</a>.
      </p>

      <h2>2. Datos que tratamos</h2>
      <ul>
        <li>
          <strong>Datos de la cuenta:</strong> nombre completo, número de cédula, correo electrónico, teléfono y contraseña (guardada cifrada de forma
          irreversible; nadie puede leerla).
        </li>
        <li>
          <strong>Datos de transporte:</strong> dirección y barrio de recogida, sede de trabajo, fechas y horas de las rutas, y el estado de cada
          recogida. Los puede cargar tu empresa (por ejemplo, desde un archivo de programación) o puedes indicarlos tú.
        </li>
        <li>
          <strong>Ubicación:</strong> solo cuando realizas una acción que la usa: el pasajero que comparte su ubicación o confirma su punto de
          recogida; el conductor al iniciar o finalizar una ruta, al guardar el punto de recogida de un pasajero o al reportar una incidencia. La
          aplicación no registra tu ubicación en segundo plano ni hace seguimiento continuo.
        </li>
        <li>
          <strong>Fotografías:</strong> las que toma el conductor como evidencia al reportar una incidencia de una recogida.
        </li>
        <li>
          <strong>Mensajes:</strong> los del chat entre el conductor y el pasajero de una ruta.
        </li>
        <li>
          <strong>Datos del conductor y su vehículo:</strong> placa, marca, modelo, capacidad y fechas de vigencia del SOAT y de la revisión
          técnico-mecánica.
        </li>
      </ul>
      <p>No pedimos ni tratamos datos sensibles (salud, biométricos, origen étnico, etc.) ni datos de pago.</p>

      <h2>3. Para qué los usamos</h2>
      <ul>
        <li>Crear y proteger tu cuenta, y confirmar tu identidad por correo.</li>
        <li>Programar, asignar y ejecutar las rutas de transporte de la empresa a la que perteneces.</li>
        <li>Permitir que el conductor asignado te ubique, te llame o te escriba para la recogida.</li>
        <li>Dejar constancia de lo ocurrido en cada recogida (por ejemplo, una incidencia con su evidencia).</li>
        <li>Enviarte avisos del servicio: confirmación de correo, recuperación de contraseña, invitaciones y la programación de rutas.</li>
        <li>Facturar el servicio a la empresa de transporte según los conductores que finalizaron rutas en el mes.</li>
      </ul>
      <p>No usamos tus datos para publicidad ni los vendemos.</p>

      <h2>4. Quién puede verlos</h2>
      <ul>
        <li>
          <strong>Los coordinadores de tu empresa</strong>, para gestionar el transporte. Un coordinador solo ve a las personas de su propia empresa.
        </li>
        <li>
          <strong>El conductor asignado a tu ruta</strong> ve tu nombre, cédula, teléfono, dirección y la ubicación que compartas, únicamente para
          recogerte.
        </li>
        <li>
          <strong>Proveedores que nos prestan servicios técnicos</strong>, que tratan los datos por nuestra cuenta: Railway (alojamiento del servidor y
          la base de datos, con servidores fuera de Colombia) y Brevo (envío de correos).
        </li>
        <li>
          Si el conductor abre la navegación, se envían a <strong>Google Maps o Waze</strong> las coordenadas del destino para trazar la ruta.
        </li>
        <li>Las autoridades competentes, cuando la ley lo exija.</li>
      </ul>

      <h2>5. Cuánto tiempo los conservamos</h2>
      <p>
        Mientras tu cuenta exista. Al eliminarla se borran tu cuenta y tu rastro en la plataforma: datos personales, rutas en las que fuiste pasajero,
        mensajes, incidencias y fotografías. Se conserva únicamente, como soporte contable del cobro del servicio, el registro de las rutas que un
        conductor finalizó (su nombre, cédula, placa y la fecha). Las copias de seguridad del servidor pueden conservar la información por un tiempo
        limitado antes de sobrescribirse.
      </p>

      <h2>6. Tus derechos</h2>
      <p>Como titular de los datos puedes, en cualquier momento y sin costo:</p>
      <ul>
        <li>Conocer, actualizar y rectificar tus datos (el nombre y el teléfono los cambias tú mismo en «Mi cuenta»).</li>
        <li>Pedir prueba de la autorización que diste y saber qué uso se les ha dado.</li>
        <li>Revocar la autorización y pedir que se supriman tus datos.</li>
        <li>Presentar quejas ante la Superintendencia de Industria y Comercio.</li>
      </ul>
      <p>
        Para ejercerlos escribe a <a href={`mailto:${CORREO}`}>{CORREO}</a> indicando tu nombre y cédula. Respondemos las consultas en un máximo de
        diez (10) días hábiles y los reclamos en un máximo de quince (15) días hábiles.
      </p>

      <h2>7. Eliminar tu cuenta</h2>
      <p>
        Puedes eliminar tu cuenta tú mismo desde la aplicación, en «Mi cuenta», o pedirlo por correo. Los pasos están en{' '}
        <Link to="/eliminar-cuenta">Eliminar mi cuenta</Link>.
      </p>

      <h2>8. Seguridad</h2>
      <p>
        La comunicación entre la aplicación y el servidor viaja cifrada (HTTPS), las contraseñas se guardan con un algoritmo de cifrado irreversible y
        el acceso a los datos está limitado por rol y por empresa. Ningún sistema es infalible; si detectamos un incidente que te afecte, te lo
        informaremos.
      </p>

      <h2>9. Menores de edad</h2>
      <p>LFMova está dirigida a trabajadores y conductores mayores de edad. No está pensada para menores y no recopilamos sus datos a sabiendas.</p>

      <h2>10. Cambios a esta política</h2>
      <p>
        Si la cambiamos, publicaremos la nueva versión en esta misma página con su fecha. Si el cambio es importante, te avisaremos por la aplicación
        o por correo.
      </p>
    </DocumentoLegal>
  )
}
