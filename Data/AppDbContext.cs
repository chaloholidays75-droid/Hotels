using Microsoft.EntityFrameworkCore;
using HotelAPI.Models; 

namespace HotelAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // A table for tickets

        public DbSet<HotelInfo> HotelInfo { get; set; }

        public DbSet<HotelStaff> HotelStaff { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelStaff>()
                .HasOne(s => s.HotelInfo)
                .WithMany(h => h.HotelStaff)
                .HasForeignKey(s => s.HotelSaleId)
                .OnDelete(DeleteBehavior.Cascade); // optional: delete staff when hotel is deleted
        }

      
    }
}
