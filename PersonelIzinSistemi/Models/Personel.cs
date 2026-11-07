namespace PersonelIzinSistemi.Models
{
    public class Personel
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public string Soyad { get; set; } = "";
        public string Departman { get; set; } = "";
        public decimal Maas { get; set; }
    }
}