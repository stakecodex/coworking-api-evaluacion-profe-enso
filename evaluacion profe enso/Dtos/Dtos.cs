using System.ComponentModel.DataAnnotations;

namespace Coworking.Dtos;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class RegistroDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellido { get; set; } = null!;

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = null!;

    [Required, StringLength(72, MinimumLength = 8)]
    public string Password { get; set; } = null!;

    [StringLength(20)]
    public string? Telefono { get; set; }
}

public class EspacioRequest
{
    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(1, 200)]
    public int Capacidad { get; set; }

    [Range(0.01, 1000000)]
    public decimal PrecioPorHora { get; set; }

    [StringLength(20)]
    public string? PisoUbicacion { get; set; }

    [RegularExpression("^(Disponible|Mantenimiento)$", ErrorMessage = "El estado debe ser 'Disponible' o 'Mantenimiento'.")]
    public string Estado { get; set; } = "Disponible";

    public List<int> ServicioIds { get; set; } = new();
}

public class EspacioResponse
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int Capacidad { get; set; }
    public decimal PrecioPorHora { get; set; }
    public string? PisoUbicacion { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public IEnumerable<string> Servicios { get; set; } = Array.Empty<string>();
}

public class ReservaRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int EspacioId { get; set; }

    [Required]
    public DateTime? FechaInicio { get; set; }

    [Required]
    public DateTime? FechaFin { get; set; }

    [StringLength(500)]
    public string? NotasAdicionales { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin <= FechaInicio)
            yield return new ValidationResult("FechaFin debe ser posterior a FechaInicio.", new[] { nameof(FechaFin) });
    }
}

public class ReservaResponse
{
    public int Id { get; set; }
    public int EspacioId { get; set; }
    public string Espacio { get; set; } = null!;
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal MontoTotal { get; set; }
    public string Estado { get; set; } = null!;
    public string? NotasAdicionales { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class DisponibilidadResponse
{
    public int EspacioId { get; set; }
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public bool Disponible { get; set; }
    public IEnumerable<BloqueOcupado> Ocupado { get; set; } = Array.Empty<BloqueOcupado>();
}

public record BloqueOcupado(DateTime FechaInicio, DateTime FechaFin);
