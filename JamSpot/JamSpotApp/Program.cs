using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Services;
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

            //builder.Services.AddAuthentication()
            //    .AddGoogle(options =>
            //    {
            //        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
            //        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            //    })
            //    .AddApple(options =>
            //    {
            //        options.ClientId = builder.Configuration["Authentication:Apple:ClientId"];
            //        options.KeyId = builder.Configuration["Authentication:Apple:KeyId"];
            //        options.TeamId = builder.Configuration["Authentication:Apple:TeamId"];
            //    });


            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddHostedService<DeleteOldEventsService>();

            var app = builder.Build();

            UserRoles(app);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // За 500 грешки
                app.UseExceptionHandler("/Error");

                // За 404 грешки
                app.UseStatusCodePagesWithReExecute("/Error/404");

                // HSTS
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "admin",
                pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static void UserRoles(WebApplication app)
        {
            // Създаване на роли и потребител при стартиране
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userManager = services.GetRequiredService<UserManager<User>>();

                // Роли, които искаме да добавим
                string[] roleNames = { "Admin", "User", "GroupAdmin", "GroupMember" };
                foreach (var roleName in roleNames)
                {
                    if (!roleManager.RoleExistsAsync(roleName).Result)
                    {
                        roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName }).Wait();
                    }
                }

                // Добавяне на администраторски потребител
                var adminEmail = "admin@example.com";
                var adminUser = userManager.FindByEmailAsync(adminEmail).Result;

                if (adminUser == null)
                {
                    var newAdmin = new User
                    {
                        ProfilePicture = "/Pictures/Admin.jpg",
                        UserName = "Admin",
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    var result = userManager.CreateAsync(newAdmin, "AdminPassword123!").Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(newAdmin, "Admin").Wait();
                    }
                }
            }
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
