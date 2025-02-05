using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserMessageEntity> UserMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserMessageEntity>(entity =>
            {
                entity.HasKey(e => e.Id); // Установка первичного ключа
                entity.Property(e => e.Text).HasMaxLength(4096); // Ограничение на длину текста
                entity.Property(e => e.SendDate).IsRequired(); // Обязательное поле
            });
        }
    }
}
