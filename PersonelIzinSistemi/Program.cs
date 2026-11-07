using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonelIzinSistemi.Data;
using PersonelIzinSistemi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<UygulamaKullanicisi, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Hesap/Giris";
    options.AccessDeniedPath = "/Hesap/Yetkisiz";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddControllersWithViews();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Anasayfa}/{action=Index}/{id?}").WithStaticAssets();
// İlk yönetici, ortam değişkenleri veya user-secrets ile sağlanır. Sabit parola bulunmaz.
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var email = config["IlkYonetici:Email"];
    var password = config["IlkYonetici:Password"];
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Yonetici", "Personel" })
        if (!await roles.RoleExistsAsync(role))
        {
        var result = await roles.CreateAsync(new IdentityRole(role));
        if (!result.Succeeded) throw new InvalidOperationException("Rol oluşturulamadı: " + string.Join(", ", result.Errors.Select(x => x.Description)));
        }
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UygulamaKullanicisi>>();
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            user = new UygulamaKullanicisi { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException("Yönetici oluşturulamadı: " + string.Join(", ", result.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(user, "Yonetici");
        }
    }
}
app.Run();
