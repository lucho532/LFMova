import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { obtenerCuenta } from '../servicios/servicioCuenta'
import { etiquetaDelRolPrincipal, obtenerNombreDelToken, obtenerRolesDelToken } from '../servicios/tokenJwt'
import { BotonCerrarSesion } from './BotonCerrarSesion'
import { CampanaNotificaciones } from './CampanaNotificaciones'
import { BotonTema } from './BotonTema'
import '../estilos/componentes/BarraSuperior.css'

/**
 * Barra superior fija de toda la aplicación: marca a la izquierda; a la
 * derecha, cuando hay sesión, el nombre y el rol de la persona, y las
 * acciones globales (cerrar sesión, tema).
 */
export function BarraSuperior() {
  const { token, nombreActualizado } = useAutenticacion()
  const nombre = nombreActualizado ?? (token ? obtenerNombreDelToken(token) : '')
  const rol = token ? etiquetaDelRolPrincipal(obtenerRolesDelToken(token)) : ''
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
        <span className="barra-superior__marca">
          <img src="/icono-lfmova.svg" alt="" className="barra-superior__logo" />
          LFMova
        </span>
        {empresas.length > 0 && <span className="barra-superior__empresa">{empresas.join(' · ')}</span>}
      </div>
      <div className="barra-superior__acciones">
        <CampanaNotificaciones />
        <BotonCerrarSesion />
        <BotonTema />
      </div>
      {token && (
        <Link to="/mi-cuenta" className="barra-superior__usuario" title="Mi cuenta">
          <span className="barra-superior__nombre">{nombre}</span>
          <span className="barra-superior__rol">{rol}</span>
        </Link>
      )}
    </header>
  )
}
