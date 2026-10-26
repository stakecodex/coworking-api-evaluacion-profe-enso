using Coworking.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coworking.Middleware;

// Convierte las excepciones en respuestas ProblemDetails (RFC 7807)
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ReglaNegocioException ex)
        {
            await Responder(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);
            await Responder(context, StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.");
        }
    }

    private static Task Responder(HttpContext context, int status, string detalle)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = Textos.Titulo(status),
            Detail = detalle,
            Instance = context.Request.Path
        };
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}

