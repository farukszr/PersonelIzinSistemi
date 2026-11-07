using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Models;
namespace PersonelIzinSistemi.Data;
public class AppDbContext : IdentityDbContext<UygulamaKullanicisi>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Personel> Personeller => Set<Personel>();
    public DbSet<IzinTalebi> IzinTalepleri => Set<IzinTalebi>();
    public DbSet<IzinIslemGecmisi> IzinIslemGecmisleri => Set<IzinIslemGecmisi>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<UygulamaKullanicisi>()
            .HasOne(x => x.Personel).WithMany().HasForeignKey(x => x.PersonelId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<UygulamaKullanicisi>().HasIndex(x => x.PersonelId).IsUnique();
    }
}
