using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelsAPI.Data;

namespace HotelsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly HotelsDbContext _db;
        public HotelsController(HotelsDbContext db) => _db = db;

        // GET api/hotels — список всех отелей
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var hotels = await _db.Hotels
                .OrderByDescending(h => h.Rating)
                .Select(h => new
                {
                    h.HotelId,
                    h.Name,
                    h.Address,
                    h.Rating,
                    h.PriceStandard,
                    h.PriceLux,
                    h.PriceEconom,
                    h.PriceFamily
                })
                .ToListAsync();

            return Ok(hotels);
        }

        // GET api/hotels/5 — один отель
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(short id)
        {
            var h = await _db.Hotels.FindAsync(id);
            if (h == null) return NotFound();

            return Ok(new
            {
                h.HotelId, h.Name, h.Address, h.Rating,
                h.PriceStandard, h.PriceLux, h.PriceEconom, h.PriceFamily
            });
        }

        // POST api/hotels — добавить отель (только авторизованным / администратору)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HotelDto dto)
        {
            var hotel = new Hotel
            {
                Name          = dto.Name,
                Address       = dto.Address,
                Rating        = dto.Rating,
                PriceStandard = dto.PriceStandard,
                PriceLux      = dto.PriceLux,
                PriceEconom   = dto.PriceEconom,
                PriceFamily   = dto.PriceFamily
            };
            _db.Hotels.Add(hotel);
            await _db.SaveChangesAsync();
            return Ok(hotel);
        }

        // PUT api/hotels/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(short id, [FromBody] HotelDto dto)
        {
            var hotel = await _db.Hotels.FindAsync(id);
            if (hotel == null) return NotFound();

            hotel.Name          = dto.Name;
            hotel.Address       = dto.Address;
            hotel.Rating        = dto.Rating;
            hotel.PriceStandard = dto.PriceStandard;
            hotel.PriceLux      = dto.PriceLux;
            hotel.PriceEconom   = dto.PriceEconom;
            hotel.PriceFamily   = dto.PriceFamily;

            await _db.SaveChangesAsync();
            return Ok(hotel);
        }

        // DELETE api/hotels/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(short id)
        {
            // Используем хранимую процедуру из БД
            try
            {
                await _db.Database.ExecuteSqlRawAsync("EXEC sp_DeleteHotel @p0", id);
                return Ok(new { message = "Отель удалён" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/hotels/5/booked-dates?roomType=Люкс
        [HttpGet("{id}/booked-dates")]
        public async Task<IActionResult> GetBookedDates(short id, [FromQuery] string roomType, [FromQuery] short? excludeBookingId = null)
        {
            var results = await _db.Bookings
                .Where(b => b.HotelId == id
                         && b.RoomType == roomType
                         && (excludeBookingId == null || b.BookingId != excludeBookingId))
                .Select(b => new { from = b.CheckIn, to = b.CheckOut })
                .ToListAsync();

            return Ok(results);
        }
    }

    public record HotelDto(
        string Name, string Address, decimal Rating,
        decimal PriceStandard, decimal PriceLux,
        decimal PriceEconom, decimal PriceFamily);
}
