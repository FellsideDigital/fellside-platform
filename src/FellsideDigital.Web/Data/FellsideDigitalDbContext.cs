using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Data
{
    public class FellsideDigitalDbContext(DbContextOptions<FellsideDigitalDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        DbSet<ApplicationUser> Customers { get; set; }
        public DbSet<ClientInvitation> ClientInvitations => Set<ClientInvitation>();
        public DbSet<ClientProject> ClientProjects => Set<ClientProject>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<ProjectStatusUpdate> ProjectStatusUpdates => Set<ProjectStatusUpdate>();
        public DbSet<ContactEnquiry> ContactEnquiries => Set<ContactEnquiry>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ClientInvitation>(e =>
            {
                e.HasIndex(i => i.Token).IsUnique();

                e.HasOne(i => i.CreatedBy)
                    .WithMany()
                    .HasForeignKey(i => i.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(i => i.AcceptedUser)
                    .WithMany()
                    .HasForeignKey(i => i.AcceptedUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Invitation)
                .WithMany()
                .HasForeignKey(u => u.InvitationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ClientProject>(e =>
            {
                e.HasOne(p => p.Client)
                    .WithMany()
                    .HasForeignKey(p => p.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.CreatedByAdmin)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Invoice>(e =>
            {
                e.HasOne(i => i.Project)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(i => i.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(i => i.Amount)
                    .HasColumnType("decimal(18,2)");
            });

            builder.Entity<ProjectStatusUpdate>(e =>
            {
                e.HasOne(u => u.Project)
                    .WithMany(p => p.StatusUpdates)
                    .HasForeignKey(u => u.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(u => u.CreatedByAdmin)
                    .WithMany()
                    .HasForeignKey(u => u.CreatedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
