using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using HotelAPI.Models;
using System.Security.Claims;

namespace HotelAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly int? _currentUserId;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;

            // ✅ Extract current user ID from HttpContext
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId))
                    _currentUserId = parsedId;
            }
        }
                public async Task<string> GenerateBookingReferenceAsync(string bookingType, CancellationToken cancellationToken = default)
        {
            // Defensive: ensure valid one-letter prefix
            bookingType = string.IsNullOrWhiteSpace(bookingType) ? "H" : bookingType.Trim().Substring(0, 1).ToUpper();

            var lastRef = await Bookings
                .Where(b => b.BookingType == bookingType)
                .OrderByDescending(b => b.Id)
                .Select(b => b.BookingReference)
                .FirstOrDefaultAsync(cancellationToken);

            int nextRefNumber = 10000;

            if (!string.IsNullOrWhiteSpace(lastRef) && lastRef.Contains("-"))
            {
                var numPart = lastRef.Split('-').Last();
                if (int.TryParse(numPart, out int parsed))
                    nextRefNumber = parsed;
            }

            nextRefNumber++;
            return $"{bookingType}-{nextRefNumber:D5}";
        }
        // ======= DbSets =======
        public DbSet<HotelInfo> HotelInfo { get; set; } = null!;
        public DbSet<HotelStaff> HotelStaff { get; set; } = null!;
        public DbSet<BookingRoom> BookingRooms { get; set; } = null!;
        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierCategory> SupplierCategories { get; set; } = null!;
        public DbSet<SupplierSubCategory> SupplierSubCategories { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Agency> Agencies { get; set; } = null!;
        public DbSet<AgencyStaff> AgencyStaff { get; set; } = null!;
        public DbSet<Commercial> Commercials { get; set; } = null!;
        public DbSet<RememberToken> RememberTokens { get; set; } = null!;
        public DbSet<RecentActivity> RecentActivities { get; set; } = null!;

        // ======= SaveChanges =======
        public override int SaveChanges()
        {
            ApplyAuditInformation();
            LogRecentActivities();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            LogRecentActivities();

            // ✅ Tell PostgreSQL which user is currently active
            var conn = (NpgsqlConnection)Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            var currentUserId = _currentUserId ?? 0;
            using (var cmd = new NpgsqlCommand("SET LOCAL app.current_user_id = @userId;", conn))
            {
                cmd.Parameters.AddWithValue("@userId", currentUserId);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        // ======= Audit Fields =======
        private void ApplyAuditInformation()
        {
            int? userId = _currentUserId;
            string? userName = null;

            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var nameClaim = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                userName = nameClaim;
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

        // ======= Recent Activity Logger =======
        private void LogRecentActivities()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var userIdStr = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "System";
            int.TryParse(userIdStr, out int userId);

            // ✅ Skip internal/system tables
            var skipList = new[] { nameof(RecentActivity), nameof(RefreshToken), nameof(RememberToken) };

            var entries = ChangeTracker.Entries()
                .Where(e =>
                    (e.State == EntityState.Added ||
                     e.State == EntityState.Modified ||
                     e.State == EntityState.Deleted)
                    && !skipList.Contains(e.Entity.GetType().Name))
                .ToList();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                var action = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                int recordId = 0;
                var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
                if (idProp?.CurrentValue != null)
                    int.TryParse(idProp.CurrentValue.ToString(), out recordId);

                string changedData = null;
                if (entry.State == EntityState.Modified)
                {
                    var modifiedProps = entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p => $"{p.Metadata.Name}: '{p.OriginalValue}' → '{p.CurrentValue}'");
                    changedData = string.Join("; ", modifiedProps);
                }

                var description = $"{action} operation on {entityName} (ID {recordId}) by {userName}.";
                if (!string.IsNullOrEmpty(changedData))
                    description += $" Changes: {changedData}";

                var activity = new RecentActivity
                {
                    UserName = userName,
                    ActionType = action,
                    TableName = entityName,
                    RecordId = recordId,
                    Description = description,
                    ChangedData = changedData,
                    Timestamp = DateTime.UtcNow
                };

                RecentActivities.Add(activity);
            }
        }

        // ======= Model Configuration =======
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // (keep all your existing relationships/configs here exactly as-is)

            modelBuilder.Entity<AgencyStaff>()
                .HasOne(a => a.Agency)
                .WithMany(b => b.Staff)
                .HasForeignKey(a => a.AgencyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelStaff>()
                .HasOne(s => s.HotelInfo)
                .WithMany(h => h.HotelStaff)
                .HasForeignKey(s => s.HotelInfoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(b => b.Id);

                entity.HasOne(b => b.Agency)
                    .WithMany()
                    .HasForeignKey(b => b.AgencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Supplier)
                    .WithMany()
                    .HasForeignKey(b => b.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Hotel)
                    .WithMany()
                    .HasForeignKey(b => b.HotelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
