namespace Coworking.Middleware;

// Textos en español para las respuestas de error HTTP
public static class Textos
{
    public static string Titulo(int status) => status switch
    {
        400 => "Solicitud incorrecta",
        401 => "No autorizado",
        403 => "Acceso denegado",
        404 => "No encontrado",
        405 => "Método no permitido",
        409 => "Conflicto",
        415 => "Tipo de contenido no admitido",
        422 => "Entidad no procesable",
        500 => "Error interno del servidor",
        _ => "Error"
    };
}
