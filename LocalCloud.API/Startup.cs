using System.Text;
using System.Threading.Tasks;
using LocalCloud.Data.Models;
using LocalCloud.Interfaces.Services;
using LocalCloud.Services;
using LocalCloud.Storage.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LocalCloud.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var authenticationSection = Configuration.GetSection("Authentication");
            var applicationSettingsSection = Configuration.GetSection("ApplicationSettings");
            var storageSettingsSection = Configuration.GetSection("StorageSettings");

            var applicationSettings = applicationSettingsSection.Get<ApplicationSettings>();
            var key = Encoding.ASCII.GetBytes(applicationSettings.SecretKey);

            services.AddHttpContextAccessor();

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var user = userService.GetByLogin(context.Principal.Identity.Name);
                        if (user == null)
                        {
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.Configure<Authentication>(authenticationSection);
            services.Configure<ApplicationSettings>(applicationSettingsSection);
            services.Configure<StorageSettings>(storageSettingsSection);

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStorage, Storage.Core.Storage>(x =>
            {
                var userService = x.GetService<IUserService>();
                var storageSettings = x.GetService<IOptions<StorageSettings>>().Value;
                if (userService.IsAnonymous)
                {
                    throw new System.AccessViolationException("Authentication error!");
                }
                var root = IStorage.Combine(storageSettings.Root, userService.Current.Author);
                return new Storage.Core.Storage(root);
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
