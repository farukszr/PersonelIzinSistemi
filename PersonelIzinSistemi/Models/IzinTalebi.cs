
namespace PersonelIzinSistemi.Models
{
    public class IzinTalebi
    {
        public int Id { get; set; }

        public int PersonelId { get; set; }

        public DateTime BaslangicTarihi { get; set; }

        public DateTime BitisTarihi { get; set; }

        public string IzinNedeni { get; set; } = "";

        // Beklemede, Onaylandı, Reddedildi
        public string Durum { get; set; } = "Beklemede";

        // Arşivleme durumu
        // false: Aktif listede
        // true: Arşivde
        public bool ArsivlendiMi { get; set; } = false;

        public Personel Personel { get; set; } = null!;
    }
}
