using System.ComponentModel.DataAnnotations;
namespace PersonelIzinSistemi.ViewModels;
public class GirisViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Parola { get; set; } = "";
    public bool BeniHatirla { get; set; }
}
