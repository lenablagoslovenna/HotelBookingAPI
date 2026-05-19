using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelsAPI.Data;

namespace HotelsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly HotelsDbContext _db;
        private readonly IConfiguration  _cfg;

        public AuthController(HotelsDbContext db, IConfiguration cfg)
        {
            _db  = db;
            _cfg = cfg;
        }

        // ── POST api/auth/login ──────────────────────────────────────
        // Тело: { "username": "guest1", "password": "1234" }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var guest = await _db.Guests
                .FirstOrDefaultAsync(g => g.Username == req.Username && g.PwdHash == req.Password);

            if (guest == null)
                return Unauthorized(new { message = "Неверный логин или пароль" });

            var token = GenerateToken(guest);
            return Ok(new
            {
                token,
                guestId   = guest.GuestId,
                firstName = guest.FirstName,
                lastName  = guest.LastName,
                email     = guest.Email
            });
        }

        // ── POST api/auth/register ───────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (await _db.Guests.AnyAsync(g => g.Username == req.Username))
                return Conflict(new { message = "Такой логин уже занят" });

            if (await _db.Guests.AnyAsync(g => g.Email == req.Email))
                return Conflict(new { message = "Email уже зарегистрирован" });

            // Генерируем следующий ID (IDENTITY в SQL Server сделает это сам, но у нас SMALLINT IDENTITY)
            var guest = new Guest
            {
                FirstName = req.FirstName,
                LastName  = req.LastName,
                Idnp      = req.Idnp,
                Email     = req.Email,
                Phone     = req.Phone,
                Username  = req.Username,
                PwdHash   = req.Password   // В реальном проекте — хешировать!
            };

            _db.Guests.Add(guest);
            await _db.SaveChangesAsync();

            var token = GenerateToken(guest);
            return Ok(new { token, guestId = guest.GuestId });
        }

        // ── Генерация JWT ────────────────────────────────────────────
        private string GenerateToken(Guest guest)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("guestId",  guest.GuestId.ToString()),
                new Claim("username", guest.Username),
                new Claim(ClaimTypes.Name, guest.Username)
            };

            var token = new JwtSecurityToken(
                claims:   claims,
                expires:  DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ── DTO ──────────────────────────────────────────────────────────
    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(
        string FirstName, string LastName,
        string Idnp, string Email,
        string Phone, string Username, string Password);
}
