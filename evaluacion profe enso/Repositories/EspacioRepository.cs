using Coworking.Models;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Repositories;

public interface IEspacioRepository
{
    Task<List<Espacio>> GetAllAsync(bool soloDisponibles);
    Task<Espacio?> GetByIdAsync(int id);
    Task<Espacio> AddAsync(Espacio espacio);
    Task UpdateAsync(Espacio espacio);
    Task DeleteAsync(Espacio espacio);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task<bool> TieneReservasAsync(int id);
    Task<bool> ExisteCategoriaAsync(int categoriaId);
    Task<List<Servicio>> GetServiciosAsync(IEnumerable<int> ids);
    Task<List<CategoriaEspacio>> GetCategoriasAsync();
}

public class EspacioRepository : IEspacioRepository
{
    private readonly CoworkingDbContext _context;

    public EspacioRepository(CoworkingDbContext context)
    {
        _context = context;
    }

    private IQueryable<Espacio> Completo() =>
        _context.Espacios.Include(e => e.Categoria).Include(e => e.Servicios);

    public async Task<List<Espacio>> GetAllAsync(bool soloDisponibles)
    {
        var query = Completo().AsNoTracking();
        if (soloDisponibles)
            query = query.Where(e => e.Estado == EstadoEspacio.Disponible);
        return await query.OrderBy(e => e.Nombre).ToListAsync();
    }

    public Task<Espacio?> GetByIdAsync(int id) =>
        Completo().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Espacio> AddAsync(Espacio espacio)
    {
        _context.Espacios.Add(espacio);
        await _context.SaveChangesAsync();
        return espacio;
    }

    public async Task UpdateAsync(Espacio espacio)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Espacio espacio)
    {
        _context.Espacios.Remove(espacio);
        await _context.SaveChangesAsync();
    }

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        _context.Espacios.AnyAsync(e => e.Nombre == nombre && e.Id != excluirId);

    public Task<bool> TieneReservasAsync(int id) =>
        _context.Reservas.AnyAsync(r => r.EspacioId == id);

    public Task<bool> ExisteCategoriaAsync(int categoriaId) =>
        _context.CategoriasEspacio.AnyAsync(c => c.Id == categoriaId);

    public Task<List<Servicio>> GetServiciosAsync(IEnumerable<int> ids) =>
        _context.Servicios.Where(s => ids.Contains(s.Id)).ToListAsync();

    public Task<List<CategoriaEspacio>> GetCategoriasAsync() =>
        _context.CategoriasEspacio.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
}
