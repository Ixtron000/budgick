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

        public virtual DbSet<MessageEntity> Messages { get; set; }

        public virtual DbSet<OrderEntity> Orders { get; set; }

        public virtual DbSet<UserEntity> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<MessageEntity>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("messages")
                    .UseCollation("utf8mb4_general_ci");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Comand)
                    .HasMaxLength(255)
                    .HasColumnName("comand");
                entity.Property(e => e.Text)
                    .HasColumnType("text")
                    .HasColumnName("text");
                entity.Property(e => e.Type)
                    .HasMaxLength(255)
                    .HasColumnName("type");
            });

            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("orders")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => e.UserId, "user_id");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");
                entity.Property(e => e.Date)
                    .HasColumnType("datetime")
                    .HasColumnName("date");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.Orders)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("orders_ibfk_1");
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.ToTable("users")
                    .UseCollation("utf8mb4_general_ci");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Admin)
                    .HasDefaultValueSql("'0'")
                    .HasColumnName("admin");
                entity.Property(e => e.Balance)
                    .HasPrecision(10, 2)
                    .HasColumnName("balance");
                entity.Property(e => e.ChatId).HasColumnName("chat_id");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
