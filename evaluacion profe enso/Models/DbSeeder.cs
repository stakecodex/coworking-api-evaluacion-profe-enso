using Microsoft.EntityFrameworkCore;

namespace Coworking.Models;

public static class DbSeeder
{
    // Aplica migraciones y asegura roles, categorías y (opcional) el administrador inicial
    public static async Task SeedAsync(CoworkingDbContext context, IConfiguration config)
    {
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        foreach (var nombre in new[] { Roles.Administrador, Roles.Cliente })
        {
            if (!await context.Roles.AnyAsync(r => r.Nombre == nombre))
                context.Roles.Add(new Rol { Nombre = nombre, Descripcion = $"Rol {nombre}" });
        }

        foreach (var nombre in new[] { "Sala de reuniones", "Escritorio" })
        {
            if (!await context.CategoriasEspacio.AnyAsync(c => c.Nombre == nombre))
                context.CategoriasEspacio.Add(new CategoriaEspacio { Nombre = nombre });
        }
        await context.SaveChangesAsync();

        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (!await context.Usuarios.AnyAsync(u => u.Email == email))
        {
            var rolAdmin = await context.Roles.SingleAsync(r => r.Nombre == Roles.Administrador);
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Administrador",
                Apellido = "Sistema",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RolId = rolAdmin.Id
            });
            await context.SaveChangesAsync();
        }
    }
}
