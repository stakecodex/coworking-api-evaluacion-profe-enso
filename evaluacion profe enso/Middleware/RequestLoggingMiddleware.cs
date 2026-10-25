using System.Diagnostics;
using Coworking.Models;

namespace Coworking.Middleware;

// Registra cada request en consola y en la tabla LogsAuditoria
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("{Metodo} {Ruta} -> {Estado} en {Ms} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);

            if (context.Request.Path.StartsWithSegments("/api"))
                await GuardarAsync(context, (int)sw.ElapsedMilliseconds);
        }
    }

    private async Task GuardarAsync(HttpContext context, int ms)
    {
        try
        {
            int? usuarioId = int.TryParse(context.User.FindFirst("uid")?.Value, out var id) ? id : null;

            // Scope propio para no depender del estado del DbContext de la request
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoworkingDbContext>();
            db.LogsAuditoria.Add(new LogAuditoria
            {
                UsuarioId = usuarioId,
                MetodoHttp = context.Request.Method,
                RutaEndpoint = context.Request.Path.Value![..Math.Min(255, context.Request.Path.Value!.Length)],
                CodigoEstadoHttp = context.Response.StatusCode,
                IpOrigen = context.Connection.RemoteIpAddress?.ToString(),
                TiempoRespuestaMs = ms,
                FechaRegistro = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar el log de auditoría");
        }
    }
}
