using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelsAPI.Data
{
    // ──────────────────────────────────────────────
    //  ENTITIES
    // ──────────────────────────────────────────────

    [Table("guest")]
    public class Guest
    {
        [Key]
        [Column("guest_id")]
        public short GuestId { get; set; }

        [Column("first_name")] public string FirstName { get; set; } = "";
        [Column("last_name")]  public string LastName  { get; set; } = "";
        [Column("IDNP")]       public string Idnp      { get; set; } = "";
        [Column("email")]      public string Email     { get; set; } = "";
        [Column("phone")]      public string Phone     { get; set; } = "";
        [Column("username")]   public string Username  { get; set; } = "";
        [Column("pwd_hash")]   public string PwdHash   { get; set; } = "";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    [Table("hotel")]
    public class Hotel
    {
        [Key]
        [Column("hotel_id")]
        public short HotelId { get; set; }

        [Column("nazvanie")]       public string  Name          { get; set; } = "";
        [Column("adres")]          public string  Address       { get; set; } = "";
        [Column("rating")]         public decimal Rating        { get; set; }
        [Column("price_standard")] public decimal PriceStandard { get; set; }
        [Column("price_lux")]      public decimal PriceLux      { get; set; }
        [Column("price_econom")]   public decimal PriceEconom   { get; set; }
        [Column("price_family")]   public decimal PriceFamily   { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    [Table("booking")]
    public class Booking
    {
        [Key]
        [Column("booking_id")]
        public short BookingId { get; set; }

        [Column("guest_id")]    public short   GuestId     { get; set; }
        [Column("hotel_id")]    public short   HotelId     { get; set; }
        [Column("adults")]      public int     Adults      { get; set; }
        [Column("children")]    public int     Children    { get; set; }
        [Column("room_type")]   public string  RoomType    { get; set; } = "";
        [Column("check_in")]    public DateOnly CheckIn    { get; set; }
        [Column("check_out")]   public DateOnly CheckOut   { get; set; }
        [Column("booking_date")]public DateOnly BookingDate{ get; set; }
        [Column("summa")]       public decimal Summa       { get; set; }

        [ForeignKey(nameof(GuestId))] public Guest? Guest { get; set; }
        [ForeignKey(nameof(HotelId))] public Hotel? Hotel { get; set; }
    }

    // ──────────────────────────────────────────────
    //  DB CONTEXT
    // ──────────────────────────────────────────────

    public class HotelsDbContext : DbContext
    {
        public HotelsDbContext(DbContextOptions<HotelsDbContext> options) : base(options) { }

        public DbSet<Guest>   Guests   { get; set; }
        public DbSet<Hotel>   Hotels   { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Guest>().ToTable("guest");
            modelBuilder.Entity<Hotel>().ToTable("hotel");
            modelBuilder.Entity<Booking>().ToTable("booking");
        }
    }
}
