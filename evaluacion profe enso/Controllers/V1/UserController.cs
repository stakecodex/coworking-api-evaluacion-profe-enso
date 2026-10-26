using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Coworking.Dtos;
using Coworking.Models;
using Coworking.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Coworking.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly IConfiguration _config;
        public UserController(IUsuarioRepository usuarios, IConfiguration config)
        {
            _usuarios = usuarios;
            _config = config;
        }

        /// <summary>Registra un nuevo usuario con rol Cliente.</summary>
        // POST: api/v1/User
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PostUsuario(RegistroDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _usuarios.ExisteEmailAsync(email))
                return Conflict(new ProblemDetails { Status = 409, Title = "Conflicto", Detail = "El email ya está registrado" });

            var rol = await _usuarios.GetRolPorNombreAsync(Roles.Cliente);
            if (rol == null)
                return Problem("El rol Cliente no está configurado.", statusCode: 500);

            //Encriptamos el password a utilizar
            string pswHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Email = email,
                PasswordHash = pswHash,
                Telefono = dto.Telefono,
                RolId = rol.Id
            };
            await _usuarios.AddAsync(usuario);
            return CreatedAtAction(nameof(GetMe), new { version = "1.0" }, new { mensaje = "Usuario creado con éxito!" });
        }

        /// <summary>Devuelve los datos del usuario autenticado.</summary>
        // GET: api/v1/User/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<object>> GetMe()
        {
            var id = int.Parse(User.FindFirstValue("uid")!);
            var usuario = await _usuarios.GetByIdAsync(id);
            if (usuario == null)
                return NotFound();

            return new { usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Email, Rol = usuario.Rol?.Nombre };
        }

        /// <summary>Inicia sesión y devuelve un JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _usuarios.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());
            if (user == null || user.Estado != "Activo")
                return Unauthorized(new { mensaje = "Credenciales incorrectas" });

            //verficiamos si la contraseña es válida
            bool pwdValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!pwdValid)
                return Unauthorized(new { mensaje = "Credenciales incorrectas" });

            var token = GenerarToken(user);
            return Ok(new { token });

        }
        private string GenerarToken(Usuario user)
        {
            // incorporamos los datos reales del usuario en los claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("uid", user.Id.ToString()),
                new Claim("nombre", $"{user.Nombre} {user.Apellido}"),
                new Claim(ClaimTypes.Role, user.Rol!.Nombre)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = creds
            };
            var tokenHadler = new JwtSecurityTokenHandler();
            var token = tokenHadler.CreateToken(tokenDescription);
            return tokenHadler.WriteToken(token);
        }
    }
}
