/** Refleja EmpresaDto (LFMova.Application). */
export interface Empresa {
  empresaId: number
  nombre: string
  cif: string
  direccion: string
  coordinadorPrincipal: string | null
  coordinadorPrincipalNombre: string | null
  activa: boolean
}

/** Refleja CrearEmpresaDto (LFMova.Application). */
export interface CrearEmpresaDatos {
  nombre: string
  cif: string
  direccion: string
  cedulaCoordinador: string
  nombreCoordinador: string
  telefonoCoordinador: string
  correoCoordinador: string
}

/** Refleja CoordinadorDto (LFMova.Application). */
export interface Coordinador {
  usuarioRolId: number
  cedula: string
  nombreCompleto: string
  email: string | null
  telefono: string
  activo: boolean
}

/** Refleja AsignarCoordinadorDto (LFMova.Application). Nombre, teléfono y correo solo se usan si la persona aún no tiene cuenta. */
export interface AsignarCoordinadorDatos {
  cedula: string
  nombreCoordinador: string
  telefonoCoordinador: string
  correoCoordinador: string
}
