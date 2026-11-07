using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;

namespace PersonelIzinSistemi.Controllers;

[Authorize(Roles = "Yonetici")]
public class PersonellerController : Controller
{
    private readonly AppDbContext _context;
    public PersonellerController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? ara, string? departman)
    {
        var query = _context.Personeller.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ara))
        {
            var term = ara.Trim();
            query = query.Where(x => x.Ad.Contains(term) || x.Soyad.Contains(term) || x.Departman.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(departman))
            query = query.Where(x => x.Departman == departman);
        ViewBag.Ara = ara;
        ViewBag.Departman = departman;
        ViewBag.Departmanlar = await _context.Personeller.AsNoTracking().Select(x => x.Departman).Distinct().OrderBy(x => x).ToListAsync();
        return View(await query.OrderBy(x => x.Ad).ThenBy(x => x.Soyad).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Personel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Personel personel)
    {
        ValidatePersonel(personel);
        if (!ModelState.IsValid) return View(personel);
        personel.Id = 0;
        _context.Personeller.Add(personel);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Personel başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var personel = await _context.Personeller.FindAsync(id);
        return personel is null ? NotFound() : View(personel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Personel input)
    {
        if (id != input.Id) return BadRequest();
        ValidatePersonel(input);
        if (!ModelState.IsValid) return View(input);
        var personel = await _context.Personeller.FindAsync(id);
        if (personel is null) return NotFound();
        personel.Ad = input.Ad.Trim();
        personel.Soyad = input.Soyad.Trim();
        personel.Departman = input.Departman.Trim();
        personel.Maas = input.Maas;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Personel bilgileri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var personel = await _context.Personeller.FindAsync(id);
        if (personel is null) return NotFound();
        if (await _context.IzinTalepleri.AnyAsync(i => i.PersonelId == id))
        {
            TempData["Error"] = "İzin geçmişi olan personel silinemez.";
            return RedirectToAction(nameof(Index));
        }
        if (await _context.Set<UygulamaKullanicisi>().AnyAsync(u => u.PersonelId == id))
        {
            TempData["Error"] = "Bu personele bağlı kullanıcı hesabı var. Önce kullanıcı hesabını kaldırın.";
            return RedirectToAction(nameof(Index));
        }
        _context.Personeller.Remove(personel);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Personel silindi.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidatePersonel(Personel p)
    {
        if (string.IsNullOrWhiteSpace(p.Ad) || p.Ad.Length > 50) ModelState.AddModelError(nameof(p.Ad), "Ad 1-50 karakter olmalıdır.");
        if (string.IsNullOrWhiteSpace(p.Soyad) || p.Soyad.Length > 50) ModelState.AddModelError(nameof(p.Soyad), "Soyad 1-50 karakter olmalıdır.");
        if (string.IsNullOrWhiteSpace(p.Departman) || p.Departman.Length > 50) ModelState.AddModelError(nameof(p.Departman), "Departman 1-50 karakter olmalıdır.");
        if (p.Maas < 0 || p.Maas > 99999999) ModelState.AddModelError(nameof(p.Maas), "Geçerli bir maaş girin.");
    }
}
