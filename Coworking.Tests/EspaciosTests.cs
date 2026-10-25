using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Coworking.Tests;

public class EspaciosTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EspaciosTests(ApiFactory factory) => _factory = factory;

    private static object Sala(string nombre, string estado = "Disponible") => new
    {
        categoriaId = 1, nombre, capacidad = 8, precioPorHora = 15000, descripcion = "Sala con proyector", pisoUbicacion = "2", estado
    };

    [Fact]
    public async Task Admin_CrudCompleto()
    {
        var admin = await _factory.AdminAsync();

        var crear = await admin.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala CRUD"));
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        Assert.NotNull(crear.Headers.Location);
        var id = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/Espacios/{id}")).StatusCode);

        var editar = await admin.PutAsJsonAsync($"/api/v1/Espacios/{id}",
            new { categoriaId = 1, nombre = "Sala CRUD v2", capacidad = 10, precioPorHora = 20000, estado = "Disponible" });
        Assert.Equal(HttpStatusCode.NoContent, editar.StatusCode);

        var leido = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/Espacios/{id}");
        Assert.Equal("Sala CRUD v2", leido.GetProperty("nombre").GetString());
        Assert.Equal("Sala de reuniones", leido.GetProperty("categoria").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/Espacios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/Espacios/{id}")).StatusCode);
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_Devuelve400()
    {
        var admin = await _factory.AdminAsync();

        var resp = await admin.PostAsJsonAsync("/api/v1/Espacios",
            new { categoriaId = 1, nombre = "", estado = "Roto", capacidad = 0, precioPorHora = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Crear_CategoriaInexistente_Devuelve400()
    {
        var admin = await _factory.AdminAsync();

        var resp = await admin.PostAsJsonAsync("/api/v1/Espacios",
            new { categoriaId = 999, nombre = "Sala sin categoria", capacidad = 4, precioPorHora = 3000 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Crear_NombreDuplicado_Devuelve409()
    {
        var admin = await _factory.AdminAsync();
        await admin.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala Duplicada"));

        var resp = await admin.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala Duplicada"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Cliente_NoPuedeCrearNiEliminar_Devuelve403()
    {
        var cliente = await _factory.NuevoClienteAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync("/api/v1/Espacios/1")).StatusCode);
    }

    [Fact]
    public async Task Cliente_SoloVeEspaciosDisponibles()
    {
        var admin = await _factory.AdminAsync();
        var creado = await admin.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala En Mantenimiento", "Mantenimiento"));
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var cliente = await _factory.NuevoClienteAsync();
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/Espacios");

        Assert.DoesNotContain(lista.EnumerateArray(), e => e.GetProperty("id").GetInt32() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/Espacios/{id}")).StatusCode);
    }

    [Fact]
    public async Task Eliminar_ConReservas_Devuelve409()
    {
        var admin = await _factory.AdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/Espacios", Sala("Sala Con Reservas"));
        var id = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var cliente = await _factory.NuevoClienteAsync();
        var inicio = DateTime.UtcNow.AddDays(3);
        await cliente.PostAsJsonAsync("/api/v1/Reservas",
            new { espacioId = id, fechaInicio = inicio, fechaFin = inicio.AddHours(1) });

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/Espacios/{id}")).StatusCode);
    }

    [Fact]
    public async Task Categorias_DevuelveListadoInicial()
    {
        var cliente = await _factory.NuevoClienteAsync();

        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/Espacios/categorias");

        Assert.True(lista.GetArrayLength() >= 2);
    }
}
