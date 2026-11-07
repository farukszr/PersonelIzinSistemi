
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;
using PersonelIzinSistemi.ViewModels;

namespace PersonelIzinSistemi.Controllers;

[Authorize(Roles = "Yonetici")]
public class KullanicilarController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<UygulamaKullanicisi> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public KullanicilarController(
        AppDbContext db,
        UserManager<UygulamaKullanicisi> users,
        RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    // Kullanıcı listesi
    public async Task<IActionResult> Index()
    {
        var users = await _users.Users
            .AsNoTracking()
            .Include(x => x.Personel)
            .OrderBy(x => x.Email)
            .ToListAsync();

        var roller = new List<string>();

        // DbContext hatasını önlemek için
        // rol sorgularını sırayla çalıştırıyoruz.
        foreach (var user in users)
        {
            var userRoles = await _users.GetRolesAsync(user);
            roller.Add(string.Join(", ", userRoles));
        }

        ViewBag.Roller = roller;

        return View(users);
    }

    // Yeni kullanıcı formu
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FillPersoneller();
        return View(new HesapOlusturViewModel());
    }

    // Yeni kullanıcı oluşturma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        HesapOlusturViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            model.Email = model.Email.Trim();

            var existingUser =
                await _users.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresi zaten kullanılıyor.");
            }
        }

        bool personelVar = await _db.Personeller
            .AnyAsync(x => x.Id == model.PersonelId);

        if (!personelVar)
        {
            ModelState.AddModelError(
                nameof(model.PersonelId),
                "Geçerli bir personel seçiniz.");
        }

        bool hesapVar = await _users.Users
            .AnyAsync(x => x.PersonelId == model.PersonelId);

        if (hesapVar)
        {
            ModelState.AddModelError(
                nameof(model.PersonelId),
                "Bu personelin zaten bir kullanıcı hesabı var.");
        }

        if (!ModelState.IsValid)
        {
            await FillPersoneller();
            return View(model);
        }

        // Personel rolünü kontrol et
        if (!await _roles.RoleExistsAsync("Personel"))
        {
            var roleResult = await _roles.CreateAsync(
                new IdentityRole("Personel"));

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await FillPersoneller();
                return View(model);
            }
        }

        // Identity kullanıcı hesabı
        var user = new UygulamaKullanicisi
        {
            UserName = model.Email,
            Email = model.Email,
            PersonelId = model.PersonelId,
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(
            user,
            model.Parola);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            await FillPersoneller();
            return View(model);
        }

        // Personel rolünü ata
        var addRoleResult = await _users.AddToRoleAsync(
            user,
            "Personel");

        if (!addRoleResult.Succeeded)
        {
            await _users.DeleteAsync(user);

            foreach (var error in addRoleResult.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            await FillPersoneller();
            return View(model);
        }

        TempData["Success"] =
            "Personel hesabı başarıyla oluşturuldu.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(string id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        var currentUser = await _users.GetUserAsync(User);

        if (currentUser?.Id == user.Id ||
            await _users.IsInRoleAsync(user, "Yonetici"))
        {
            TempData["Error"] =
                "Yönetici hesabının durumu değiştirilemez.";

            return RedirectToAction(nameof(Index));
        }

        bool pasif = user.LockoutEnd.HasValue &&
                     user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        IdentityResult result;

        if (pasif)
        {
            result = await _users.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            result = await _users.SetLockoutEnabledAsync(user, true);

            if (result.Succeeded)
            {
                result = await _users.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.MaxValue);
            }
        }


        if (result.Succeeded)
        {
            if (!pasif)
            {
                var stampResult =
                    await _users.UpdateSecurityStampAsync(user);

                if (!stampResult.Succeeded)
                {
                    TempData["Error"] =
                        "Hesap pasifleştirildi ancak oturum güvenliği güncellenemedi.";

                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = pasif
                ? "Kullanıcı hesabı aktifleştirildi."
                : "Kullanıcı hesabı pasifleştirildi.";
        }

        else
        {
            TempData["Error"] = string.Join(
                ", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }


    // Kullanıcı hesabı silme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        var currentUser = await _users.GetUserAsync(User);

        // Yönetici kendi hesabını silemez
        if (currentUser?.Id == user.Id)
        {
            TempData["Error"] =
                "Kendi hesabınızı silemezsiniz.";

            return RedirectToAction(nameof(Index));
        }

        // Yönetici hesabı silinemez
        if (await _users.IsInRoleAsync(user, "Yonetici"))
        {
            TempData["Error"] =
                "Yönetici hesabı bu ekrandan silinemez.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _users.DeleteAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] =
                "Kullanıcı hesabı silindi. Personel kaydı korundu.";
        }
        else
        {
            TempData["Error"] =
                "Hesap silinemedi: " +
                string.Join(", ",
                    result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    // Hesabı olmayan personelleri getir
    private async Task FillPersoneller()
    {
        var used = await _users.Users
            .Where(x => x.PersonelId != null)
            .Select(x => x.PersonelId)
            .ToListAsync();

        var people = await _db.Personeller
            .AsNoTracking()
            .Where(x => !used.Contains(x.Id))
            .OrderBy(x => x.Ad)
            .ThenBy(x => x.Soyad)
            .ToListAsync();

        ViewBag.Personeller = new SelectList(
            people.Select(x => new
            {
                x.Id,
                Ad = x.Ad + " " + x.Soyad
            }),
            "Id",
            "Ad");
    }
}
