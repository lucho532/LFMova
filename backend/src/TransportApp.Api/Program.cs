using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;
using TransportApp.Api.BackgroundServices;
using TransportApp.Api.Configuration;
using TransportApp.Application;
using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure;
using TransportApp.Infrastructure.Autenticacion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(opciones => opciones.SuppressAsyncSuffixInActionNames = false);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LFMova API",
        Version = "v1",
        Description = "Plataforma de gestión y operación de transporte empresarial multiempresa. Todas las fechas/horas se manejan en UTC (ver AGENTS.md §41)."
    });

    var archivoXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(archivoXml))
    {
        opciones.IncludeXmlComments(archivoXml);
    }

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT obtenido de POST /api/autenticacion/iniciar-sesion. Ejemplo: \"Bearer {token}\"."
    });

    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", documento, null), new List<string>() }
    });
});

builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AgregarAplicacion();

var opcionesFrontend = builder.Configuration.GetSection(OpcionesFrontend.Seccion).Get<OpcionesFrontend>() ?? new OpcionesFrontend();
builder.Services.AddSingleton(opcionesFrontend);

builder.Services.AddHostedService<AlertaEjecucionBackgroundService>();

var opcionesJwt = builder.Configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opcionesJwt.Emisor,
            ValidateAudience = true,
            ValidAudience = opcionesJwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesJwt.ClaveSecreta)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("RequiereAdministradorPlataforma", p => p.Requirements.Add(new RequisitoRol(Rol.ADMINISTRADOR_PLATAFORMA)));
    opciones.AddPolicy("RequiereCoordinador", p => p.Requirements.Add(new RequisitoRol(Rol.COORDINADOR)));
    opciones.AddPolicy("RequiereConductor", p => p.Requirements.Add(new RequisitoRol(Rol.CONDUCTOR)));
    opciones.AddPolicy("RequiereEmpleado", p => p.Requirements.Add(new RequisitoRol(Rol.EMPLEADO)));
});
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ManejadorRequisitoRol>();

const string PoliticaCorsFrontend = "PoliticaCorsFrontend";
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaCorsFrontend, politica =>
    {
        politica.WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(PoliticaCorsFrontend);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada de la API. Se expone parcialmente para permitir que
/// <c>WebApplicationFactory&lt;Program&gt;</c> arranque la aplicación en las
/// pruebas de integración.
/// </summary>
public partial class Program
{
}
