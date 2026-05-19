using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelsAPI.Data;

namespace HotelsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]                         // все методы требуют JWT
    public class BookingsController : ControllerBase
    {
        private readonly HotelsDbContext _db;
        public BookingsController(HotelsDbContext db) => _db = db;

        // ── Вспомогательный метод: ID гостя из токена ────────────────
        private short CurrentGuestId() =>
            short.Parse(User.FindFirstValue("guestId")!);

        // GET api/bookings — брони текущего гостя
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var guestId = CurrentGuestId();

            var list = await _db.Bookings
                .Where(b => b.GuestId == guestId)
                .Include(b => b.Hotel)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new
                {
                    b.BookingId,
                    b.HotelId,
                    HotelName = b.Hotel!.Name,
                    b.Adults,
                    b.Children,
                    b.RoomType,
                    CheckIn   = b.CheckIn.ToString("yyyy-MM-dd"),
                    CheckOut  = b.CheckOut.ToString("yyyy-MM-dd"),
                    BookingDate = b.BookingDate.ToString("yyyy-MM-dd"),
                    b.Summa
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET api/bookings/all — все брони (для WPF-приложения / admin)
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new
                {
                    b.BookingId,
                    b.GuestId,
                    GuestName = b.Guest!.FirstName + " " + b.Guest.LastName,
                    b.HotelId,
                    HotelName = b.Hotel!.Name,
                    b.Adults,
                    b.Children,
                    b.RoomType,
                    CheckIn   = b.CheckIn.ToString("yyyy-MM-dd"),
                    CheckOut  = b.CheckOut.ToString("yyyy-MM-dd"),
                    BookingDate = b.BookingDate.ToString("yyyy-MM-dd"),
                    b.Summa
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET api/bookings/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(short id)
        {
            var b = await _db.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (b == null) return NotFound();

            // Гость может видеть только свои брони
            if (b.GuestId != CurrentGuestId())
                return Forbid();

            return Ok(new
            {
                b.BookingId, b.GuestId, b.HotelId,
                HotelName = b.Hotel!.Name,
                b.Adults, b.Children, b.RoomType,
                CheckIn  = b.CheckIn.ToString("yyyy-MM-dd"),
                CheckOut = b.CheckOut.ToString("yyyy-MM-dd"),
                BookingDate = b.BookingDate.ToString("yyyy-MM-dd"),
                b.Summa
            });
        }

        // POST api/bookings — создать бронь
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingDto dto)
        {
            var guestId = CurrentGuestId();

            var hotel = await _db.Hotels.FindAsync(dto.HotelId);
            if (hotel == null) return BadRequest(new { message = "Отель не найден" });

            // Проверка пересечения дат
            var checkIn  = DateOnly.Parse(dto.CheckIn);
            var checkOut = DateOnly.Parse(dto.CheckOut);

            var conflict = await _db.Bookings.AnyAsync(b =>
                b.HotelId  == dto.HotelId &&
                b.RoomType == dto.RoomType &&
                b.CheckIn  < checkOut &&
                b.CheckOut > checkIn);

            if (conflict)
                return Conflict(new { message = "Выбранные даты уже заняты" });

            // Рассчитываем сумму
            int nights = checkOut.DayNumber - checkIn.DayNumber;
            decimal pricePerNight = dto.RoomType switch
            {
                "Стандарт"  => hotel.PriceStandard,
                "Люкс"      => hotel.PriceLux,
                "Эконом"    => hotel.PriceEconom,
                "Семейный"  => hotel.PriceFamily,
                _           => hotel.PriceStandard
            };

            var booking = new Booking
            {
                GuestId     = guestId,
                HotelId     = dto.HotelId,
                Adults      = dto.Adults,
                Children    = dto.Children,
                RoomType    = dto.RoomType,
                CheckIn     = checkIn,
                CheckOut    = checkOut,
                BookingDate = DateOnly.FromDateTime(DateTime.Today),
                Summa       = pricePerNight * nights
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            return Ok(new { booking.BookingId, booking.Summa });
        }

        // PUT api/bookings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(short id, [FromBody] BookingUpdateDto dto)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.GuestId != CurrentGuestId()) return Forbid();

            var hotel = await _db.Hotels.FindAsync(booking.HotelId);
            var checkIn  = DateOnly.Parse(dto.CheckIn);
            var checkOut = DateOnly.Parse(dto.CheckOut);

            var conflict = await _db.Bookings.AnyAsync(b =>
                b.BookingId != id &&
                b.HotelId   == booking.HotelId &&
                b.RoomType  == dto.RoomType &&
                b.CheckIn   < checkOut &&
                b.CheckOut  > checkIn);

            if (conflict) return Conflict(new { message = "Выбранные даты уже заняты" });

            int nights = checkOut.DayNumber - checkIn.DayNumber;
            decimal pricePerNight = dto.RoomType switch
            {
                "Стандарт" => hotel!.PriceStandard,
                "Люкс"     => hotel!.PriceLux,
                "Эконом"   => hotel!.PriceEconom,
                "Семейный" => hotel!.PriceFamily,
                _          => hotel!.PriceStandard
            };

            booking.Adults   = dto.Adults;
            booking.Children = dto.Children;
            booking.RoomType = dto.RoomType;
            booking.CheckIn  = checkIn;
            booking.CheckOut = checkOut;
            booking.Summa    = pricePerNight * nights;

            await _db.SaveChangesAsync();
            return Ok(new { booking.BookingId, booking.Summa });
        }

        // DELETE api/bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(short id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.GuestId != CurrentGuestId()) return Forbid();

            await _db.Database.ExecuteSqlRawAsync("EXEC sp_DeleteBooking @p0", id);
            return Ok(new { message = "Бронь удалена" });
        }
    }

    public record BookingDto(
        short HotelId, int Adults, int Children,
        string RoomType, string CheckIn, string CheckOut);

    public record BookingUpdateDto(
        int Adults, int Children,
        string RoomType, string CheckIn, string CheckOut);
}
