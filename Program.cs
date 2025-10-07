using AspNetCore.ReCaptcha;
using System.Net;

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);

//IronBarCode.License.LicenseKey = "IRONSUITE.DSEPEC.GMAIL.COM.29492-A23BEEF5BD-GLJ7C-6ZT4ZCQUBYTT-LYLZMHXQJNES-ZTDZJW4TIHUR-UJQ6RGMHIX2R-GQUCWPJ7IR3S-KQKFAJY3D2TR-NIATGB-TL7GINBZ4T6LUA-DEPLOYMENT.TRIAL-4HJYI5.TRIAL.EXPIRES.20.FEB.2024";


// Add services to the container.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddMvc();
builder.Services.AddLocalization();
builder.Services.AddScoped<UserDataService>(provider => new UserDataService(provider.GetRequiredService<IConfiguration>()));
builder.Services.AddReCaptcha(builder.Configuration.GetSection("GoogleReCaptcha"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseRequestLocalization(new RequestLocalizationOptions().AddSupportedCultures(new[] { "hr-HR" }));

//app.UseAuthorization();

app.MapControllerRoute(name: "default",pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Pdf}");

app.Run();
