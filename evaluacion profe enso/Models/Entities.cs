using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Coworking.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cliente = "Cliente";
}

public static class EstadoReserva
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Cancelada = "Cancelada";
}

public static class EstadoEspacio
{
    public const string Disponible = "Disponible";
    public const string Mantenimiento = "Mantenimiento";
    public static readonly string[] Validos = { Disponible, Mantenimiento };
}

public partial class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
    [JsonIgnore]
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Email { get; set; } = null!;

    [JsonIgnore]
    public string PasswordHash { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Estado { get; set; } = "Activo";

    public DateTime FechaRegistro { get; set; }

    public int RolId { get; set; }
    public virtual Rol? Rol { get; set; } = null;
    [JsonIgnore]
    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

public partial class CategoriaEspacio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
    [JsonIgnore]
    public virtual ICollection<Espacio> Espacios { get; set; } = new List<Espacio>();
}

public partial class Servicio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
    [JsonIgnore]
    public virtual ICollection<Espacio> Espacios { get; set; } = new List<Espacio>();
}

public partial class Espacio
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int Capacidad { get; set; }

    public decimal PrecioPorHora { get; set; }

    public string? PisoUbicacion { get; set; }

    public string Estado { get; set; } = EstadoEspacio.Disponible;

    public DateTime FechaCreacion { get; set; }

    public virtual CategoriaEspacio Categoria { get; set; } = null!;
    public virtual ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    [JsonIgnore]
    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

public partial class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int EspacioId { get; set; }

    // Fechas almacenadas en UTC
    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoTotal { get; set; }

    public string Estado { get; set; } = EstadoReserva.Pendiente;

    public string? NotasAdicionales { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Espacio Espacio { get; set; } = null!;

    public virtual Pago? Pago { get; set; }
}

public partial class Pago
{
    public int Id { get; set; }

    public int ReservaId { get; set; }

    public decimal Monto { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string Estado { get; set; } = "Aprobado";

    public DateTime FechaPago { get; set; }

    [JsonIgnore]
    public virtual Reserva Reserva { get; set; } = null!;
}

public partial class LogAuditoria
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }

    public string MetodoHttp { get; set; } = null!;

    public string RutaEndpoint { get; set; } = null!;

    public int CodigoEstadoHttp { get; set; }

    public string? IpOrigen { get; set; }

    public int TiempoRespuestaMs { get; set; }

    public DateTime FechaRegistro { get; set; }
}
