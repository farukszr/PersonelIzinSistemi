
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;

namespace PersonelIzinSistemi.Controllers
{
    [Authorize(Roles = "Yonetici")]
    public class IzinIslemGecmisiController : Controller
    {
        private readonly AppDbContext _context;

        public IzinIslemGecmisiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gecmis = await _context.IzinIslemGecmisleri
                .AsNoTracking()
                .OrderByDescending(x => x.IslemTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var kullanicilar = await _context.Users
            .AsNoTracking()
            .ToDictionaryAsync(
            x => x.Id,
            x => x.UserName ?? "Bilinmeyen Kullanıcı");

            var izinler = await _context.IzinTalepleri
                .AsNoTracking()
                .Include(x => x.Personel)
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Personel != null
                        ? x.Personel.Ad + " " + x.Personel.Soyad
                        : "Bilinmeyen Personel");

            // Yönetici etiketi belirli bir e-posta adresine bağlı değildir.
            var yoneticiIds = await (from userRole in _context.UserRoles
                                    join role in _context.Roles on userRole.RoleId equals role.Id
                                    where role.Name == "Yonetici"
                                    select userRole.UserId).ToListAsync();
            foreach (var id in yoneticiIds)
                if (kullanicilar.ContainsKey(id))
                    kullanicilar[id] = "Sistem Yöneticisi";

            ViewBag.Kullanicilar = kullanicilar;
            ViewBag.IzinPersonelleri = izinler;

            return View(gecmis);
        }

    }
}
