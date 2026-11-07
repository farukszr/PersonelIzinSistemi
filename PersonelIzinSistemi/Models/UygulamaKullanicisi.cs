using Microsoft.AspNetCore.Identity;
namespace PersonelIzinSistemi.Models;
public class UygulamaKullanicisi : IdentityUser
{
    public int? PersonelId { get; set; }
    public Personel? Personel { get; set; }
}
