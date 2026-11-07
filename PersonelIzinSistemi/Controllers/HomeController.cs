
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;

namespace PersonelIzinSistemi.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Personel hesabı ana sayfaya gelirse
            // kendi izin taleplerine yönlendirilir.
            if (User.IsInRole("Personel") &&
                !User.IsInRole("Yonetici"))
            {
                return RedirectToAction(
                    "Index",
                    "IzinTalepleri");
            }

            // Yönetici olmayan diğer roller
            // yönetim paneline erişemez.
            if (!User.IsInRole("Yonetici"))
            {
                return Forbid();
            }

            // Personel istatistikleri
            ViewBag.PersonelSayisi = await _context.Personeller
                .CountAsync();

            ViewBag.DepartmanSayisi = await _context.Personeller
                .Select(p => p.Departman)
                .Distinct()
                .CountAsync();

            ViewBag.ToplamMaas = await _context.Personeller
                .SumAsync(p => (decimal?)p.Maas) ?? 0m;

            // Son eklenen 5 personel
            ViewBag.SonPersoneller = await _context.Personeller
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            // İzin istatistikleri
            ViewBag.ToplamIzin = await _context.IzinTalepleri
                .CountAsync();

            ViewBag.BekleyenIzin = await _context.IzinTalepleri
                .CountAsync(i => i.Durum == "Beklemede");

            ViewBag.OnaylananIzin = await _context.IzinTalepleri
                .CountAsync(i => i.Durum == "Onaylandı");

            ViewBag.ReddedilenIzin = await _context.IzinTalepleri
                .CountAsync(i => i.Durum == "Reddedildi");

            // Son 5 izin talebi
            ViewBag.SonIzinler = await _context.IzinTalepleri
                .Include(i => i.Personel)
                .OrderByDescending(i => i.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Yonetici")]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
