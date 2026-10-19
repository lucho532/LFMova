// Siembra datos de demostración en la base de desarrollo: 3 empresas, cada una con su coordinador,
// 10 conductores con vehículo y unidad, una base de empleados y una jornada completa (entrada y salida)
// que llena todos los vehículos. Usa los mismos servicios de la aplicación (importación de Excel con
// reparto entre unidades), así que ejercita la lógica real. No envía correos.
//
// Uso: dotnet run --project tools/SembrarDatosDemo -- <ruta appsettings.Development.json> [AAAA-MM-DD]

using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure;
using LFMova.Infrastructure.Data;

const string ClaveDemo = "Demo12345";

var configuracion = new ConfigurationBuilder().AddJsonFile(args[0]).Build();
var fecha = args.Length > 1 ? DateOnly.Parse(args[1]) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

var servicios = new ServiceCollection();
servicios.AddLogging();
servicios.AgregarInfraestructura(configuracion);
servicios.AgregarAplicacion();
await using var proveedor = servicios.BuildServiceProvider();

var empresasDemo = new[]
{
    (Nombre: "Transportes Panamericana SAS", Cif: "900100001-1", Sede: "PANAMERICANA", Capacidades: new[] { 4, 4, 5, 5, 6, 6, 8, 8, 10, 10 }, Prefijo: "PAN"),
    (Nombre: "Movilidad Patria SAS", Cif: "900100002-2", Sede: "PATRIA", Capacidades: new[] { 4, 4, 4, 6, 6, 8, 8, 8, 12, 12 }, Prefijo: "PAT"),
    (Nombre: "Rutas del Norte SAS", Cif: "900100003-3", Sede: "NORTE", Capacidades: new[] { 5, 5, 5, 5, 7, 7, 9, 9, 11, 11 }, Prefijo: "NOR"),
};

var nombres = new[] { "Ana", "Luis", "María", "Jorge", "Sofía", "Andrés", "Camila", "Diego", "Valentina", "Carlos", "Laura", "Felipe", "Daniela", "Santiago", "Paula", "Mateo", "Juliana", "Sebastián", "Natalia", "Esteban" };
var apellidos = new[] { "Pérez Gómez", "Rojas Díaz", "Torres Ruiz", "Vega Mora", "Lara Cruz", "Mejía Paz", "Castro León", "Ortiz Sosa", "Ramírez Peña", "Herrera Cano", "Molina Ríos", "Suárez Pinto", "Cortés Vera", "Duarte Gil", "Bravo Nieto" };
var barrios = new[] { "Chapinero", "Teusaquillo", "Barrios Unidos", "Suba", "Engativá", "Bosa", "Kennedy", "Fontibón", "Usaquén", "Ciudad Bolívar" };
var marcas = new[] { ("Renault", "Master"), ("Chevrolet", "N300"), ("Hyundai", "H1"), ("Kia", "Carnival"), ("Toyota", "Hiace") };

var cedulaCoordinador = 1000000001L;
var cedulaConductor = 2000000001L;
var cedulaEmpleado = 3000000001L;

for (var e = 0; e < empresasDemo.Length; e++)
{
    var demo = empresasDemo[e];
    using var alcance = proveedor.CreateScope();
    var sp = alcance.ServiceProvider;
    var contexto = sp.GetRequiredService<LFMovaDbContext>();
    var hasheador = sp.GetRequiredService<IHasheadorContrasenas>();

    if (await contexto.Empresas.AnyAsync(x => x.Nombre == demo.Nombre))
    {
        Console.WriteLine($"{demo.Nombre}: ya existe, se omite.");
        cedulaCoordinador++;
        cedulaConductor += 10;
        cedulaEmpleado += demo.Capacidades.Sum();
        continue;
    }

    var empresa = new Empresa { Nombre = demo.Nombre, Cif = demo.Cif, Direccion = $"Carrera {10 + e} # 20-{30 + e}, Bogotá", Activa = true };
    contexto.Empresas.Add(empresa);
    await contexto.SaveChangesAsync();

    Usuario NuevoUsuario(long cedula, string nombre, string correo, string telefono)
        => new()
        {
            Cedula = cedula.ToString(), NombreCompleto = nombre, Email = correo, Telefono = telefono,
            PasswordHash = hasheador.Hashear(ClaveDemo), CorreoConfirmado = true, Activo = true
        };

    // Coordinador.
    var coordinador = NuevoUsuario(cedulaCoordinador++, $"Coordinador {e + 1} {demo.Prefijo}", $"coordinador{e + 1}@demo.lfmova.test", $"300000000{e + 1}");
    contexto.Usuarios.Add(coordinador);
    await contexto.SaveChangesAsync();
    contexto.UsuarioRoles.Add(new UsuarioRol { UsuarioId = coordinador.UsuarioId, Rol = Rol.COORDINADOR, EmpresaId = empresa.EmpresaId, Activo = true });
    await contexto.SaveChangesAsync();

    // Conductores con su vehículo y unidad (mismo camino que usa la aplicación).
    var conductorServicio = sp.GetRequiredService<IConductorServicio>();
    for (var c = 0; c < 10; c++)
    {
        var persona = NuevoUsuario(cedulaConductor++, $"{nombres[(c + e * 3) % nombres.Length]} {apellidos[(c * 2 + e) % apellidos.Length]}", $"conductor{e + 1}.{c + 1}@demo.lfmova.test", $"31{e}00000{c:00}");
        contexto.Usuarios.Add(persona);
        await contexto.SaveChangesAsync();
        contexto.UsuarioRoles.Add(new UsuarioRol { UsuarioId = persona.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = null, Activo = true });
        await contexto.SaveChangesAsync();

        var (marca, modelo) = marcas[(c + e) % marcas.Length];
        await conductorServicio.CrearAsync(empresa.EmpresaId, new CrearConductorDto
        {
            Cedula = persona.Cedula,
            Vehiculo = new CrearVehiculoDto
            {
                Placa = $"{demo.Prefijo}{100 + c}", Marca = marca, Modelo = $"{2019 + (c % 6)} {modelo}", Capacidad = demo.Capacidades[c],
                VigenciaSoat = new DateOnly(2027, 3, 1).AddDays(c * 20), VigenciaTecnomecanica = new DateOnly(2027, 8, 1).AddDays(c * 15)
            }
        });
    }

    // Hoja de Excel: todos los empleados entran a las 06:00 y salen a las 15:00; la cantidad iguala la capacidad total.
    var totalEmpleados = demo.Capacidades.Sum();
    using var libro = new XLWorkbook();
    var hoja = libro.AddWorksheet("Programación");
    hoja.Cell(1, 1).Value = "TRANSPORTADOR:";
    hoja.Cell(1, 2).Value = demo.Nombre.ToUpperInvariant();
    hoja.Cell(2, 1).Value = "FECHA:";
    hoja.Cell(2, 2).Value = $"{fecha.Day} {fecha:MMM}".ToUpperInvariant();
    var encabezados = new[] { "CEDULA", "NOMBRE", "APELLIDOS", "DIRECCION", "BARRIO", "CELULAR", "ENTRA", "SALE" };
    for (var i = 0; i < encabezados.Length; i++)
    {
        hoja.Cell(4, i + 1).Value = encabezados[i];
    }

    var fila = 5;
    foreach (var (titulo, hora, columna) in new[] { ($"ENTRADA {demo.Sede}", new TimeSpan(6, 0, 0), 7), ($"SALIDA {demo.Sede}", new TimeSpan(15, 0, 0), 8) })
    {
        hoja.Cell(fila++, 1).Value = titulo;
        for (var i = 0; i < totalEmpleados; i++)
        {
            var cedula = cedulaEmpleado + i;
            hoja.Cell(fila, 1).Value = cedula;
            hoja.Cell(fila, 2).Value = nombres[(i + e) % nombres.Length];
            hoja.Cell(fila, 3).Value = apellidos[(i * 3 + e) % apellidos.Length];
            hoja.Cell(fila, 4).Value = $"Calle {20 + i * 2} # {5 + i % 30}-{10 + i % 70}";
            hoja.Cell(fila, 5).Value = barrios[(i + e) % barrios.Length];
            hoja.Cell(fila, 6).Value = 3200000000L + e * 10000000L + i;
            hoja.Cell(fila, columna).Value = hora;
            fila++;
        }
    }

    cedulaEmpleado += totalEmpleados;
    using var memoria = new MemoryStream();
    libro.SaveAs(memoria);
    memoria.Position = 0;

    var importacion = sp.GetRequiredService<IImportacionExcelServicio>();
    var resultado = await importacion.ImportarAsync(empresa.EmpresaId, coordinador.UsuarioId, $"demo-{demo.Prefijo}.xlsx", memoria, fecha, null, true);

    await sp.GetRequiredService<IJornadaServicio>().PublicarAsync(empresa.EmpresaId, resultado.JornadaId);

    Console.WriteLine($"{demo.Nombre}: coordinador {coordinador.Cedula}, 10 conductores, {resultado.EmpleadosCreados} empleados, " +
        $"{resultado.ServiciosCreados} rutas, {resultado.PasajerosAsignados} pasajeros asignados, jornada {fecha} publicada.");
}

Console.WriteLine($"Listo. Clave de todos los usuarios con contraseña: {ClaveDemo}");
