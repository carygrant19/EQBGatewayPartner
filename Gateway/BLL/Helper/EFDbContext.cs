using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Models = Gateway.Data.Models;

namespace Gateway.BLL.Helper
{
    public class EFDbContext : DbContext
    {
        public EFDbContext(DbContextOptions<EFDbContext> options) : base(options)
        {
            this.ChangeTracker.LazyLoadingEnabled = false;
        }
        //Logging
        public DbSet<Models.ActivityLog> ActivityLog { get; set; }
        public DbSet<Models.AuditLog> AuditLog { get; set; }
        public DbSet<Models.ExceptionLog> ExceptionLog { get; set; }
        public DbSet<Models.HttpLog> HttpLog { get; set; }
        public DbSet<Models.TransactionLog> TransactionLog { get; set; }


        public DbSet<Models.ActiveUser> ActiveUser { get; set; } 
        public DbSet<Models.Branch> Branch { get; set; }
        public DbSet<Models.Client> Client { get; set; }
        public DbSet<Models.Company> Company { get; set; }
        public DbSet<Models.Module> Module { get; set; }
        public DbSet<Models.ModulePermission> ModulePermission { get; set; }
        public DbSet<Models.PasswordHistory> PasswordHistory { get; set; }
        public DbSet<Models.Permission> Permission { get; set; }
        public DbSet<Models.Role> Role { get; set; }
        public DbSet<Models.RoleModulePermission> RoleModulePermission { get; set; }
        public DbSet<Models.User> User { get; set; }
        public DbSet<Models.UserRole> UserRole { get; set; }

        //Application
        public DbSet<Models.Route> Route { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Module>()
                .HasMany(m => m.Children)
                .WithOne(m => m.Parent)
                .HasForeignKey(m => m.ParentId)
                .HasPrincipalKey(m => m.Id);

            modelBuilder.Entity<ModulePermission>()
                .HasOne(mp => mp.Module)
                .WithMany(m => m.ModulePermission)
                .HasForeignKey(mp => mp.ModuleId)
                .HasPrincipalKey(m => m.Id);
             
            
            modelBuilder.Entity<RoleModulePermission>()
                .HasOne(m => m.Permissions)
                .WithMany(rm => rm.RoleModulePermissions)
                .HasForeignKey(rm => rm.PermissionId)
                .HasPrincipalKey(m => m.Id);
            modelBuilder.Entity<User>()
                .HasMany(m => m.UserRoles)
                .WithOne(rm => rm.User)
                .HasForeignKey(rm => rm.UserId)
                .HasPrincipalKey(m => m.Id);

            modelBuilder.Entity<UserRole>()
                .HasOne(r => r.Role)
                .WithMany()
                .HasForeignKey(rm => rm.RoleId)
                .HasPrincipalKey(m => m.Id);

            modelBuilder.Entity<User>()
                .HasOne(m => m.Branch)
                .WithMany()
                .HasForeignKey(rm => rm.BranchId)
                .HasPrincipalKey(m => m.Id);

            //application
            modelBuilder.Entity<Route>()
              .HasMany(r => r.Hosts)
              .WithOne(h => h.Route)
              .HasForeignKey(h => h.RouteId)
              .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Route>()
                .HasMany(r => r.IpRules)
                .WithOne(i => i.Route)
                .HasForeignKey(i => i.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Route>()
                .HasMany(r => r.Clients)
                .WithOne(c => c.Route)
                .HasForeignKey(c => c.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Route>()
                .HasOne(r => r.RouteCategory)
                .WithMany()
                .HasForeignKey(r => r.Category)
                .HasPrincipalKey(c => c.Code);

        }
    }
     
}
