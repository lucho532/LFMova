using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasVinculacionConductorEmpresaTests
{
    [Fact]
    public void YaExisteVinculacion_DevuelveVerdadero_CuandoYaHayUnaVinculacionConEsaEmpresa()
    {
        var vinculaciones = new List<VinculacionConductorEmpresa>
        {
            new() { ConductorId = 1, EmpresaId = 2, Activa = false }
        };

        Assert.True(ReglasVinculacionConductorEmpresa.YaExisteVinculacion(vinculaciones, 2));
    }

    [Fact]
    public void YaExisteVinculacion_DevuelveFalso_CuandoNoHayVinculacionConEsaEmpresa()
    {
        var vinculaciones = new List<VinculacionConductorEmpresa>
        {
            new() { ConductorId = 1, EmpresaId = 3, Activa = true }
        };

        Assert.False(ReglasVinculacionConductorEmpresa.YaExisteVinculacion(vinculaciones, 2));
    }
}
