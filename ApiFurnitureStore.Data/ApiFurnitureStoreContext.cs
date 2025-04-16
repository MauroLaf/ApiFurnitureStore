using Microsoft.EntityFrameworkCore;
using ApiFurnitureStore.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ApiFurnitureStore.Shared.DTOs;

namespace ApiFurnitureStore.Data
{
    public class ApiFurnitureStoreContext : IdentityDbContext //antes era dbcontext hasta que agregamos paquete identity
    {
        //constructor dbcontext
        public ApiFurnitureStoreContext(DbContextOptions options) : base (options) { }
        
        //clases de entity "dbset" (representacion de tablas)
        //NO OLVIDAR AGREGAR referencia a shared
        public DbSet<Client> Clients {  get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        //uso un metodo heredado de dbcontext para usar sqlite
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite();
        }
        //para avisar que agregare una nueva tabla con relaciones y especificar relaciones como las fk
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //especifico que creare una tabla que tendra una pk(compuesta)
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<OrderDetail>()
                        .HasKey(od => new {od.OrderId, od.ProductId});
        }

    }
}
