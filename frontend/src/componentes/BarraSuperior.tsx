import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { obtenerCuenta } from '../servicios/servicioCuenta'
import { etiquetaDelRolPrincipal, obtenerNombreDelToken, obtenerRolesDelToken, rutaInicialSegunRoles } from '../servicios/tokenJwt'
import { BotonRefrescar } from './BotonRefrescar'
import { CampanaNotificaciones } from './CampanaNotificaciones'
import { MenuConfiguracion } from './MenuConfiguracion'
import '../estilos/componentes/BarraSuperior.css'

/**
 * Barra superior fija de toda la aplicación: marca a la izquierda (lleva
 * al inicio que corresponde al rol de la persona); a la
 * derecha, el botón de actualizar la pantalla, las notificaciones, el nombre y el rol de la persona cuando hay
 * sesión (lleva a "Mi cuenta") y la rueda de configuración, que guarda el
 * cambio de tema y cerrar sesión.
 */
interface PropiedadesBarraSuperior {
  /** Recarga la pantalla actual con los datos más recientes. */
  alRefrescar: () => void
}

export function BarraSuperior({ alRefrescar }: PropiedadesBarraSuperior) {
  const { token, nombreActualizado } = useAutenticacion()
  const nombre = nombreActualizado ?? (token ? obtenerNombreDelToken(token) : '')
  const rol = token ? etiquetaDelRolPrincipal(obtenerRolesDelToken(token)) : ''
  // La marca lleva al inicio que corresponde al rol; sin sesión, a la pantalla de entrada.
  const rutaDeInicio = token ? rutaInicialSegunRoles(obtenerRolesDelToken(token)) : '/iniciar-sesion'
  const [empresas, setEmpresas] = useState<string[]>([])

  // El nombre de la empresa se muestra bajo la marca; el administrador de plataforma no pertenece a ninguna.
  useEffect(() => {
    if (!token) {
      setEmpresas([])
      return
    }
    let cancelado = false
    obtenerCuenta(token)
      .then((cuenta) => {
        if (!cancelado) setEmpresas(cuenta.empresas ?? [])
      })
      .catch(() => {
        if (!cancelado) setEmpresas([])
      })
    return () => {
      cancelado = true
    }
  }, [token])

  return (
    <header className="barra-superior">
      <div className="barra-superior__identidad">
        <Link to={rutaDeInicio} className="barra-superior__marca" title="Ir al inicio">
          <img src="/icono-lfmova.svg" alt="" className="barra-superior__logo" />
          LFMova
        </Link>
        {empresas.length > 0 && <span className="barra-superior__empresa">{empresas.join(' · ')}</span>}
      </div>
      <div className="barra-superior__acciones">
        {token && <BotonRefrescar alRefrescar={alRefrescar} />}
        <CampanaNotificaciones />
      </div>
      {token && (
        <Link to="/mi-cuenta" className="barra-superior__usuario" title="Mi cuenta">
          <span className="barra-superior__nombre">{nombre}</span>
          <span className="barra-superior__rol">{rol}</span>
        </Link>
      )}
      <MenuConfiguracion />
    </header>
  )
}
