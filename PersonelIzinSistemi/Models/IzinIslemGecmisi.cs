
using System;

namespace PersonelIzinSistemi.Models
{
    public class IzinIslemGecmisi
    {
        public int Id { get; set; }

        public int IzinTalebiId { get; set; }

        public string IslemTuru { get; set; } = "";

        public string OncekiDurum { get; set; } = "";

        public string YeniDurum { get; set; } = "";

        public string IslemiYapanKullaniciId { get; set; } = "";

        public DateTime IslemTarihi { get; set; } = DateTime.UtcNow;
    }
}
