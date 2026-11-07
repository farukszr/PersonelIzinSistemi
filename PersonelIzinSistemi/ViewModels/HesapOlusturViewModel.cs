using System.ComponentModel.DataAnnotations;
namespace PersonelIzinSistemi.ViewModels;
public class HesapOlusturViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(10), DataType(DataType.Password)] public string Parola { get; set; } = "";
    [Required] public int PersonelId { get; set; }
}
