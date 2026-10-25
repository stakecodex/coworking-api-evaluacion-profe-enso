using Coworking.Dtos;
using Coworking.Models;
using Coworking.Repositories;

namespace Coworking.Services;

public class ReglaNegocioException : Exception
{
    public int StatusCode { get; }

    public ReglaNegocioException(int statusCode, string mensaje) : base(mensaje)
    {
        StatusCode = statusCode;
    }
}

public interface IReservaService
{
    Task<Reserva> CrearAsync(ReservaRequest request, int usuarioId);
    Task<Reserva> ConfirmarAsync(int reservaId);
    Task CancelarAsync(int reservaId, int usuarioId, bool esAdmin);
    Task<DisponibilidadResponse> DisponibilidadAsync(int espacioId, DateTime desde, DateTime hasta);
}

public class ReservaService : IReservaService
{
    public const int MaxHorasPorReserva = 8;

    private readonly IReservaRepository _reservas;
    private readonly IEspacioRepository _espacios;

    public ReservaService(IReservaRepository reservas, IEspacioRepository espacios)
    {
        _reservas = reservas;
        _espacios = espacios;
    }

    public async Task<Reserva> CrearAsync(ReservaRequest request, int usuarioId)
    {
        var inicio = request.FechaInicio!.Value.ToUniversalTime();
        var fin = request.FechaFin!.Value.ToUniversalTime();

        if (inicio <= DateTime.UtcNow)
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La reserva debe comenzar en el futuro.");

        var horas = (decimal)(fin - inicio).TotalHours;
        if (horas > MaxHorasPorReserva)
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest,
                $"Una reserva no puede superar las {MaxHorasPorReserva} horas.");

        var espacio = await _espacios.GetByIdAsync(request.EspacioId)
            ?? throw new ReglaNegocioException(StatusCodes.Status404NotFound, "El espacio no existe.");

        if (espacio.Estado != EstadoEspacio.Disponible)
            throw new ReglaNegocioException(StatusCodes.Status409Conflict, "El espacio no está disponible para reservas.");

        if (await _reservas.HayConflictoAsync(espacio.Id, inicio, fin))
            throw new ReglaNegocioException(StatusCodes.Status409Conflict,
                "El espacio ya está reservado en el horario solicitado.");

        var reserva = new Reserva
        {
            EspacioId = espacio.Id,
            UsuarioId = usuarioId,
            FechaInicio = inicio,
            FechaFin = fin,
            MontoTotal = Math.Round(horas * espacio.PrecioPorHora, 2),
            Estado = EstadoReserva.Pendiente,
            NotasAdicionales = request.NotasAdicionales,
            FechaCreacion = DateTime.UtcNow
        };

        await _reservas.AddAsync(reserva);
        return (await _reservas.GetByIdAsync(reserva.Id))!;
    }

    public async Task<Reserva> ConfirmarAsync(int reservaId)
    {
        var reserva = await _reservas.GetByIdAsync(reservaId)
            ?? throw new ReglaNegocioException(StatusCodes.Status404NotFound, "La reserva no existe.");

        if (reserva.Estado != EstadoReserva.Pendiente)
            throw new ReglaNegocioException(StatusCodes.Status409Conflict, "Sólo se pueden aprobar reservas pendientes.");

        reserva.Estado = EstadoReserva.Confirmada;
        await _reservas.UpdateAsync(reserva);
        return reserva;
    }

    public async Task CancelarAsync(int reservaId, int usuarioId, bool esAdmin)
    {
        var reserva = await _reservas.GetByIdAsync(reservaId)
            ?? throw new ReglaNegocioException(StatusCodes.Status404NotFound, "La reserva no existe.");

        if (!esAdmin && reserva.UsuarioId != usuarioId)
            throw new ReglaNegocioException(StatusCodes.Status403Forbidden, "No puede cancelar reservas de otros usuarios.");

        if (reserva.Estado == EstadoReserva.Cancelada)
            throw new ReglaNegocioException(StatusCodes.Status409Conflict, "La reserva ya está cancelada.");

        if (!esAdmin && reserva.FechaInicio <= DateTime.UtcNow)
            throw new ReglaNegocioException(StatusCodes.Status409Conflict, "No se puede cancelar una reserva ya iniciada.");

        reserva.Estado = EstadoReserva.Cancelada;
        await _reservas.UpdateAsync(reserva);
    }

    public async Task<DisponibilidadResponse> DisponibilidadAsync(int espacioId, DateTime desde, DateTime hasta)
    {
        var espacio = await _espacios.GetByIdAsync(espacioId)
            ?? throw new ReglaNegocioException(StatusCodes.Status404NotFound, "El espacio no existe.");

        var d = desde.ToUniversalTime();
        var h = hasta.ToUniversalTime();
        if (h <= d)
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "'hasta' debe ser posterior a 'desde'.");

        var ocupadas = await _reservas.GetOcupadasAsync(espacioId, d, h);
        return new DisponibilidadResponse
        {
            EspacioId = espacioId,
            Desde = d,
            Hasta = h,
            Disponible = espacio.Estado == EstadoEspacio.Disponible && ocupadas.Count == 0,
            Ocupado = ocupadas.Select(r => new BloqueOcupado(r.FechaInicio, r.FechaFin)).ToList()
        };
    }
}
