using Newsletter.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Newsletter.Data
{
    public class NewsletterDbContext : IdentityDbContext<User>
    {
        public NewsletterDbContext(DbContextOptions<NewsletterDbContext> options) : base(options) { }
        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<GameLibrary> GameLibraries { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<GamePlatform> GamePlatforms { get; set; }
        public DbSet<GameGenre> GameGenre { get; set; }
        public virtual DbSet<GameInEpic> GamesInEpic { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<GameLibrary>()
                .HasKey(gl => new { gl.LibraryId, gl.GameId });

            modelBuilder.Entity<GameLibrary>()
                .HasOne(gl => gl.Library)
                .WithMany(l => l.GameLibraries)
                .HasForeignKey(gl => gl.LibraryId);
            modelBuilder.Entity<GameLibrary>()
                .HasOne(gl => gl.Game)
                .WithMany(g => g.GameLibraries)
                .HasForeignKey(gl => gl.GameId);


            modelBuilder.Entity<GamePlatform>()
                .HasKey(gp => new { gp.GameId, gp.PlatformId });
            modelBuilder.Entity<GamePlatform>()
                .HasOne(gp => gp.Game)
                .WithMany(g => g.GamePlatforms)
                .HasForeignKey(gp => gp.GameId);
            modelBuilder.Entity<GamePlatform>()
                .HasOne(gp => gp.Platform)
                .WithMany(p => p.GamePlatforms)
                .HasForeignKey(gp => gp.PlatformId);

            modelBuilder.Entity<GameGenre>()
                .HasKey(gg => new { gg.GameId, gg.GenreId });
            modelBuilder.Entity<GameGenre>()
                .HasOne(gg => gg.Game)
                .WithMany(g => g.GameGenre)
                .HasForeignKey(gg => gg.GameId);
            modelBuilder.Entity<GameGenre>()
                .HasOne (gg => gg.Genre)
                .WithMany(g => g.GameGenre)
                .HasForeignKey (gg => gg.GenreId);

            modelBuilder.Entity<GameCategory>()
                .HasKey(gc => new { gc.GameId, gc.CategoryId });

            modelBuilder.Entity<GameInEpic>(entity =>
            {                
                entity.ToTable("GamesInEpic");               
                entity.HasKey(e => new { e.GameId, e.EpicStoreId });               
                entity.Property(e => e.GameId).HasColumnName("GameId");
                entity.Property(e => e.EpicStoreId).HasColumnName("EpicStoreId");               
                entity.HasOne(d => d.Game)
                      .WithOne(p => p.EpicData) 
                      .HasForeignKey<GameInEpic>(d => d.GameId) 
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}