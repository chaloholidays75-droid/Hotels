using Microsoft.EntityFrameworkCore;
using HotelAPI.Models; 

namespace HotelAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // A table for tickets

        public DbSet<HotelInfo> HotelInfos { get; set; }

        public DbSet<HotelStaff> HotelStaffs { get; set; }
       
      
    }
}
