using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using WEB.Models.Entities;

namespace WEB.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("ServiceManager", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<ServiceDescription> ServiceDescriptions { get; set; }
        public DbSet<ClientDetail> ClientDetails { get; set; }
        public DbSet<ReceiveHistoryItem> ReceiveHistoryItems { get; set; }
        public DbSet<TransmitHistoryItem> TransmitHistoryItems { get; set; }
        public DbSet<ServiceDetail> ServiceDetails { get; set; }
        public DbSet<TundukOrganization> TundukOrganizations { get; set; }
        public DbSet<Subsystem> Subsystems { get; set; }
        public DbSet<ServiceCode> ServiceCodes { get; set; }
    }
}