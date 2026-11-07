
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;

namespace PersonelIzinSistemi.Controllers
{
    [Authorize]
    public class IzinTalepleriController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UygulamaKullanicisi> _users;

        public IzinTalepleriController(
            AppDbContext context,
            UserManager<UygulamaKullanicisi> users)
        {
            _context = context;
            _users = users;
        }

        // Aktif izin taleplerini listeleme
        public async Task<IActionResult> Index()
        {
            var query = _context.IzinTalepleri
                .AsNoTracking()
                .Include(i => i.Personel)
                .Where(i => !i.ArsivlendiMi);

            if (!User.IsInRole("Yonetici"))
            {
                var user = await _users.GetUserAsync(User);

                if (user?.PersonelId == null)
                    return Forbid();

                query = query.Where(
                    i => i.PersonelId == user.PersonelId.Value);
            }

            var izinTalepleri = await query
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            return View(izinTalepleri);
        }

        // Arşivlenmiş izinleri listeleme
        [HttpGet]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> Arsiv()
        {
            var izinler = await _context.IzinTalepleri
                .AsNoTracking()
                .Include(i => i.Personel)
                .Where(i => i.ArsivlendiMi)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            return View(izinler);
        }

        // İzin oluşturma sayfası
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _users.GetUserAsync(User);

            if (!User.IsInRole("Yonetici") &&
                user?.PersonelId == null)
            {
                return Forbid();
            }

            ViewBag.Personeller = await _context.Personeller
                .Where(p =>
                    User.IsInRole("Yonetici") ||
                    p.Id == user!.PersonelId)
                .OrderBy(p => p.Ad)
                .ToListAsync();

            return View();
        }

        // Yeni izin talebi oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PersonelId,BaslangicTarihi,BitisTarihi,IzinNedeni")]
            IzinTalebi izinTalebi)
        {
            var currentUser = await _users.GetUserAsync(User);

            if (!User.IsInRole("Yonetici"))
            {
                if (currentUser?.PersonelId == null)
                    return Forbid();

                izinTalebi.PersonelId = currentUser.PersonelId.Value;

                ModelState.Remove(nameof(IzinTalebi.PersonelId));
            }

            izinTalebi.IzinNedeni =
                (izinTalebi.IzinNedeni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(izinTalebi.IzinNedeni))
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.IzinNedeni),
                    "İzin nedeni zorunludur.");
            }
            else if (izinTalebi.IzinNedeni.Length > 1000)
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.IzinNedeni),
                    "İzin nedeni en fazla 1000 karakter olabilir.");
            }

            if (izinTalebi.BaslangicTarihi == default)
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.BaslangicTarihi),
                    "Başlangıç tarihi zorunludur.");
            }

            if (izinTalebi.BitisTarihi == default)
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.BitisTarihi),
                    "Bitiş tarihi zorunludur.");
            }

            if (izinTalebi.BaslangicTarihi != default &&
                izinTalebi.BaslangicTarihi.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.BaslangicTarihi),
                    "Geçmiş tarihli izin talebi oluşturulamaz.");
            }

            if (izinTalebi.BaslangicTarihi != default &&
                izinTalebi.BitisTarihi != default &&
                izinTalebi.BitisTarihi.Date <
                izinTalebi.BaslangicTarihi.Date)
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.BitisTarihi),
                    "Bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            if (izinTalebi.PersonelId <= 0 ||
                !await _context.Personeller.AnyAsync(
                    p => p.Id == izinTalebi.PersonelId))
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.PersonelId),
                    "Geçerli bir personel seçiniz.");
            }

            if (izinTalebi.PersonelId > 0 &&
                izinTalebi.BaslangicTarihi != default &&
                izinTalebi.BitisTarihi != default &&
                izinTalebi.BitisTarihi.Date >=
                izinTalebi.BaslangicTarihi.Date &&
                await _context.IzinTalepleri.AnyAsync(i =>
                    i.PersonelId == izinTalebi.PersonelId &&
                    i.Durum != "Reddedildi" &&
                    i.BaslangicTarihi < izinTalebi.BitisTarihi.Date.AddDays(1) &&
                    i.BitisTarihi >= izinTalebi.BaslangicTarihi.Date))
            {
                ModelState.AddModelError(
                    nameof(IzinTalebi.BaslangicTarihi),
                    "Bu tarihlerde bekleyen veya onaylanan başka bir izin bulunuyor.");
            }

            ModelState.Remove(nameof(IzinTalebi.Personel));
            ModelState.Remove(nameof(IzinTalebi.Durum));
            ModelState.Remove(nameof(IzinTalebi.ArsivlendiMi));

            if (ModelState.IsValid)
            {
                izinTalebi.Id = 0;
                izinTalebi.Durum = "Beklemede";
                izinTalebi.ArsivlendiMi = false;

                _context.IzinTalepleri.Add(izinTalebi);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "İzin talebiniz başarıyla oluşturuldu.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Personeller = await _context.Personeller
                .Where(p =>
                    User.IsInRole("Yonetici") ||
                    (currentUser != null &&
                     p.Id == currentUser.PersonelId))
                .OrderBy(p => p.Ad)
                .ToListAsync();

            return View(izinTalebi);
        }

        // İzin güncellemesini ve işlem geçmişini tek transaction içinde kaydeder.
        private async Task<int> GuncelleVeKaydetAsync(
            int izinId, string islemTuru, string oncekiDurum,
            string yeniDurum, Func<Task<int>> guncelle)
        {
            var kullaniciId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(kullaniciId))
                throw new InvalidOperationException("Oturum açan kullanıcı belirlenemedi.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var etkilenen = await guncelle();
            if (etkilenen == 0)
            {
                await transaction.RollbackAsync();
                return 0;
            }

            _context.IzinIslemGecmisleri.Add(new IzinIslemGecmisi
            {
                IzinTalebiId = izinId,
                IslemTuru = islemTuru,
                OncekiDurum = oncekiDurum,
                YeniDurum = yeniDurum,
                IslemiYapanKullaniciId = kullaniciId,
                IslemTarihi = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return etkilenen;
        }

        // İzin talebini onaylama
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> Onayla(int id)
        {
            var guncellenen = await GuncelleVeKaydetAsync(
                id, "Onaylama", "Beklemede", "Onaylandı",
                () => _context.IzinTalepleri
                    .Where(i => i.Id == id && !i.ArsivlendiMi && i.Durum == "Beklemede")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Durum, "Onaylandı")));

            if (guncellenen == 0)
            {
                TempData["Error"] =
                    "Talep bulunamadı, arşivlenmiş veya daha önce sonuçlandırılmış.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "İzin talebi başarıyla onaylandı.";

            return RedirectToAction(nameof(Index));
        }

        // İzin talebini reddetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> Reddet(int id)
        {
            var guncellenen = await GuncelleVeKaydetAsync(
                id, "Reddetme", "Beklemede", "Reddedildi",
                () => _context.IzinTalepleri
                    .Where(i => i.Id == id && !i.ArsivlendiMi && i.Durum == "Beklemede")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Durum, "Reddedildi")));

            if (guncellenen == 0)
            {
                TempData["Error"] =
                    "Talep bulunamadı, arşivlenmiş veya daha önce sonuçlandırılmış.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "İzin talebi başarıyla reddedildi.";

            return RedirectToAction(nameof(Index));
        }

        // Yanlış verilen kararı geri alma
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> KarariGeriAl(int id)
        {
            var izin = await _context.IzinTalepleri
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    !i.ArsivlendiMi &&
                    (i.Durum == "Onaylandı" ||
                     i.Durum == "Reddedildi"));

            if (izin == null)
            {
                TempData["Error"] =
                    "Geri alınabilecek bir izin kararı bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            if (izin.BaslangicTarihi.Date <= DateTime.Today)
            {
                TempData["Error"] =
                    "Başlamış veya geçmiş tarihli izinlerin kararı doğrudan geri alınamaz.";

                return RedirectToAction(nameof(Index));
            }

            // Başka aktif veya arşivlenmiş onaylı/bekleyen
            // izinlerle tarih çakışmasını engelle
            var cakismaVar = await _context.IzinTalepleri
                .AnyAsync(i =>
                    i.Id != id &&
                    i.PersonelId == izin.PersonelId &&
                    i.Durum != "Reddedildi" &&
                    i.BaslangicTarihi < izin.BitisTarihi.Date.AddDays(1) &&
                    i.BitisTarihi >= izin.BaslangicTarihi.Date);

            if (cakismaVar)
            {
                TempData["Error"] =
                    "Bu tarihlerde başka bir izin bulunduğu için karar geri alınamıyor.";

                return RedirectToAction(nameof(Index));
            }

            var guncellenen = await GuncelleVeKaydetAsync(
                id, "Kararı Geri Alma", izin.Durum, "Beklemede",
                () => _context.IzinTalepleri
                    .Where(i => i.Id == id && !i.ArsivlendiMi &&
                                i.BaslangicTarihi.Date > DateTime.Today &&
                                i.Durum == izin.Durum)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Durum, "Beklemede")));

            if (guncellenen == 0)
            {
                TempData["Error"] =
                    "İzin durumu değiştiği için işlem gerçekleştirilemedi.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "İzin kararı geri alındı. Talep yeniden beklemeye alındı.";

            return RedirectToAction(nameof(Index));
        }

        // Sonuçlandırılmış talebi arşivleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> Arsivle(int id)
        {
            var mevcutDurum = await _context.IzinTalepleri.AsNoTracking()
                .Where(i => i.Id == id && !i.ArsivlendiMi &&
                            (i.Durum == "Onaylandı" || i.Durum == "Reddedildi"))
                .Select(i => i.Durum)
                .FirstOrDefaultAsync();

            var guncellenen = mevcutDurum == null ? 0 : await GuncelleVeKaydetAsync(
                id, "Arşivleme", mevcutDurum + " (Aktif)", mevcutDurum + " (Arşiv)",
                () => _context.IzinTalepleri
                    .Where(i => i.Id == id && !i.ArsivlendiMi && i.Durum == mevcutDurum)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.ArsivlendiMi, true)));

            if (guncellenen == 0)
            {
                TempData["Error"] =
                    "Yalnızca sonuçlandırılmış aktif talepler arşivlenebilir.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "İzin talebi başarıyla arşivlendi.";

            return RedirectToAction(nameof(Index));
        }

        // Arşivdeki talebi aktif listeye geri getirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Yonetici")]
        public async Task<IActionResult> ArsivdenCikar(int id)
        {
            var mevcutDurum = await _context.IzinTalepleri.AsNoTracking()
                .Where(i => i.Id == id && i.ArsivlendiMi)
                .Select(i => i.Durum)
                .FirstOrDefaultAsync();

            var guncellenen = mevcutDurum == null ? 0 : await GuncelleVeKaydetAsync(
                id, "Arşivden Çıkarma", mevcutDurum + " (Arşiv)", mevcutDurum + " (Aktif)",
                () => _context.IzinTalepleri
                    .Where(i => i.Id == id && i.ArsivlendiMi && i.Durum == mevcutDurum)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.ArsivlendiMi, false)));

            if (guncellenen == 0)
            {
                TempData["Error"] =
                    "Arşivde geri getirilebilecek bir kayıt bulunamadı.";

                return RedirectToAction(nameof(Arsiv));
            }

            TempData["Success"] =
                "İzin talebi aktif listeye geri getirildi.";

            return RedirectToAction(nameof(Arsiv));
        }
    }
}
