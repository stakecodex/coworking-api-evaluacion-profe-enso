using System.ComponentModel.DataAnnotations;

namespace Coworking.Dtos;

public class LoginDto
{
    [Display(Name = "correo")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), EmailAddress(ErrorMessage = "El {0} no tiene un formato válido.")]
    public string Email { get; set; } = null!;

    [Display(Name = "contraseña")]
    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    public string Password { get; set; } = null!;
}

public class RegistroDto
{
    [Display(Name = "nombre")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), StringLength(100, MinimumLength = 2, ErrorMessage = "El campo {0} debe tener entre {2} y {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "apellido")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), StringLength(100, MinimumLength = 2, ErrorMessage = "El campo {0} debe tener entre {2} y {1} caracteres.")]
    public string Apellido { get; set; } = null!;

    [Display(Name = "correo")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), EmailAddress(ErrorMessage = "El {0} no tiene un formato válido."), StringLength(120, ErrorMessage = "El campo {0} no puede superar {1} caracteres.")]
    public string Email { get; set; } = null!;

    [Display(Name = "contraseña")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), StringLength(72, MinimumLength = 8, ErrorMessage = "La {0} debe tener entre {2} y {1} caracteres.")]
    public string Password { get; set; } = null!;

    [Display(Name = "teléfono")]
    [StringLength(20, ErrorMessage = "El campo {0} no puede superar {1} caracteres.")]
    public string? Telefono { get; set; }
}

public class EspacioRequest
{
    [Display(Name = "categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una {0} válida.")]
    public int CategoriaId { get; set; }

    [Display(Name = "nombre")]
    [Required(ErrorMessage = "El campo {0} es obligatorio."), StringLength(100, MinimumLength = 2, ErrorMessage = "El campo {0} debe tener entre {2} y {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "descripción")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar {1} caracteres.")]
    public string? Descripcion { get; set; }

    [Display(Name = "capacidad")]
    [Range(1, 200, ErrorMessage = "La {0} debe estar entre {1} y {2}.")]
    public int Capacidad { get; set; }

    [Display(Name = "precio por hora")]
    [Range(0.01, 1000000, ErrorMessage = "El {0} debe estar entre {1} y {2}.")]
    public decimal PrecioPorHora { get; set; }

    [Display(Name = "piso")]
    [StringLength(20, ErrorMessage = "El campo {0} no puede superar {1} caracteres.")]
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
    [Display(Name = "espacio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un {0} válido.")]
    public int EspacioId { get; set; }

    [Display(Name = "fecha de inicio")]
    [Required(ErrorMessage = "La {0} es obligatoria.")]
    public DateTime? FechaInicio { get; set; }

    [Display(Name = "fecha de fin")]
    [Required(ErrorMessage = "La {0} es obligatoria.")]
    public DateTime? FechaFin { get; set; }

    [Display(Name = "notas")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar {1} caracteres.")]
    public string? NotasAdicionales { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin <= FechaInicio)
            yield return new ValidationResult("La fecha de fin debe ser posterior a la fecha de inicio.", new[] { nameof(FechaFin) });
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
