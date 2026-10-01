using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasUsuarioRolTests
{
    [Fact]
    public void ViolaUnicidadDeCoordinador_DevuelveVerdadero_CuandoYaEsCoordinadorActivoDeOtraEmpresa()
    {
        var rolesActivos = new[]
        {
            new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 }
        };

        Assert.True(ReglasUsuarioRol.ViolaUnicidadDeCoordinador(rolesActivos, 2));
    }

    [Fact]
    public void ViolaUnicidadDeCoordinador_DevuelveFalso_CuandoEsLaMismaEmpresa()
    {
        var rolesActivos = new[]
        {
            new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 }
        };

        Assert.False(ReglasUsuarioRol.ViolaUnicidadDeCoordinador(rolesActivos, 1));
    }

    [Fact]
    public void ViolaUnicidadDeCoordinador_DevuelveFalso_CuandoElRolDeOtraEmpresaEstaInactivo()
    {
        var rolesActivos = new[]
        {
            new UsuarioRol { Rol = Rol.COORDINADOR, Activo = false, EmpresaId = 1 }
        };

        Assert.False(ReglasUsuarioRol.ViolaUnicidadDeCoordinador(rolesActivos, 2));
    }

    [Fact]
    public void EsAutoasignacion_DevuelveVerdadero_CuandoEjecutorYDestinoSonElMismo()
    {
        Assert.True(ReglasUsuarioRol.EsAutoasignacion(5, 5));
    }

    [Fact]
    public void EsAutoasignacion_DevuelveFalso_CuandoSonUsuariosDistintos()
    {
        Assert.False(ReglasUsuarioRol.EsAutoasignacion(5, 6));
    }

    [Fact]
    public void YaEsCoordinadorDeEmpresa_DevuelveVerdadero_CuandoTieneRolActivoEnEsaEmpresa()
    {
        var roles = new[] { new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 } };

        Assert.True(ReglasUsuarioRol.YaEsCoordinadorDeEmpresa(roles, 1));
    }

    [Fact]
    public void YaEsCoordinadorDeEmpresa_DevuelveFalso_CuandoElRolEsDeOtraEmpresa()
    {
        var roles = new[] { new UsuarioRol { Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 } };

        Assert.False(ReglasUsuarioRol.YaEsCoordinadorDeEmpresa(roles, 2));
    }

    [Fact]
    public void EsElUltimoCoordinadorActivo_DevuelveVerdadero_CuandoEsElUnicoActivo()
    {
        var roles = new[]
        {
            new UsuarioRol { UsuarioRolId = 10, Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 }
        };

        Assert.True(ReglasUsuarioRol.EsElUltimoCoordinadorActivo(roles, 10));
    }

    [Fact]
    public void EsElUltimoCoordinadorActivo_DevuelveFalso_CuandoHayOtroCoordinadorActivo()
    {
        var roles = new[]
        {
            new UsuarioRol { UsuarioRolId = 10, Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 },
            new UsuarioRol { UsuarioRolId = 11, Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 }
        };

        Assert.False(ReglasUsuarioRol.EsElUltimoCoordinadorActivo(roles, 10));
    }

    [Fact]
    public void EsElUltimoCoordinadorActivo_DevuelveFalso_CuandoElQueSeRevocaYaEstaInactivo()
    {
        var roles = new[]
        {
            new UsuarioRol { UsuarioRolId = 10, Rol = Rol.COORDINADOR, Activo = false, EmpresaId = 1 },
            new UsuarioRol { UsuarioRolId = 11, Rol = Rol.COORDINADOR, Activo = true, EmpresaId = 1 }
        };

        Assert.False(ReglasUsuarioRol.EsElUltimoCoordinadorActivo(roles, 10));
    }
}
