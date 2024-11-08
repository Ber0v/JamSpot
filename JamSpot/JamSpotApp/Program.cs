using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace JamSpotApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<JamSpotDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services
                .AddIdentity<User, IdentityRole<Guid>>(options =>
                {
                    ConfigureIdentity(options, builder);
                })
                .AddEntityFrameworkStores<JamSpotDbContext>()
                .AddRoles<IdentityRole<Guid>>()
                .AddSignInManager<SignInManager<User>>()
                .AddUserManager<UserManager<User>>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static void ConfigureIdentity(IdentityOptions options, WebApplicationBuilder builder)
        {
            options.Password.RequireDigit =
                                   builder.Configuration.GetValue<bool>("Identity:Password:RequireDigit");
            options.Password.RequireLowercase =
                builder.Configuration.GetValue<bool>("Identity:Password:RequireLowercase");
            options.Password.RequireUppercase =
                builder.Configuration.GetValue<bool>("Identity:Password:RequireUppercase");
            options.Password.RequireNonAlphanumeric =
                builder.Configuration.GetValue<bool>("Identity:Password:RequireNonAlphanumeric");
            options.Password.RequiredLength =
                builder.Configuration.GetValue<int>("Identity:Password:RequiredLength");
            options.Password.RequiredUniqueChars =
                builder.Configuration.GetValue<int>("Identity:Password:RequiredUniqueChars");

            options.SignIn.RequireConfirmedAccount =
                builder.Configuration.GetValue<bool>("Identity:SignIn:RequireConfirmedAccount");
            options.SignIn.RequireConfirmedEmail =
                builder.Configuration.GetValue<bool>("Identity:SignIn:RequireConfirmedEmail");
            options.SignIn.RequireConfirmedPhoneNumber =
                builder.Configuration.GetValue<bool>("Identity:SignIn:RequireConfirmedPhoneNumber");

            options.User.RequireUniqueEmail =
                builder.Configuration.GetValue<bool>("Identity:User:RequireUniqueEmail");

            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        }
    }
}
