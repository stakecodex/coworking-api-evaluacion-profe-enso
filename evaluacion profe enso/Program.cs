using Asp.Versioning;
using Coworking.Middleware;
using Coworking.Models;
using Coworking.Repositories;
using Coworking.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Los secretos (Jwt__Key, ConnectionStrings__MiConexion) se entregan por variables de entorno
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Debe definir la variable de entorno Jwt__Key (mínimo 32 caracteres).");

// Add services to the container.
// DbContext, repositorios y servicio de negocio: Scoped (una instancia por request)
builder.Services.AddDbContext<CoworkingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MiConexion")));
builder.Services.AddScoped<IEspacioRepository, EspacioRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();

// Configurar JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Versionamiento por URL: /api/v1/...
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// configuramos los endpoints 
builder.Services.AddEndpointsApiExplorer();
// generamos el documento con swagger, con soporte para probar las APIs bloqueadas con el token
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Reservas Coworking",
        Version = "v1",
        Description = "Gestión de espacios (salas y escritorios) y reservas de un coworking."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xml))
        options.IncludeXmlComments(xml);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Ingrese el token obtenido en /api/v1/User/login"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoworkingDbContext>();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

// Middleware propio: el logging envuelve al manejo de errores para registrar el código final (incluido 500)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Reservas Coworking v1"));

app.UseHttpsRedirection();

// se habilitan los permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
