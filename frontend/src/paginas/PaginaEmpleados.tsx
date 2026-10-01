import { useParams } from 'react-router-dom'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { ListaEmpleados } from '../componentes/ListaEmpleados'

/**
 * Directorio de empleados de la empresa: permite buscarlos por cédula o
 * nombre y, desde ahí, asignarles el rol de coordinador o de conductor.
 */
export function PaginaEmpleados() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const idEmpresa = Number(empresaId)

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina titulo="Empleados" subtitulo="Busca a un empleado por cédula, nombre o correo y asígnale el rol de coordinador o de conductor. A alguien que todavía no es parte de la empresa, invítalo por correo." />
      <ListaEmpleados empresaId={idEmpresa} />
    </ContenedorPagina>
  )
}
