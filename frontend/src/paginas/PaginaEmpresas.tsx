import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { BuscadorPersona } from '../componentes/BuscadorPersona'
import { PanelAccionesPersonaAdministrador } from '../componentes/PanelAccionesPersonaAdministrador'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { EnlaceBoton } from '../componentes/EnlaceBoton'
import { EtiquetaEstado } from '../componentes/EtiquetaEstado'
import { FilaTarjetasResumen } from '../componentes/FilaTarjetasResumen'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TablaDatos } from '../componentes/TablaDatos'
import { TarjetaResumen } from '../componentes/TarjetaResumen'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Empresa } from '../modelos/empresa'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarEmpresa, desactivarEmpresa, obtenerEmpresas } from '../servicios/servicioEmpresas'
import './PaginaEmpresas.css'

/**
 * Panel del administrador de plataforma: indicadores calculados sobre las
 * empresas ya cargadas (no hay un endpoint de estadísticas), búsqueda local y
 * tabla con el coordinador de cada empresa. Las empresas sin coordinador
 * activo se destacan porque requieren una acción del administrador.
 */
export function PaginaEmpresas() {
  const { token } = useAutenticacion()
  const [empresas, setEmpresas] = useState<Empresa[]>([])
  const [busqueda, setBusqueda] = useState('')
  const [cedulaBuscada, setCedulaBuscada] = useState('')
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(true)
  const [empresaIdEnCurso, setEmpresaIdEnCurso] = useState<number | null>(null)

  async function cargarEmpresas() {
    if (!token) return
    try {
      setEmpresas(await obtenerEmpresas(token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las empresas.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarEmpresas()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  const filas = useMemo(() => {
    const texto = busqueda.trim().toLowerCase()
    const ordenadas = [...empresas].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'))
    if (!texto) return ordenadas
    return ordenadas.filter((e) => [e.nombre, e.cif, e.coordinadorPrincipalNombre ?? '', e.coordinadorPrincipal ?? ''].some((valor) => valor.toLowerCase().includes(texto)))
  }, [empresas, busqueda])

  const activas = empresas.filter((e) => e.activa).length
  const sinCoordinador = empresas.filter((e) => !e.coordinadorPrincipal).length

  async function alCambiarEstado(empresa: Empresa) {
    if (!token) return
    setEmpresaIdEnCurso(empresa.empresaId)
    setMensajeError(null)
    try {
      if (empresa.activa) {
        await desactivarEmpresa(empresa.empresaId, token)
      } else {
        await activarEmpresa(empresa.empresaId, token)
      }
      await cargarEmpresas()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cambiar el estado de la empresa.')
    } finally {
      setEmpresaIdEnCurso(null)
    }
  }

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina
        titulo="Empresas"
        subtitulo="Empresas registradas en la plataforma."
        acciones={<EnlaceBoton a="/empresas/nueva">+ Nueva empresa</EnlaceBoton>}
      />

      <FilaTarjetasResumen>
        <TarjetaResumen titulo="Empresas" valor={empresas.length} />
        <TarjetaResumen titulo="Activas" valor={activas} detalle={`${empresas.length - activas} inactivas`} />
        <TarjetaResumen titulo="Sin coordinador" valor={sinCoordinador} detalle="Requieren asignar uno" destacada={sinCoordinador > 0} />
      </FilaTarjetasResumen>

      <div className="pagina-empresas__buscador">
        <input
          type="search"
          placeholder="Filtrar empresas, o escribe una cédula y pulsa Enter para buscar a la persona…"
          value={busqueda}
          onChange={(evento) => setBusqueda(evento.target.value)}
          onKeyDown={(evento) => {
            if (evento.key === 'Enter') setCedulaBuscada(busqueda.trim())
          }}
        />
        <BotonSecundario type="button" onClick={() => setCedulaBuscada(busqueda.trim())} disabled={!busqueda.trim()}>
          Buscar persona
        </BotonSecundario>
      </div>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      {cedulaBuscada && (
        <BuscadorPersona
          cedulaInicial={cedulaBuscada}
          ayuda="Búsqueda en toda la base de datos. Verifica que es la persona y luego asígnale un rol y una empresa."
          acciones={(persona) => (
            <PanelAccionesPersonaAdministrador
              persona={persona}
              empresas={empresas}
              alTerminar={async (mensaje) => {
                setMensajeExito(mensaje)
                setCedulaBuscada('')
                setBusqueda('')
                await cargarEmpresas()
              }}
            />
          )}
        />
      )}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : filas.length === 0 ? (
        <p className="contenedor-pagina__estado">
          {empresas.length === 0
            ? 'Todavía no hay empresas registradas.'
            : `Ninguna empresa, CIF o coordinador coincide con "${busqueda.trim()}". La búsqueda solo encuentra coordinadores que ya tienen una empresa.`}{' '}
          <Link to="/empresas/nueva">Crear una empresa</Link>
        </p>
      ) : (
        <TablaDatos columnas={['Empresa', 'Dirección', 'Coordinador', 'Estado', '']}>
          {filas.map((empresa) => (
            <tr key={empresa.empresaId}>
              <td>
                <Link to={`/empresas/${empresa.empresaId}`}>{empresa.nombre}</Link>
                <div className="pagina-empresas__secundario">CIF {empresa.cif}</div>
              </td>
              <td>{empresa.direccion}</td>
              <td>
                {empresa.coordinadorPrincipal ? (
                  <>
                    {empresa.coordinadorPrincipalNombre}
                    <div className="pagina-empresas__secundario">Cédula {empresa.coordinadorPrincipal}</div>
                  </>
                ) : (
                  <Link to={`/empresas/${empresa.empresaId}`} className="pagina-empresas__sin-coordinador">
                    ! Sin coordinador · asignar
                  </Link>
                )}
              </td>
              <td>
                <EtiquetaEstado activo={empresa.activa} />
              </td>
              <td className="pagina-empresas__acciones">
                <BotonSecundario onClick={() => alCambiarEstado(empresa)} disabled={empresaIdEnCurso === empresa.empresaId}>
                  {empresa.activa ? 'Desactivar' : 'Activar'}
                </BotonSecundario>
              </td>
            </tr>
          ))}
        </TablaDatos>
      )}
    </ContenedorPagina>
  )
}
