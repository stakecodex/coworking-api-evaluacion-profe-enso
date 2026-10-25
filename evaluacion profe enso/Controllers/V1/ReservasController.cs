using System.Security.Claims;
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
    [Authorize]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaRepository _reservas;
        private readonly IReservaService _service;

        public ReservasController(IReservaRepository reservas, IReservaService service)
        {
            _reservas = reservas;
            _service = service;
        }

        private int UserId => int.Parse(User.FindFirstValue("uid")!);

        private static ReservaResponse ToResponse(Reserva r) => new()
        {
            Id = r.Id,
            EspacioId = r.EspacioId,
            Espacio = r.Espacio.Nombre,
            UsuarioId = r.UsuarioId,
            Usuario = $"{r.Usuario.Nombre} {r.Usuario.Apellido}",
            FechaInicio = r.FechaInicio,
            FechaFin = r.FechaFin,
            MontoTotal = r.MontoTotal,
            Estado = r.Estado,
            NotasAdicionales = r.NotasAdicionales,
            FechaCreacion = r.FechaCreacion
        };

        /// <summary>Lista todas las reservas (sólo administrador).</summary>
        // GET: api/v1/Reservas
        [HttpGet]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<ActionResult<IEnumerable<ReservaResponse>>> GetReservas()
        {
            return (await _reservas.GetAllAsync()).Select(ToResponse).ToList();
        }

        /// <summary>Historial de reservas del usuario autenticado.</summary>
        // GET: api/v1/Reservas/mias
        [HttpGet("mias")]
        public async Task<ActionResult<IEnumerable<ReservaResponse>>> GetMisReservas()
        {
            return (await _reservas.GetByUsuarioAsync(UserId)).Select(ToResponse).ToList();
        }

        // GET: api/v1/Reservas/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservaResponse>> GetReserva(int id)
        {
            var reserva = await _reservas.GetByIdAsync(id);

            if (reserva == null || (reserva.UsuarioId != UserId && !User.IsInRole(Roles.Administrador)))
            {
                return NotFound();
            }

            return ToResponse(reserva);
        }

        /// <summary>Crea una reserva en estado Pendiente (sólo clientes).</summary>
        // POST: api/v1/Reservas
        [HttpPost]
        [Authorize(Roles = Roles.Cliente)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReservaResponse>> PostReserva(ReservaRequest request)
        {
            var reserva = await _service.CrearAsync(request, UserId);
            return CreatedAtAction(nameof(GetReserva), new { version = "1.0", id = reserva.Id }, ToResponse(reserva));
        }

        /// <summary>Aprueba una reserva pendiente (sólo administrador).</summary>
        // PUT: api/v1/Reservas/5/confirmar
        [HttpPut("{id:int}/confirmar")]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<ActionResult<ReservaResponse>> ConfirmarReserva(int id)
        {
            return ToResponse(await _service.ConfirmarAsync(id));
        }

        /// <summary>Cancela una reserva. El administrador puede cancelar cualquiera.</summary>
        // DELETE: api/v1/Reservas/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            await _service.CancelarAsync(id, UserId, User.IsInRole(Roles.Administrador));
            return NoContent();
        }
    }
}
