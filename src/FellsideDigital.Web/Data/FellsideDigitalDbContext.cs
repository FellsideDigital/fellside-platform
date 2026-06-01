using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FellsideDigital.Web.Data
{
    public class FellsideDigitalDbContext(DbContextOptions<FellsideDigitalDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        DbSet<ApplicationUser> Customers { get; set; }
        public DbSet<ClientInvitation> ClientInvitations => Set<ClientInvitation>();
        public DbSet<ClientProject> ClientProjects => Set<ClientProject>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<ProjectStatusUpdate> ProjectStatusUpdates => Set<ProjectStatusUpdate>();
        public DbSet<ProjectPlanPhase> ProjectPlanPhases => Set<ProjectPlanPhase>();
        public DbSet<ContactEnquiry> ContactEnquiries => Set<ContactEnquiry>();
        public DbSet<QrScan> QrScans => Set<QrScan>();
        public DbSet<QrLead> QrLeads => Set<QrLead>();
        public DbSet<ProjectMetric> ProjectMetrics => Set<ProjectMetric>();
        public DbSet<ProjectPipelineStep> ProjectPipelineSteps => Set<ProjectPipelineStep>();
        public DbSet<ProjectIntegration> ProjectIntegrations => Set<ProjectIntegration>();

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeDateTimesToUtc()
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dateTime)
                    {
                        property.CurrentValue = NormalizeToUtc(dateTime);
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDateTime)
                    {
                        property.CurrentValue = NormalizeToUtc(nullableDateTime);
                    }
                }
            }
        }

        private static DateTime NormalizeToUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => NormalizeToUtc(v),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var utcNullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? NormalizeToUtc(v.Value) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcDateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(utcNullableDateTimeConverter);
                    }
                }
            }

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

            builder.Entity<ProjectPlanPhase>(e =>
            {
                e.HasOne(ph => ph.Project)
                    .WithMany(p => p.PlanPhases)
                    .HasForeignKey(ph => ph.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<QrLead>(e =>
            {
                e.HasOne(l => l.QrScan)
                    .WithMany(s => s.Leads)
                    .HasForeignKey(l => l.QrScanId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ProjectMetric>(e =>
            {
                e.HasOne(m => m.Project)
                    .WithMany(p => p.Metrics)
                    .HasForeignKey(m => m.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProjectPipelineStep>(e =>
            {
                e.HasOne(s => s.Project)
                    .WithMany(p => p.PipelineSteps)
                    .HasForeignKey(s => s.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProjectIntegration>(e =>
            {
                e.HasOne(i => i.Project)
                    .WithMany(p => p.Integrations)
                    .HasForeignKey(i => i.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
