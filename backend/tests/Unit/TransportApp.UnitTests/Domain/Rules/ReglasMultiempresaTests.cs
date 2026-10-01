using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasMultiempresaTests
{
    [Fact]
    public void EmpleadoPerteneceAEmpresa_DevuelveVerdadero_CuandoEmpresaCoincide()
    {
        var empleado = new Empleado { EmpresaId = 1 };

        Assert.True(ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, 1));
    }

    [Fact]
    public void EmpleadoPerteneceAEmpresa_DevuelveFalso_CuandoEmpresaNoCoincide()
    {
        var empleado = new Empleado { EmpresaId = 1 };

        Assert.False(ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, 2));
    }

    [Fact]
    public void ServicioEsConsistenteConEmpresa_DevuelveVerdadero_CuandoJornadaYSedeCoinciden()
    {
        var jornada = new Jornada { EmpresaId = 1 };
        var sede = new Sede { EmpresaId = 1 };

        Assert.True(ReglasMultiempresa.ServicioEsConsistenteConEmpresa(jornada, sede, 1));
    }

    [Fact]
    public void ServicioEsConsistenteConEmpresa_DevuelveFalso_CuandoSedeEsDeOtraEmpresa()
    {
        var jornada = new Jornada { EmpresaId = 1 };
        var sede = new Sede { EmpresaId = 2 };

        Assert.False(ReglasMultiempresa.ServicioEsConsistenteConEmpresa(jornada, sede, 1));
    }

    [Fact]
    public void CoordinadorOperaSobreEmpresa_DevuelveVerdadero_CuandoRolActivoYEmpresaCoincide()
    {
        var rol = new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 };

        Assert.True(ReglasMultiempresa.CoordinadorOperaSobreEmpresa(rol, 1));
    }

    [Fact]
    public void CoordinadorOperaSobreEmpresa_DevuelveFalso_CuandoRolEstaInactivo()
    {
        var rol = new UsuarioRol { Rol = Rol.COORDINADOR, Activo = false, EmpresaId = 1 };

        Assert.False(ReglasMultiempresa.CoordinadorOperaSobreEmpresa(rol, 1));
    }

    [Fact]
    public void CoordinadorOperaSobreEmpresa_DevuelveFalso_CuandoEsOtraEmpresa()
    {
        var rol = new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 };

        Assert.False(ReglasMultiempresa.CoordinadorOperaSobreEmpresa(rol, 2));
    }
}
