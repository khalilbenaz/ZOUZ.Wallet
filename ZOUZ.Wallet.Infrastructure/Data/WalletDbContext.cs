using ZOUZ.Wallet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders; 
using System;

namespace ZOUZ.Wallet.Infrastructure.Data;
public class WalletDbContext : DbContext
    {
        public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
        {
        }

        public DbSet<Core.Entities.Wallet> Wallets { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Bill> Bills { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurations des entités
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);

            // Configuration explicite de Wallet
            modelBuilder.Entity<Core.Entities.Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OwnerId).IsRequired();
                entity.Property(e => e.OwnerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.KycLevel).IsRequired();
                entity.Property(e => e.DailyLimit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MonthlyLimit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CurrentDailyUsage).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CurrentMonthlyUsage).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CinNumber).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired();
                
                // Relation avec Offer (optionnelle)
                entity.HasOne(e => e.Offer)
                      .WithMany(o => o.Wallets)
                      .HasForeignKey(e => e.OfferId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Relation avec User
                entity.HasOne<User>()
                      .WithMany(u => u.Wallets)
                      .HasForeignKey(e => e.OwnerId)
                      .HasPrincipalKey(u => u.Username) // Utilisez username comme clé étrangère
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration explicite de Offer
            modelBuilder.Entity<Offer>(entity =>
            {
                entity.ToTable("Offers");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.SpendingLimit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ValidFrom).IsRequired();
                entity.Property(e => e.ValidTo).IsRequired();
                entity.Property(e => e.CashbackPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.FeesDiscount).HasColumnType("decimal(5,2)");
                entity.Property(e => e.RechargeBonus).HasColumnType("decimal(5,2)");
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configuration explicite de Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.WalletId).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Fee).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Cashback).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
                entity.Property(e => e.IsSuccessful).IsRequired();
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                
                // Relation avec Wallet (source)
                entity.HasOne(e => e.Wallet)
                      .WithMany(w => w.Transactions)
                      .HasForeignKey(e => e.WalletId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Relation avec Wallet (destination, optionnelle)
                entity.HasOne(e => e.DestinationWallet)
                      .WithMany()
                      .HasForeignKey(e => e.DestinationWalletId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
                
                // Relation avec Bill (optionnelle)
                entity.HasOne(e => e.Bill)
                      .WithMany(b => b.Transactions)
                      .HasForeignKey(e => e.BillId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });

            // Configuration explicite de User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique(); // Username unique
                
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique(); // Email unique
                
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.CinNumber).HasMaxLength(20);
                entity.Property(e => e.DateOfBirth).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.KycLevel).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configuration explicite de Bill
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.ToTable("Bills");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.BillerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BillerReference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CustomerReference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.DueDate).IsRequired();
                entity.Property(e => e.IsPaid).IsRequired();
                entity.Property(e => e.BillType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
            });
        }

        // Override SaveChanges pour automatiquement remplir les timestamps
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                    
                    // Éviter de modifier CreatedAt
                    entry.Property("CreatedAt").IsModified = false;
                }
            }
        }
    }