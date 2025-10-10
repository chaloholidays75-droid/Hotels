using Microsoft.EntityFrameworkCore;
using HotelAPI.Models;
using HotelAPI.Services;

namespace HotelAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<HotelInfo> HotelInfo { get; set; } = null!;
        public DbSet<HotelStaff> HotelStaff { get; set; } = null!;
        public DbSet<BookingRoom> BookingRooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierCategory> SupplierCategories { get; set; } = null!;
        public DbSet<SupplierSubCategory> SupplierSubCategories { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Agency> Agencies { get; set; }
        public DbSet<AgencyStaff> AgencyStaff { get; set; }

        public DbSet<RecentActivity> RecentActivities { get; set; }

    public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInformation()
        {
            int? userId = null;

            // Get current user ID from HTTP context
            var httpContext = _httpContextAccessor?.HttpContext;
            var userIdStr = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                userId = int.Parse(userIdStr);
            }

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedById = userId;
                    entry.Entity.UpdatedById = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedById = userId;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelStaff>()
                .HasOne(s => s.HotelInfo)
                .WithMany(h => h.HotelStaff)
                .HasForeignKey(s => s.HotelInfoId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Country>(entity =>
            {
                entity.ToTable("Countries");
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.Code).HasColumnName("Code");
                entity.Property(e => e.Flag).HasColumnName("Flag");
                entity.Property(e => e.PhoneCode).HasColumnName("PhoneCode");
                entity.Property(e => e.PhoneNumberDigits).HasColumnName("PhoneNumberDigits");
            });
            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("Cities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.CountryId).HasColumnName("CountryId");
            });

            // Map table name
            modelBuilder.Entity<City>().ToTable("Cities");

            // Configure primary key
            modelBuilder.Entity<City>().HasKey(c => c.Id);

            // Configure relationship: City -> Country
            modelBuilder.Entity<City>()
                .HasOne(c => c.Country)
                .WithMany(country => country.Cities)
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Configure Hotels navigation
            modelBuilder.Entity<City>()
                .HasMany(c => c.Hotels)
                .WithOne(h => h.City)
                .HasForeignKey(h => h.CityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecentActivity>(entity =>
            {
                entity.ToTable("RecentActivities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Entity).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(255);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
            });
            modelBuilder.Entity<Booking>(entity =>
                {
                    entity.ToTable("Bookings");
                    entity.HasKey(b => b.Id);

                    // Foreign key -> Agency
                    entity.HasOne(b => b.Agency)
                        .WithMany() // You can optionally add a ICollection<Booking> in Agency
                        .HasForeignKey(b => b.AgencyId)
                        .OnDelete(DeleteBehavior.Restrict);

                    // Foreign key -> Supplier
                    entity.HasOne(b => b.Supplier)
                        .WithMany()
                        .HasForeignKey(b => b.SupplierId)
                        .OnDelete(DeleteBehavior.Restrict);

                    // Foreign key -> HotelInfo
                    entity.HasOne(b => b.Hotel)
                        .WithMany()
                        .HasForeignKey(b => b.HotelId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

        }

    }
}
