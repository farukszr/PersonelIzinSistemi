
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonelIzinSistemi.Models;
using PersonelIzinSistemi.ViewModels;

namespace PersonelIzinSistemi.Controllers;

public class HesapController : Controller
{
    private readonly SignInManager<UygulamaKullanicisi> _signIn;
    private readonly UserManager<UygulamaKullanicisi> _userManager;

    public HesapController(
        SignInManager<UygulamaKullanicisi> signIn,
        UserManager<UygulamaKullanicisi> userManager)
    {
        _signIn = signIn;
        _userManager = userManager;
    }

    // Giriş sayfası
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Giris(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new GirisViewModel());
    }

    // Giriş işlemi
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(
        GirisViewModel model,
        string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(
            model.Email,
            model.Parola,
            model.BeniHatirla,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(
                model.Email);

            if (user == null)
            {
                await _signIn.SignOutAsync();

                ModelState.AddModelError(
                    "",
                    "Kullanıcı hesabı bulunamadı.");

                return View(model);
            }

            bool isAdmin = await _userManager.IsInRoleAsync(
                user,
                "Yonetici");

            bool isPersonel = await _userManager.IsInRoleAsync(
                user,
                "Personel");

            // Yönetici ana sayfaya yönlendirilir.
            if (isAdmin)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // Personel izin talepleri sayfasına yönlendirilir.
            if (isPersonel)
            {
                return RedirectToAction(
                    "Index",
                    "IzinTalepleri");
            }

            // Tanımlı rolü olmayan kullanıcı sisteme alınmaz.
            await _signIn.SignOutAsync();

            ModelState.AddModelError(
                "",
                "Hesabınıza geçerli bir rol atanmamış.");

            return View(model);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                "",
                "Hesabınız pasif durumda veya geçici olarak kilitlenmiş. Lütfen sistem yöneticisiyle iletişime geçin.");
        }

        else if (result.IsNotAllowed)
        {
            ModelState.AddModelError(
                "",
                "Bu hesabın giriş yapmasına izin verilmiyor.");
        }
        else
        {
            ModelState.AddModelError(
                "",
                "E-posta veya parola hatalı.");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(model);
    }

    // Çıkış işlemi
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cikis()
    {
        await _signIn.SignOutAsync();

        return RedirectToAction(nameof(Giris));
    }

    // Yetkisiz erişim sayfası
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Yetkisiz()
    {
        return View();
    }
}
