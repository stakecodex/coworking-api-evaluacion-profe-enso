using Asp.Versioning;
using Coworking.Dtos;
using Coworking.Models;
using Coworking.Repositories;
using Coworking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coworking.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize] // bloquea todos los endpoins o apis
    public class EspaciosController : ControllerBase
    {
        private readonly IEspacioRepository _espacios;
        private readonly IReservaService _reservaService;

        public EspaciosController(IEspacioRepository espacios, IReservaService reservaService)
        {
            _espacios = espacios;
            _reservaService = reservaService;
        }

        private static EspacioResponse ToResponse(Espacio e) => new()
        {
            Id = e.Id,
            CategoriaId = e.CategoriaId,
            Categoria = e.Categoria.Nombre,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion,
            Capacidad = e.Capacidad,
            PrecioPorHora = e.PrecioPorHora,
            PisoUbicacion = e.PisoUbicacion,
            Estado = e.Estado,
            FechaCreacion = e.FechaCreacion,
            Servicios = e.Servicios.Select(s => s.Nombre).OrderBy(n => n).ToList()
        };

        private async Task<ActionResult?> ValidarReferenciasAsync(EspacioRequest request, int? excluirId)
        {
            if (!await _espacios.ExisteCategoriaAsync(request.CategoriaId))
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = "La categoría no existe." });

            if (await _espacios.ExisteNombreAsync(request.Nombre.Trim(), excluirId))
                return Conflict(new ProblemDetails { Status = 409, Title = "Conflict", Detail = "Ya existe un espacio con ese nombre." });

            return null;
        }

        /// <summary>Lista los espacios. Los clientes sólo ven los disponibles.</summary>
        // GET: api/v1/Espacios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EspacioResponse>>> GetEspacios()
        {
            var lista = await _espacios.GetAllAsync(soloDisponibles: !User.IsInRole(Roles.Administrador));
            return lista.Select(ToResponse).ToList();
        }

        /// <summary>Lista las categorías de espacio.</summary>
        // GET: api/v1/Espacios/categorias
        [HttpGet("categorias")]
        public async Task<ActionResult<IEnumerable<CategoriaEspacio>>> GetCategorias()
        {
            return await _espacios.GetCategoriasAsync();
        }

        // GET: api/v1/Espacios/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EspacioResponse>> GetEspacio(int id)
        {
            var espacio = await _espacios.GetByIdAsync(id);

            if (espacio == null || (espacio.Estado != EstadoEspacio.Disponible && !User.IsInRole(Roles.Administrador)))
            {
                return NotFound();
            }

            return ToResponse(espacio);
        }

        /// <summary>Consulta si un espacio está libre en un rango de fechas.</summary>
        // GET: api/v1/Espacios/5/disponibilidad?desde=...&hasta=...
        [HttpGet("{id:int}/disponibilidad")]
        public async Task<ActionResult<DisponibilidadResponse>> GetDisponibilidad(int id, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            return await _reservaService.DisponibilidadAsync(id, desde, hasta);
        }

        // PUT: api/v1/Espacios/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> PutEspacio(int id, EspacioRequest request)
        {
            var espacio = await _espacios.GetByIdAsync(id);
            if (espacio == null)
            {
                return NotFound();
            }

            var error = await ValidarReferenciasAsync(request, id);
            if (error != null) return error;

            espacio.CategoriaId = request.CategoriaId;
            espacio.Nombre = request.Nombre.Trim();
            espacio.Descripcion = request.Descripcion;
            espacio.Capacidad = request.Capacidad;
            espacio.PrecioPorHora = request.PrecioPorHora;
            espacio.PisoUbicacion = request.PisoUbicacion;
            espacio.Estado = request.Estado;
            espacio.Servicios = await _espacios.GetServiciosAsync(request.ServicioIds);
            await _espacios.UpdateAsync(espacio);

            return NoContent();
        }

        // POST: api/v1/Espacios
        [HttpPost]
        [Authorize(Roles = Roles.Administrador)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<EspacioResponse>> PostEspacio(EspacioRequest request)
        {
            var error = await ValidarReferenciasAsync(request, null);
            if (error != null) return error;

            var espacio = new Espacio
            {
                CategoriaId = request.CategoriaId,
                Nombre = request.Nombre.Trim(),
                Descripcion = request.Descripcion,
                Capacidad = request.Capacidad,
                PrecioPorHora = request.PrecioPorHora,
                PisoUbicacion = request.PisoUbicacion,
                Estado = request.Estado,
                FechaCreacion = DateTime.UtcNow,
                Servicios = await _espacios.GetServiciosAsync(request.ServicioIds)
            };
            await _espacios.AddAsync(espacio);

            var creado = await _espacios.GetByIdAsync(espacio.Id);
            return CreatedAtAction(nameof(GetEspacio), new { version = "1.0", id = espacio.Id }, ToResponse(creado!));
        }

        // DELETE: api/v1/Espacios/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> DeleteEspacio(int id)
        {
            var espacio = await _espacios.GetByIdAsync(id);
            if (espacio == null)
            {
                return NotFound();
            }

            if (await _espacios.TieneReservasAsync(id))
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "El espacio tiene reservas asociadas; póngalo en Mantenimiento en lugar de eliminarlo."
                });

            await _espacios.DeleteAsync(espacio);

            return NoContent();
        }
    }
}
