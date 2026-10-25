using Coworking.Models;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email);
    Task<Usuario?> GetByIdAsync(int id);
    Task<bool> ExisteEmailAsync(string email);
    Task<Rol?> GetRolPorNombreAsync(string nombre);
    Task<Usuario> AddAsync(Usuario usuario);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly CoworkingDbContext _context;

    public UsuarioRepository(CoworkingDbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> GetByEmailAsync(string email) =>
        _context.Usuarios.Include(u => u.Rol).SingleOrDefaultAsync(u => u.Email == email);

    public Task<Usuario?> GetByIdAsync(int id) =>
        _context.Usuarios.Include(u => u.Rol).SingleOrDefaultAsync(u => u.Id == id);

    public Task<bool> ExisteEmailAsync(string email) =>
        _context.Usuarios.AnyAsync(u => u.Email == email);

    public Task<Rol?> GetRolPorNombreAsync(string nombre) =>
        _context.Roles.SingleOrDefaultAsync(r => r.Nombre == nombre);

    public async Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
}
