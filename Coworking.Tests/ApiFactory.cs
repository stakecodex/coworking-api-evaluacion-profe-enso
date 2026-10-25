using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Coworking.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@coworking.test";
    public const string AdminPassword = "Admin12345!";

    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "clave-de-pruebas-de-integracion-0123456789");
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
    }

    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var viejos = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<CoworkingDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();
            foreach (var d in viejos)
                services.Remove(d);

            services.AddDbContext<CoworkingDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task<HttpClient> ClienteAutenticadoAsync(string email, string password)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/User/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
        return client;
    }

    public Task<HttpClient> AdminAsync() => ClienteAutenticadoAsync(AdminEmail, AdminPassword);

    public async Task<HttpClient> NuevoClienteAsync()
    {
        var email = $"cliente{Guid.NewGuid():N}@test.com";
        var reg = await CreateClient().PostAsJsonAsync("/api/v1/User",
            new { nombre = "Cliente Test", apellido = "Prueba", email, password = "Password123" });
        reg.EnsureSuccessStatusCode();
        return await ClienteAutenticadoAsync(email, "Password123");
    }
}
