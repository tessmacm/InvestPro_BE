using IMS.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<InvestorType> InvestorTypes => Set<InvestorType>();
    public DbSet<InvestmentInterest> InvestmentInterests => Set<InvestmentInterest>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<InvestorCommitment> InvestorCommitments => Set<InvestorCommitment>();
    public DbSet<InvestorDocument> InvestorDocuments => Set<InvestorDocument>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RoiContract> RoiContracts => Set<RoiContract>();
    public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Database.Migrate();
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    base.OnConfiguring(optionsBuilder);
    //    // Configure the database provider and connection string here if not using dependency injection
    //    optionsBuilder.UseSqlServer(@"Server=AKBER-PC\SQLEXPRESS;Database=IMSDb2;Trusted_Connection=True;TrustServerCertificate=True");
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. MUST call base.OnModelCreating first to initialize Identity table schemas
        base.OnModelCreating(modelBuilder);


        // 2. Configure 1:1 relationship between Login (ApplicationUser) and Financial Profile (InvestorProfile)
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.InvestorNav)
            .WithOne()
            .HasForeignKey<ApplicationUser>(u => u.InvestorId)
            .OnDelete(DeleteBehavior.SetNull); // If profile is gone, keep user login; or Restrict

        modelBuilder.Entity<ApplicationUser>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<ApplicationUser>("ApplicationUser");


        modelBuilder.Entity<Investor>(entity =>
        {
            entity.ToTable("Investors");

            entity.HasKey(i => i.InvestorId);

            entity.HasOne(i => i.InvestorTypeNav)
            .WithMany()
            .HasForeignKey(i => i.InvestorTypeId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of InvestorType if linked to Investors

            entity.HasOne(i => i.InvestmentInterestNav)
             .WithMany()
             .HasForeignKey(i => i.InvestmentInterestId)
             .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of InvestmentInterest if linked to Investors
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");

            entity.Property(p => p.TargetFunding)
            .HasColumnType("decimal(18,2)");

            entity.Property(p => p.FundedAmount)
            .HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<InvestorCommitment>(entity =>
        {
            entity.ToTable("InvestorCommitments");
            entity.HasOne(ic => ic.InvestorNav)
            .WithMany(ic => ic.Commitments)
            .HasForeignKey(ic => ic.InvestorId)
            .OnDelete(DeleteBehavior.Cascade); // If Investor is deleted, delete commitments

            entity.HasOne(ic => ic.ProjectNav)
            .WithMany(ic => ic.Commitments)
            .HasForeignKey(ic => ic.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // If Project is deleted, delete commitments
        });

        modelBuilder.Entity<InvestorDocument>(entity =>
        {
            entity.ToTable("InvestorDocuments");

            entity.HasOne(id => id.InvestorNav)
            .WithMany(i => i.Documents)
            .HasForeignKey(d => d.InvestorId)
            .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure precision/scale for monetary decimal properties to avoid silent truncation
        modelBuilder.Entity<InvestmentInterest>(entity =>
        {
            entity.Property(e => e.MinAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
        });

        SeedLookupData(modelBuilder);
    }

    private void SeedLookupData(ModelBuilder builder)
    {
        // Seed Investor Types
        builder.Entity<InvestorType>().HasData(
            new InvestorType { Id = 1, Name = "Individual" },
            new InvestorType { Id = 2, Name = "Business" }
        );
        // Seed Investment Interests
        builder.Entity<InvestmentInterest>().HasData(
            new InvestmentInterest { Id = 1, DisplayRange = "50,000 - 100,000", MinAmount = 50000, MaxAmount = 100000 },
            new InvestmentInterest { Id = 2, DisplayRange = "100,000 - 500,000", MinAmount = 100000, MaxAmount = 500000 },
            new InvestmentInterest { Id = 3, DisplayRange = "500,000 - 1,000,000", MinAmount = 500000, MaxAmount = 1000000 },
            new InvestmentInterest { Id = 4, DisplayRange = "1,000,000+", MinAmount = 1000000, MaxAmount = 999999999999.99m }
        );
    }

}
