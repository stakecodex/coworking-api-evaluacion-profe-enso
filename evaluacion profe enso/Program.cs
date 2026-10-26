using Asp.Versioning;
using Coworking.Middleware;
using Coworking.Models;
using Coworking.Repositories;
using Coworking.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddControllers()
    .AddMvcOptions(o =>
    {
        // Mensajes de enlace de datos en español
        var m = o.ModelBindingMessageProvider;
        m.SetValueIsInvalidAccessor(v => $"El valor '{v}' no es válido.");
        m.SetAttemptedValueIsInvalidAccessor((v, campo) => $"El valor '{v}' no es válido para {campo}.");
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"El valor '{v}' no es válido.");
        m.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
        m.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
        m.SetValueMustNotBeNullAccessor(v => $"El valor '{v}' es obligatorio.");
        m.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor obligatorio '{campo}'.");
        m.SetMissingKeyOrValueAccessor(() => "Se requiere un valor.");
        m.SetMissingRequestBodyRequiredValueAccessor(() => "Se requiere un cuerpo en la solicitud.");
        m.SetUnknownValueIsInvalidAccessor(campo => $"El valor no es válido para {campo}.");
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor no es válido.");
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        foreach (var (codigo, mapeo) in o.ClientErrorMapping)
            mapeo.Title = Textos.Titulo(codigo);

        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errores = ctx.ModelState
                .Where(e => e.Value!.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x =>
                        x.Exception != null || ctx.ModelState.Keys.Any(k => k == e.Key && k.StartsWith("$")) ? "El formato del dato enviado no es válido."
                        : x.ErrorMessage.StartsWith("The ") && x.ErrorMessage.EndsWith(" field is required.") ? "Se requiere un cuerpo válido en la solicitud."
                        : x.ErrorMessage).ToArray());
            var problema = new ValidationProblemDetails(errores)
            {
                Title = "Uno o más errores de validación.",
                Status = StatusCodes.Status400BadRequest,
                Instance = ctx.HttpContext.Request.Path
            };
            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        };
    });

// CORS: permite que un frontend local (cualquier puerto de localhost) consuma la API
builder.Services.AddCors(options => options.AddPolicy("FrontendLocal", policy =>
    policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
          .AllowAnyHeader()
          .AllowAnyMethod()));

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
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Reservas Coworking v1");
    options.DocumentTitle = "API Reservas Coworking";
    options.InjectJavascript("/swagger-es.js");
});

app.UseHttpsRedirection();

// Frontend de demostración servido por la propia API (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("FrontendLocal");

// se habilitan los permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
