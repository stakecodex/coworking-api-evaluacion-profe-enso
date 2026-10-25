using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Coworking.Tests;

public class ReservasTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ReservasTests(ApiFactory factory) => _factory = factory;

    private async Task<int> CrearEspacioAsync(string nombre, decimal precio = 10000)
    {
        var admin = await _factory.AdminAsync();
        var resp = await admin.PostAsJsonAsync("/api/v1/Espacios",
            new { categoriaId = 1, nombre, capacidad = 6, precioPorHora = precio, estado = "Disponible" });
        return (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> Reservar(HttpClient c, int espacioId, DateTime inicio, int horas) =>
        c.PostAsJsonAsync("/api/v1/Reservas", new { espacioId, fechaInicio = inicio, fechaFin = inicio.AddHours(horas) });

    private static async Task<int> IdDe(HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    [Fact]
    public async Task Cliente_CreaReserva_PendienteYCalculaTotal()
    {
        var id = await CrearEspacioAsync("Sala Total", 10000);
        var cliente = await _factory.NuevoClienteAsync();

        var resp = await Reservar(cliente, id, DateTime.UtcNow.AddDays(1), 2);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20000m, json.GetProperty("montoTotal").GetDecimal());
        Assert.Equal("Pendiente", json.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Admin_ApruebaReserva_UnaSolaVez()
    {
        var id = await CrearEspacioAsync("Sala Aprobacion");
        var cliente = await _factory.NuevoClienteAsync();
        var admin = await _factory.AdminAsync();
        var idReserva = await IdDe(await Reservar(cliente, id, DateTime.UtcNow.AddDays(1), 1));

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsync($"/api/v1/Reservas/{idReserva}/confirmar", null)).StatusCode);

        var ok = await admin.PutAsync($"/api/v1/Reservas/{idReserva}/confirmar", null);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Confirmada", (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsync($"/api/v1/Reservas/{idReserva}/confirmar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsync("/api/v1/Reservas/99999/confirmar", null)).StatusCode);
    }

    [Fact]
    public async Task Reserva_Solapada_Devuelve409()
    {
        var id = await CrearEspacioAsync("Sala Solape");
        var cliente = await _factory.NuevoClienteAsync();
        var inicio = DateTime.UtcNow.AddDays(2);
        await Reservar(cliente, id, inicio, 2);

        var otro = await _factory.NuevoClienteAsync();
        var resp = await Reservar(otro, id, inicio.AddHours(1), 2);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        // Un bloque contiguo no se solapa
        Assert.Equal(HttpStatusCode.Created, (await Reservar(otro, id, inicio.AddHours(2), 1)).StatusCode);
    }

    [Fact]
    public async Task Reserva_EnElPasado_Devuelve400()
    {
        var id = await CrearEspacioAsync("Sala Pasado");
        var cliente = await _factory.NuevoClienteAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Reservar(cliente, id, DateTime.UtcNow.AddDays(-1), 1)).StatusCode);
    }

    [Fact]
    public async Task Reserva_FechaFinAnteriorAInicio_Devuelve400()
    {
        var id = await CrearEspacioAsync("Sala Fechas");
        var cliente = await _factory.NuevoClienteAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Reservar(cliente, id, DateTime.UtcNow.AddDays(1), -1)).StatusCode);
    }

    [Fact]
    public async Task Reserva_MasDe8Horas_Devuelve400()
    {
        var id = await CrearEspacioAsync("Sala Larga");
        var cliente = await _factory.NuevoClienteAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Reservar(cliente, id, DateTime.UtcNow.AddDays(1), 9)).StatusCode);
    }

    [Fact]
    public async Task Reserva_EspacioInexistente_Devuelve404()
    {
        var cliente = await _factory.NuevoClienteAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await Reservar(cliente, 99999, DateTime.UtcNow.AddDays(1), 1)).StatusCode);
    }

    [Fact]
    public async Task Admin_NoPuedeReservar_Devuelve403()
    {
        var id = await CrearEspacioAsync("Sala Admin");
        var admin = await _factory.AdminAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await Reservar(admin, id, DateTime.UtcNow.AddDays(1), 1)).StatusCode);
    }

    [Fact]
    public async Task Historial_SoloMuestraReservasPropias()
    {
        var id = await CrearEspacioAsync("Sala Historial");
        var a = await _factory.NuevoClienteAsync();
        var b = await _factory.NuevoClienteAsync();
        await Reservar(a, id, DateTime.UtcNow.AddDays(5), 1);

        var deA = await a.GetFromJsonAsync<JsonElement>("/api/v1/Reservas/mias");
        var deB = await b.GetFromJsonAsync<JsonElement>("/api/v1/Reservas/mias");

        Assert.Equal(1, deA.GetArrayLength());
        Assert.Equal(0, deB.GetArrayLength());
    }

    [Fact]
    public async Task ListadoGeneral_SoloAdmin()
    {
        var cliente = await _factory.NuevoClienteAsync();
        var admin = await _factory.AdminAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/Reservas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/Reservas")).StatusCode);
    }

    [Fact]
    public async Task ReservaAjena_NoEsVisibleNiCancelablePorOtroCliente()
    {
        var id = await CrearEspacioAsync("Sala Ajena");
        var duenio = await _factory.NuevoClienteAsync();
        var intruso = await _factory.NuevoClienteAsync();
        var idReserva = await IdDe(await Reservar(duenio, id, DateTime.UtcNow.AddDays(4), 1));

        Assert.Equal(HttpStatusCode.NotFound, (await intruso.GetAsync($"/api/v1/Reservas/{idReserva}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await intruso.DeleteAsync($"/api/v1/Reservas/{idReserva}")).StatusCode);
    }

    [Fact]
    public async Task Cancelar_LiberaElHorario()
    {
        var id = await CrearEspacioAsync("Sala Cancelar");
        var cliente = await _factory.NuevoClienteAsync();
        var inicio = DateTime.UtcNow.AddDays(6);
        var idReserva = await IdDe(await Reservar(cliente, id, inicio, 2));

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/v1/Reservas/{idReserva}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/v1/Reservas/{idReserva}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Reservar(cliente, id, inicio, 2)).StatusCode);
    }

    [Fact]
    public async Task Admin_PuedeCancelarCualquierReserva()
    {
        var id = await CrearEspacioAsync("Sala Admin Cancela");
        var cliente = await _factory.NuevoClienteAsync();
        var admin = await _factory.AdminAsync();
        var idReserva = await IdDe(await Reservar(cliente, id, DateTime.UtcNow.AddDays(7), 1));

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/Reservas/{idReserva}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/v1/Reservas/99999")).StatusCode);
    }

    [Fact]
    public async Task Disponibilidad_ReflejaReservas()
    {
        var id = await CrearEspacioAsync("Sala Disponibilidad");
        var cliente = await _factory.NuevoClienteAsync();
        var inicio = DateTime.UtcNow.Date.AddDays(10).AddHours(9);
        var desde = Uri.EscapeDataString(inicio.ToString("o"));
        var hasta = Uri.EscapeDataString(inicio.AddHours(4).ToString("o"));

        var antes = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/Espacios/{id}/disponibilidad?desde={desde}&hasta={hasta}");
        Assert.True(antes.GetProperty("disponible").GetBoolean());

        await Reservar(cliente, id, inicio.AddHours(1), 1);

        var despues = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/Espacios/{id}/disponibilidad?desde={desde}&hasta={hasta}");
        Assert.False(despues.GetProperty("disponible").GetBoolean());
        Assert.Equal(1, despues.GetProperty("ocupado").GetArrayLength());
    }
}
