import './AnimacionRutaBus.css'

/**
 * Decoración puramente visual: una carretera punteada entre una casa y un
 * edificio de oficinas, con un bus que la recorre en bucle. No recibe datos
 * ni tiene lógica: es una animación fija para ambientar la pantalla de inicio
 * de sesión.
 */
export function AnimacionRutaBus() {
  return (
    <div className="animacion-ruta" aria-hidden="true">
      <svg className="animacion-ruta__casa" viewBox="0 0 24 24" width="24" height="24">
        <path d="M12 3 L21 10.5 V20 a1 1 0 0 1 -1 1 H4 a1 1 0 0 1 -1 -1 V10.5 Z" fill="var(--text-h)" />
        <rect x="10" y="14" width="4" height="7" fill="var(--accent)" />
      </svg>

      <div className="animacion-ruta__carretera">
        <svg className="animacion-ruta__bus" viewBox="0 0 56 28" width="56" height="28">
          <rect x="3" y="4" width="46" height="17" rx="4" fill="#ffffff" stroke="var(--border)" strokeWidth="1" />
          <rect x="7" y="7.5" width="9" height="7" rx="1.5" fill="var(--accent)" opacity="0.4" />
          <rect x="18" y="7.5" width="9" height="7" rx="1.5" fill="var(--accent)" opacity="0.4" />
          <rect x="29" y="7.5" width="9" height="7" rx="1.5" fill="var(--accent)" opacity="0.4" />
          <rect x="40" y="7.5" width="6" height="7" rx="1.5" fill="var(--accent)" opacity="0.4" />
          <rect x="3" y="16.5" width="46" height="3" fill="var(--accent)" />
          <circle cx="14" cy="22" r="4" fill="var(--text-h)" />
          <circle cx="38" cy="22" r="4" fill="var(--text-h)" />
          <circle cx="14" cy="22" r="1.6" fill="#ffffff" />
          <circle cx="38" cy="22" r="1.6" fill="#ffffff" />
        </svg>
      </div>

      <svg className="animacion-ruta__edificio" viewBox="0 0 24 24" width="24" height="24">
        <rect x="6" y="2" width="12" height="20" rx="1" fill="var(--text-h)" />
        <rect x="8.5" y="5" width="2.5" height="2.5" fill="var(--accent)" />
        <rect x="13" y="5" width="2.5" height="2.5" fill="var(--accent)" />
        <rect x="8.5" y="10" width="2.5" height="2.5" fill="var(--accent)" />
        <rect x="13" y="10" width="2.5" height="2.5" fill="var(--accent)" />
        <rect x="8.5" y="15" width="2.5" height="2.5" fill="var(--accent)" />
        <rect x="13" y="15" width="2.5" height="2.5" fill="var(--accent)" />
      </svg>
    </div>
  )
}
