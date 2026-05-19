using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelsAPI.Data;

namespace HotelsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GuestsController : ControllerBase
    {
        private readonly HotelsDbContext _db;
        public GuestsController(HotelsDbContext db) => _db = db;

        private short CurrentGuestId() =>
            short.Parse(User.FindFirstValue("guestId")!);

        // GET api/guests/me — профиль текущего гостя
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var id    = CurrentGuestId();
            var guest = await _db.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            return Ok(new
            {
                guest.GuestId,
                guest.FirstName,
                guest.LastName,
                guest.Idnp,
                guest.Email,
                guest.Phone,
                guest.Username
            });
        }

        // PUT api/guests/me — обновить профиль
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] GuestUpdateDto dto)
        {
            var id    = CurrentGuestId();
            var guest = await _db.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            guest.FirstName = dto.FirstName;
            guest.LastName  = dto.LastName;
            guest.Email     = dto.Email;
            guest.Phone     = dto.Phone;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Профиль обновлён" });
        }

        // GET api/guests — все гости (для WPF-части / admin)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Guests
                .Select(g => new
                {
                    g.GuestId,
                    g.FirstName,
                    g.LastName,
                    g.Email,
                    g.Phone,
                    g.Username
                })
                .ToListAsync();
            return Ok(list);
        }

        // DELETE api/guests/5 — удалить гостя со всеми бронями
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(short id)
        {
            await _db.Database.ExecuteSqlRawAsync("EXEC sp_DeleteGuest @p0", id);
            return Ok(new { message = "Гость удалён" });
        }
    }

    public record GuestUpdateDto(string FirstName, string LastName, string Email, string Phone);
}
