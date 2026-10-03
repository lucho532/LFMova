import { useState } from 'react'
import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom'
import './estilos/App.css'
import { BarraLateral } from './componentes/BarraLateral'
import { AvisoActualizacion } from './componentes/AvisoActualizacion'
import { BarraSuperior } from './componentes/BarraSuperior'
import { ProveedorAutenticacion } from './contexto/ContextoAutenticacion'
import { ProveedorNotificaciones } from './contexto/ContextoNotificaciones'
import { ProveedorTema } from './contexto/ContextoTema'
import { PaginaConfirmarCorreo } from './paginas/PaginaConfirmarCorreo'
import { PaginaDescargar } from './paginas/PaginaDescargar'
import { PaginaEliminarCuenta } from './paginas/PaginaEliminarCuenta'
import { PaginaCrearCuenta } from './paginas/PaginaCrearCuenta'
import { PaginaEmpresaSegunRol } from './paginas/PaginaEmpresaSegunRol'
import { PaginaEmpresas } from './paginas/PaginaEmpresas'
import { PaginaIniciarSesion } from './paginas/PaginaIniciarSesion'
import { PaginaInvitacion } from './paginas/PaginaInvitacion'
import { PaginaMiJornada } from './paginas/PaginaMiJornada'
import { PaginaMiCuenta } from './paginas/PaginaMiCuenta'
import { PaginaMiTransporte } from './paginas/PaginaMiTransporte'
import { PaginaNuevaEmpresa } from './paginas/PaginaNuevaEmpresa'
import { PaginaPrivacidad } from './paginas/PaginaPrivacidad'
import { PaginaOlvideContrasena } from './paginas/PaginaOlvideContrasena'
import { PaginaRestablecerContrasena } from './paginas/PaginaRestablecerContrasena'
import { PaginaConductores } from './paginas/PaginaConductores'
import { PaginaDetalleConductor } from './paginas/PaginaDetalleConductor'
import { PaginaDetalleProgramacion } from './paginas/PaginaDetalleProgramacion'
import { PaginaEmpleados } from './paginas/PaginaEmpleados'
import { PaginaDetalleSede } from './paginas/PaginaDetalleSede'
import { PaginaChatConductor } from './paginas/PaginaChatConductor'
import { PaginaChatEmpleado } from './paginas/PaginaChatEmpleado'
import { PaginaImportarExcel } from './paginas/PaginaImportarExcel'
import { PaginaPasajerosEmpresa } from './paginas/PaginaPasajerosEmpresa'
import { PaginaRutasEmpresa } from './paginas/PaginaRutasEmpresa'
import { PaginaNuevaProgramacion } from './paginas/PaginaNuevaProgramacion'
import { PaginaNuevaSede } from './paginas/PaginaNuevaSede'
import { PaginaProgramaciones } from './paginas/PaginaProgramaciones'
import { PaginaSedes } from './paginas/PaginaSedes'
import { PaginaServicioConductor } from './paginas/PaginaServicioConductor'
import { PaginaZonas } from './paginas/PaginaZonas'
import { RutaProtegida } from './rutas/RutaProtegida'

/** Componente raíz: configura los proveedores globales (tema, autenticación) y las rutas. */
function App() {
  // Al cambiar, la pantalla actual se vuelve a montar y recarga sus datos (botón de actualizar de la barra superior).
  const [recarga, setRecarga] = useState(0)

  return (
    <ProveedorTema>
      <ProveedorAutenticacion>
        <ProveedorNotificaciones>
        <Router>
          <AvisoActualizacion />
          <BarraSuperior alRefrescar={() => setRecarga((valor) => valor + 1)} />
          <div className="app-cuerpo">
            <BarraLateral />
            <div className="app-contenido" key={recarga}>
              <Routes>
                <Route path="/iniciar-sesion" element={<PaginaIniciarSesion />} />
                <Route path="/crear-cuenta" element={<PaginaCrearCuenta />} />
                <Route path="/confirmar-correo" element={<PaginaConfirmarCorreo />} />
                <Route path="/olvide-contrasena" element={<PaginaOlvideContrasena />} />
                <Route path="/restablecer-contrasena" element={<PaginaRestablecerContrasena />} />
                <Route path="/invitacion" element={<PaginaInvitacion />} />
                <Route path="/descargar" element={<PaginaDescargar />} />
                <Route path="/privacidad" element={<PaginaPrivacidad />} />
                <Route path="/eliminar-cuenta" element={<PaginaEliminarCuenta />} />
                <Route
                  path="/conductor"
                  element={
                    <RutaProtegida>
                      <PaginaMiJornada />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/conductor/finalizadas"
                  element={
                    <RutaProtegida>
                      <PaginaMiJornada />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/conductor/vehiculo"
                  element={
                    <RutaProtegida>
                      <PaginaMiJornada />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/conductor/servicios/:empresaId/:jornadaId/:servicioId"
                  element={
                    <RutaProtegida>
                      <PaginaServicioConductor />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/conductor/servicios/:empresaId/:jornadaId/:servicioId/chat/:servicioPasajeroId"
                  element={
                    <RutaProtegida>
                      <PaginaChatConductor />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/mi-transporte/chat/:servicioPasajeroId"
                  element={
                    <RutaProtegida>
                      <PaginaChatEmpleado />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/mi-cuenta"
                  element={
                    <RutaProtegida>
                      <PaginaMiCuenta />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/mi-transporte"
                  element={
                    <RutaProtegida>
                      <PaginaMiTransporte />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/nueva"
                  element={
                    <RutaProtegida>
                      <PaginaNuevaEmpresa />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas"
                  element={
                    <RutaProtegida>
                      <PaginaEmpresas />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId"
                  element={
                    <RutaProtegida>
                      <PaginaEmpresaSegunRol />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/sedes"
                  element={
                    <RutaProtegida>
                      <PaginaSedes />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/sedes/nueva"
                  element={
                    <RutaProtegida>
                      <PaginaNuevaSede />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/sedes/:sedeId"
                  element={
                    <RutaProtegida>
                      <PaginaDetalleSede />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/conductores"
                  element={
                    <RutaProtegida>
                      <PaginaConductores />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/conductores/:conductorId"
                  element={
                    <RutaProtegida>
                      <PaginaDetalleConductor />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/empleados"
                  element={
                    <RutaProtegida>
                      <PaginaEmpleados />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/programaciones"
                  element={
                    <RutaProtegida>
                      <PaginaProgramaciones />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/programaciones/nueva"
                  element={
                    <RutaProtegida>
                      <PaginaNuevaProgramacion />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/programaciones/:programacionId"
                  element={
                    <RutaProtegida>
                      <PaginaDetalleProgramacion />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/rutas"
                  element={
                    <RutaProtegida>
                      <PaginaRutasEmpresa />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/pasajeros"
                  element={
                    <RutaProtegida>
                      <PaginaPasajerosEmpresa />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/programacion"
                  element={
                    <RutaProtegida>
                      <PaginaImportarExcel />
                    </RutaProtegida>
                  }
                />
                <Route
                  path="/empresas/:empresaId/zonas"
                  element={
                    <RutaProtegida>
                      <PaginaZonas />
                    </RutaProtegida>
                  }
                />
                <Route path="*" element={<Navigate to="/iniciar-sesion" replace />} />
              </Routes>
            </div>
          </div>
        </Router>
        </ProveedorNotificaciones>
      </ProveedorAutenticacion>
    </ProveedorTema>
  )
}

export default App
