using Coworking.Models;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Repositories;

public interface IReservaRepository
{
    Task<List<Reserva>> GetAllAsync();
    Task<List<Reserva>> GetByUsuarioAsync(int usuarioId);
    Task<Reserva?> GetByIdAsync(int id);
    Task<Reserva> AddAsync(Reserva reserva);
    Task UpdateAsync(Reserva reserva);
    Task<bool> HayConflictoAsync(int espacioId, DateTime inicio, DateTime fin);
    Task<List<Reserva>> GetOcupadasAsync(int espacioId, DateTime desde, DateTime hasta);
}

public class ReservaRepository : IReservaRepository
{
    private static readonly string[] EstadosActivos = { EstadoReserva.Pendiente, EstadoReserva.Confirmada };

    private readonly CoworkingDbContext _context;

    public ReservaRepository(CoworkingDbContext context)
    {
        _context = context;
    }

    private IQueryable<Reserva> Completa() =>
        _context.Reservas
            .Include(r => r.Espacio)
            .Include(r => r.Usuario);

    public Task<List<Reserva>> GetAllAsync() =>
        Completa().AsNoTracking().OrderByDescending(r => r.FechaInicio).ToListAsync();

    public Task<List<Reserva>> GetByUsuarioAsync(int usuarioId) =>
        Completa().AsNoTracking().Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.FechaInicio).ToListAsync();

    public Task<Reserva?> GetByIdAsync(int id) =>
        Completa().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Reserva> AddAsync(Reserva reserva)
    {
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        return reserva;
    }

    public async Task UpdateAsync(Reserva reserva)
    {
        await _context.SaveChangesAsync();
    }

    public Task<bool> HayConflictoAsync(int espacioId, DateTime inicio, DateTime fin) =>
        _context.Reservas.AnyAsync(r =>
            r.EspacioId == espacioId &&
            EstadosActivos.Contains(r.Estado) &&
            r.FechaInicio < fin && r.FechaFin > inicio);

    public Task<List<Reserva>> GetOcupadasAsync(int espacioId, DateTime desde, DateTime hasta) =>
        _context.Reservas.AsNoTracking()
            .Where(r => r.EspacioId == espacioId &&
                        EstadosActivos.Contains(r.Estado) &&
                        r.FechaInicio < hasta && r.FechaFin > desde)
            .OrderBy(r => r.FechaInicio).ToListAsync();
}
