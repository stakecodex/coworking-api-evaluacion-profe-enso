using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Coworking.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Admin_DevuelveToken()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/User/login",
            new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_Devuelve401()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/User/login",
            new { email = ApiFactory.AdminEmail, password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Registro_CreaCliente_YEmailDuplicadoDevuelve409()
    {
        var client = _factory.CreateClient();
        var body = new { nombre = "Ana", apellido = "Pérez", email = "ana@test.com", password = "Password123" };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/User", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/User", body)).StatusCode);
    }

    [Fact]
    public async Task Registro_DatosInvalidos_Devuelve400()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/User",
            new { nombre = "A", apellido = "", email = "no-es-email", password = "corta" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Me_DevuelveRolDelUsuario()
    {
        var admin = await _factory.AdminAsync();
        var json = await admin.GetFromJsonAsync<JsonElement>("/api/v1/User/me");

        Assert.Equal("Administrador", json.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task EndpointProtegido_SinToken_Devuelve401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/v1/Espacios");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task RutaSinVersion_Devuelve404()
    {
        var admin = await _factory.AdminAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/Espacios")).StatusCode);
    }

    [Fact]
    public async Task Swagger_EstaDisponible()
    {
        var resp = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("/api/v1/Espacios", await resp.Content.ReadAsStringAsync());
    }
}
