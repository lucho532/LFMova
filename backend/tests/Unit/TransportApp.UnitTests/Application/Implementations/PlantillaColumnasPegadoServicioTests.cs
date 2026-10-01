using TransportApp.Application.DTOs.Importaciones;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;

namespace TransportApp.UnitTests.Application.Implementations;

public class PlantillaColumnasPegadoServicioTests
{
    private class PlantillaRepositorioFalso : IPlantillaColumnasPegadoRepositorio
    {
        private readonly Dictionary<int, PlantillaColumnasPegado> _plantillas = new();
        private int _siguienteId = 1;

        public Task<PlantillaColumnasPegado?> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_plantillas.Values.FirstOrDefault(p => p.EmpresaId == empresaId));

        public Task AgregarAsync(PlantillaColumnasPegado plantilla)
        {
            plantilla.PlantillaColumnasPegadoId = _siguienteId++;
            _plantillas[plantilla.PlantillaColumnasPegadoId] = plantilla;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task ObtenerPorEmpresaAsync_DevuelveNulo_CuandoTodaviaNoHayPlantilla()
    {
        var servicio = new PlantillaColumnasPegadoServicio(new PlantillaRepositorioFalso());

        Assert.Null(await servicio.ObtenerPorEmpresaAsync(1));
    }

    [Fact]
    public async Task GuardarAsync_CreaLaPlantilla_ConLosCincoCamposEnElOrdenIndicado()
    {
        var servicio = new PlantillaColumnasPegadoServicio(new PlantillaRepositorioFalso());

        var plantilla = await servicio.GuardarAsync(1, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "cedula", "nombre", "barrio", "direccion", "celular" }
        });

        Assert.Equal(new[] { "CEDULA", "NOMBRE", "BARRIO", "DIRECCION", "CELULAR" }, plantilla.ColumnasEnOrden);
    }

    [Fact]
    public async Task GuardarAsync_ReemplazaLaPlantillaExistente_EnVezDeCrearOtra()
    {
        var servicio = new PlantillaColumnasPegadoServicio(new PlantillaRepositorioFalso());
        var primera = await servicio.GuardarAsync(1, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "CEDULA", "NOMBRE", "CELULAR", "DIRECCION", "BARRIO" }
        });

        var segunda = await servicio.GuardarAsync(1, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "NOMBRE", "CEDULA", "CELULAR", "DIRECCION", "BARRIO" }
        });

        Assert.Equal(primera.PlantillaColumnasPegadoId, segunda.PlantillaColumnasPegadoId);
        var actual = await servicio.ObtenerPorEmpresaAsync(1);
        Assert.Equal(new[] { "NOMBRE", "CEDULA", "CELULAR", "DIRECCION", "BARRIO" }, actual!.ColumnasEnOrden);
    }

    [Theory]
    [InlineData("CEDULA,NOMBRE,CELULAR,DIRECCION")] // faltan campos
    [InlineData("CEDULA,NOMBRE,CELULAR,DIRECCION,BARRIO,CEDULA")] // sobra uno repetido
    [InlineData("CEDULA,NOMBRE,CELULAR,DIRECCION,DIRECCION")] // repetido, falta BARRIO
    [InlineData("CEDULA,NOMBRE,CELULAR,DIRECCION,TELEFONO")] // campo que no existe
    [InlineData("CEDULA,NOMBRE,APELLIDOS,APELLIDOS,CELULAR,DIRECCION,BARRIO")] // APELLIDOS repetido
    public async Task GuardarAsync_LanzaExcepcion_CuandoNoCumpleLasReglasDeCadaCampo(string columnas)
    {
        var servicio = new PlantillaColumnasPegadoServicio(new PlantillaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.GuardarAsync(1, new GuardarPlantillaColumnasPegadoDto { ColumnasEnOrden = columnas.Split(',').ToList() }));
    }

    [Fact]
    public async Task GuardarAsync_PermiteApellidosSeparadoYColumnasIgnoradas_SinLimitarseACinco()
    {
        var servicio = new PlantillaColumnasPegadoServicio(new PlantillaRepositorioFalso());

        // Excel con nombre y apellidos en columnas separadas, más dos columnas que no importan (por
        // ejemplo "Área" y "Cargo"): siete columnas en total, no cinco.
        var plantilla = await servicio.GuardarAsync(1, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "CEDULA", "NOMBRE", "APELLIDOS", "IGNORAR", "BARRIO", "DIRECCION", "CELULAR", "IGNORAR" }
        });

        Assert.Equal(new[] { "CEDULA", "NOMBRE", "APELLIDOS", "IGNORAR", "BARRIO", "DIRECCION", "CELULAR", "IGNORAR" }, plantilla.ColumnasEnOrden);
    }
}
