# Personel İzin Sistemi — v1.0

ASP.NET Core MVC (.NET 10), Entity Framework Core, SQL Server ve ASP.NET Core Identity ile geliştirilmiş personel izin yönetimi uygulaması.

## Özellikler

- Yönetici ve personel rollerine göre giriş ve yetkilendirme
- Personel ve kullanıcı hesabı yönetimi; aktif/pasif hesaplar
- İzin başvurusu, onay/red, karar geri alma, arşivleme
- İzin işlem geçmişi ve yönetici özet ekranı
- Sunucu tarafı izin tarihleri ve çakışma kontrolleri

## Kurulum

1. Visual Studio 2022 veya .NET 10 SDK, SQL Server Express kurun.
2. `PersonelIzinSistemi.slnx` çözümünü Visual Studio'da açın.
3. `PersonelIzinSistemi/appsettings.json` dosyasındaki `DefaultConnection` bağlantısını kendi SQL Server örneğinize göre düzenleyin.
4. `PersonelIzinSistemi` projesinin bulunduğu klasörde `dotnet ef database update` çalıştırın (EF CLI gerekirse `dotnet tool install --global dotnet-ef`).
5. Yönetici hesabı oluşturmak için User Secrets veya ortam değişkenleriyle `IlkYonetici:Email` ve `IlkYonetici:Password` tanımlayın. Parolayı kaynak koduna koymayın. Örnek:
   ```powershell
   dotnet user-secrets set "IlkYonetici:Email" "admin@example.com"
   dotnet user-secrets set "IlkYonetici:Password" "KendinizeOzelGuclu123!"
   ```
6. `dotnet run` ile başlatın; `/Hesap/Giris` sayfasından giriş yapın.

> Not: İlk yönetici yalnızca belirtilen e-posta için hesap yoksa oluşturulur. Başlangıç kimlik bilgilerini güvenle saklayın. Projeye gerçek çalışan bilgilerini, veritabanı yedeklerini ve gizli anahtarları eklemeyin.

## Test kontrol listesi

- [ ] Yönetici giriş ve çıkış
- [ ] Personel oluşturma / güncelleme / silme korumaları
- [ ] Kullanıcı oluşturma ve personel hesabıyla giriş
- [ ] Personelin yalnızca kendi izinlerini görmesi
- [ ] İzin talebi tarih ve çakışma kontrolleri
- [ ] Onay, red, geri alma, arşiv ve işlem geçmişi
- [ ] Pasif hesapla girişin engellenmesi ve açık oturumun kapanması
- [ ] Hata ve başarı mesajlarının görünmesi

## Teslim notu

Bu paket kaynak kod incelemesiyle düzenlenmiştir. Bu ortamda .NET SDK/SQL Server bulunmadığından derleme, veritabanı migration ve uçtan uca testler burada çalıştırılamamıştır. Gerçek kullanım öncesinde yukarıdaki kontrolleri yerel ortamda tamamlayın. Bu uygulama bordro, yasal izin bakiyesi hesaplama veya kurumsal üretim güvenlik denetimi sağlamaz.
