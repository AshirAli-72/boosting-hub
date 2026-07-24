using BoostingHub.backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RoleHasPermission> RolesHasPermissions { get; set; }
    public DbSet<UserHasRole> UserHasRoles { get; set; }
    public DbSet<Orders> Orders { get; set; }
    public DbSet<TaskGenerate> TaskGenerates { get; set; }
    public DbSet<TaskComplete> TaskCompletes { get; set; }
    public DbSet<TaskProof> TaskProofs { get; set; }
    public DbSet<AcceptedTask> AcceptedTasks { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<SocialMediaAccount> SocialMediaAccounts { get; set; }
    public DbSet<Package> Packages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);



        // ── User ─────────────────────────────────────────────────────────────
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique().HasFilter("[email] IS NOT NULL");
            e.HasIndex(u => u.Phone).IsUnique().HasFilter("[phone] IS NOT NULL");
            e.Property(u => u.Email).HasMaxLength(255);
            e.Property(u => u.Phone).HasMaxLength(50);
            e.Property(u => u.Password).HasMaxLength(500);
            e.Property(u => u.RememberToken).HasMaxLength(500);
            e.Property(u => u.Name).HasMaxLength(200);
            e.Property(u => u.EmailChangeToken).HasMaxLength(500);
            e.HasOne(u => u.Wallet).WithOne(w => w.User).HasForeignKey<Wallet>(w => w.UserId);
        });

        // ── Role ─────────────────────────────────────────────────────────────
        builder.Entity<Role>(e =>
        {
            e.Property(r => r.RoleTitle).HasMaxLength(100).IsRequired();
            e.HasIndex(r => r.RoleTitle).IsUnique();
        });

        // ── Permission ───────────────────────────────────────────────────────
        builder.Entity<Permission>(e =>
        {
            e.Property(p => p.Names).HasMaxLength(255);
            e.Property(p => p.Slugs).HasMaxLength(255);
            e.HasIndex(p => p.Slugs).IsUnique().HasFilter("[slugs] IS NOT NULL");
        });

        // ── RoleHasPermission ─────────────────────────────────────────────────
        builder.Entity<RoleHasPermission>(e =>
        {
            e.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            e.HasOne(rp => rp.Role).WithMany(r => r.RoleHasPermissions).HasForeignKey(rp => rp.RoleId);
            e.HasOne(rp => rp.Permission).WithMany(p => p.RoleHasPermissions).HasForeignKey(rp => rp.PermissionId);
        });

        // ── UserHasRole ───────────────────────────────────────────────────────
        builder.Entity<UserHasRole>(e =>
        {
            e.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            e.HasOne(ur => ur.User).WithMany(u => u.UserHasRoles).HasForeignKey(ur => ur.UserId);
            e.HasOne(ur => ur.Role).WithMany(r => r.UserHasRoles).HasForeignKey(ur => ur.RoleId);
        });

        // ── Orders (Campaigns) ────────────────────────────────────────────────
        builder.Entity<Orders>(e =>
        {
            e.ToTable("orders");
            e.Property(c => c.Platform).HasMaxLength(500);
            e.Property(c => c.Service).HasMaxLength(500);
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.SocialMediaUrl).HasMaxLength(500);
            e.Property(c => c.Status).HasMaxLength(50);
            e.Property(c => c.CreatedAt).HasColumnType("datetime2");
            e.Property(c => c.PackageId).IsRequired(false);
            e.HasIndex(c => c.Status);
        });

        // ── TaskGenerate ──────────────────────────────────────────────────────
        builder.Entity<TaskGenerate>(e =>
        {
            e.ToTable("task_generate");
            e.Property(t => t.Platform).HasMaxLength(200);
            e.Property(t => t.Service).HasMaxLength(200);
            e.Property(t => t.Url).HasMaxLength(500);
            e.Property(t => t.CreatedAt).HasColumnType("datetime2");
            e.Property(t => t.Reward).HasColumnType("decimal(18,2)");
            e.Property(t => t.Status).HasMaxLength(50);
            e.HasOne(t => t.Order).WithMany(o => o.TaskGenerates).HasForeignKey(t => t.OrderId);
            e.HasIndex(t => t.OrderId);
            e.HasIndex(t => t.Status);
        });

        // ── TaskComplete ──────────────────────────────────────────────────────
        builder.Entity<TaskComplete>(e =>
        {
            e.ToTable("task_complete");
            e.Property(t => t.Date).HasColumnType("datetime2");
            e.Property(t => t.Status).HasMaxLength(50);
            e.HasIndex(t => t.UserId);
            e.HasIndex(t => t.TaskId);

            // proof_id is optional and points to task_proofs.id
            e.HasOne(t => t.Proof)
                .WithMany()
                .HasForeignKey(t => t.ProofId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(t => t.Task)
                .WithMany(tg => tg.TaskCompletes)
                .HasForeignKey(t => t.TaskId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── TaskProof ─────────────────────────────────────────────────────────
        builder.Entity<TaskProof>(e =>
        {
            e.ToTable("task_proofs");
            e.Property(p => p.Date).HasColumnType("datetime2");
            e.Property(p => p.ProofUrl).HasMaxLength(2048);
            e.Property(p => p.ProofType).HasMaxLength(50);
            e.Property(p => p.Status).HasMaxLength(50);
            e.Property(p => p.VerificationStatus).IsRequired().HasDefaultValue(4);
            e.Property(p => p.RejectReason).HasMaxLength(1000);

            e.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(p => p.Task)
                .WithMany()
                .HasForeignKey(p => p.TaskId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(p => p.VerificationStatus);
            e.HasIndex(p => new { p.UserId, p.TaskId });
        });


        // ── AcceptedTask ──────────────────────────────────────────────────────
        builder.Entity<AcceptedTask>(e =>
        {
            e.ToTable("accepted_tasks");
            e.Property(a => a.AcceptedAt).HasColumnType("datetime2");
            e.Property(a => a.Status).HasMaxLength(50);

            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(a => a.Task)
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.TaskId);
            e.HasIndex(a => new { a.UserId, a.TaskId }).IsUnique();
        });

        // ── Notification ──────────────────────────────────────────────────────
        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Type).HasMaxLength(100).IsRequired();
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.ReadAt).HasColumnType("datetime2");
            e.Property(n => n.CreatedAt).HasColumnType("datetime2");
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(n => n.UserId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // ── Wallet ────────────────────────────────────────────────────────────
        builder.Entity<Wallet>(e =>
        {
            e.Property(w => w.TotalBalance).HasColumnType("decimal(18,2)");
            e.Property(w => w.Withdrawn).HasColumnType("decimal(18,2)");
            e.Property(w => w.CreatedAt).HasColumnType("datetime2");
            e.Property(w => w.Currency).HasMaxLength(10);
            e.Property(w => w.Status).HasMaxLength(20);
            e.HasIndex(w => w.UserId).IsUnique();
        });

        // ── Transaction ───────────────────────────────────────────────────────
        builder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Type).HasMaxLength(50).IsRequired();
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.BalanceAfter).HasColumnType("decimal(18,2)");
            e.Property(t => t.Description).HasMaxLength(500);
            e.Property(t => t.ReferenceType).HasMaxLength(100);
            e.Property(t => t.CreatedAt).HasColumnType("datetime2");
            e.Property(t => t.Status).HasMaxLength(50);
            e.HasOne(t => t.Wallet).WithMany(w => w.Transactions).HasForeignKey(t => t.WalletId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(t => t.WalletId);
            e.HasIndex(t => t.CreatedAt);
        });

        // ── Account ───────────────────────────────────────────────────────────
        builder.Entity<Account>(e =>
        {
            e.Property(a => a.AccountTitle).HasMaxLength(200).IsRequired();
            e.Property(a => a.MobileNumber).HasMaxLength(50).IsRequired();
            e.Property(a => a.Cnic).HasMaxLength(50).IsRequired();
            e.Property(a => a.Status).HasMaxLength(20);
            e.Property(a => a.CreatedAt).HasColumnType("datetime2");
            e.Property(a => a.UpdatedAt).HasColumnType("datetime2");
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(a => a.UserId);
        });

        // ── ActivityLog ──────────────────────────────────────────────────────
        builder.Entity<ActivityLog>(e =>
        {
            e.Property(l => l.UserRole).HasMaxLength(20).IsRequired();
            e.Property(l => l.Event).HasMaxLength(50).IsRequired();
            e.Property(l => l.Description).HasMaxLength(500);
            e.Property(l => l.SubjectType).HasMaxLength(50).IsRequired();
            e.Property(l => l.SubjectName).HasMaxLength(200);
            e.Property(l => l.UserName).HasMaxLength(200);
            e.Property(l => l.UserEmail).HasMaxLength(255);
            e.Property(l => l.IpAddress).HasMaxLength(50);
            e.Property(l => l.CreatedAt).HasColumnType("datetime2");
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(l => l.UserId);
            e.HasIndex(l => new { l.SubjectType, l.SubjectId });
            e.HasIndex(l => l.CreatedAt);
            e.HasIndex(l => l.Event);
        });

        // ── SocialMediaAccount ─────────────────────────────────────────────
        builder.Entity<SocialMediaAccount>(e =>
        {
            e.Property(s => s.Platform).HasMaxLength(100).IsRequired();
            e.Property(s => s.Username).HasMaxLength(200).IsRequired();
            e.Property(s => s.ProfileUrl).HasMaxLength(500);
            e.Property(s => s.CreatedAt).HasColumnType("datetime2");
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => new { s.UserId, s.Platform }).IsUnique();
        });

        // ── Package ─────────────────────────────────────────────────────────
        builder.Entity<Package>(e =>
        {
            e.Property(p => p.Platform).HasMaxLength(200).IsRequired();
            e.Property(p => p.Service).HasMaxLength(200).IsRequired();
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.CreatedAt).HasColumnType("datetime2");
            e.Property(p => p.UpdatedAt).HasColumnType("datetime2");
            e.HasIndex(p => new { p.Platform, p.Service });
        });

    }
}
