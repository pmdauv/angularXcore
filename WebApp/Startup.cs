using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using OpenIddict.Core;
using OpenIddict.Models;
using Swashbuckle.AspNetCore.Swagger;
using WebApp.DataAccessLayer;
using WebApp.DataAccessLayer.Models;

namespace WebApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddDbContext<ApplicationDbContext>(
                options =>
                {
                    options.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"));
                    options.UseOpenIddict();
                });

            services.AddDbContext<DataDbContext>(
                options => options.UseSqlServer(this.Configuration.GetConnectionString("DefaultDataConnection")));

            //services.AddDbContext<ApplicationDbContext>(
            //    options => options.UseSqlite(this.Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
            {
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 6;
                o.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromHours(24);
                o.Cookies.ExternalCookie.ExpireTimeSpan = TimeSpan.FromHours(24);
                //o.Cookies.ApplicationCookie.LoginPath = "/login";
                o.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = ctx =>
                    {
                        // Needed so that API returns unauthorized instead of the default page with StatusCode.Ok
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == (int)HttpStatusCode.OK)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                        else if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }
                        return Task.FromResult(0);
                    }
                };
            })
                .AddEntityFrameworkStores<ApplicationDbContext, Guid>()
                .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            var openIdDictBuilder = services.AddOpenIddict()
                // Register the Entity Framework stores.
                .AddEntityFrameworkCoreStores<ApplicationDbContext>()

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                .AddMvcBinders()

                .EnableTokenEndpoint("/connect/token")

                .AllowPasswordFlow()
                .AllowClientCredentialsFlow()

                // During development, you can disable the HTTPS requirement.
                .DisableHttpsRequirement()

                 // Register a new ephemeral key, that is discarded when the application
                 // shuts down. Tokens signed using this key are automatically invalidated.
                 // This method should only be used during development.
                 .AddEphemeralSigningKey();

            openIdDictBuilder.SetAccessTokenLifetime(TimeSpan.FromHours(24));

            // Add framework services.
            services.AddMvc();
            services.AddOptions();
            services.AddNodeServices();

            services.AddSwaggerGen(c =>
          {
              c.SwaggerDoc("v1", new Info { Title = "angularXcore API", Version = "v1" });
              c.AddSecurityDefinition("OpenIdDict", new OAuth2Scheme
              {
                  Type = "oauth2",
                  Flow = "password",
                  TokenUrl = "/connect/token"
              });
          });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<DataLayer, DataLayer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddNLog();

            app.AddNLogWeb();
            env.ConfigureNLog("nlog.config");
            LogManager.Configuration.Variables["connectionString"] = this.Configuration.GetConnectionString("DefaultLogConnection");
            LogManager.Configuration.Variables["configDir"] = "C:\\temp\\";
            var logger = LogManager.GetCurrentClassLogger();

            if (env.IsDevelopment())
            {
                logger.Trace($"Environment in WebApp isDevelopment: {env.EnvironmentName}");
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                logger.Trace($"Environment in WebApp !isDevelopment: {env.EnvironmentName}");
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();

            //app.UseGoogleAuthentication(new GoogleOptions()
            //{
            //    ClientId = Configuration["Authentication:Google:ClientId"],
            //    ClientSecret = Configuration["Authentication:Google:ClientSecret"]
            //});

            //app.UseFacebookAuthentication(new FacebookOptions()
            //{
            //    AppId = Configuration["Authentication:Facebook:AppId"],
            //    AppSecret = Configuration["Authentication:Facebook:AppSecret"]
            //});

            //app.UseMicrosoftAccountAuthentication(new MicrosoftAccountOptions()
            //{
            //    ClientId = Configuration["Authentication:Microsoft:ClientId"],
            //    ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"]
            //});

            app.UseCors(builder =>
                builder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin()
            );

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "angularXcore v1");
            });

            app.UseOAuthValidation();
            app.UseOpenIddict();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });

            this.InitializeAsync(app.ApplicationServices, CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync(cancellationToken);

                var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

                if (await manager.FindByClientIdAsync("TestClient", cancellationToken) == null)
                {
                    var application = new OpenIddictApplication
                    {
                        ClientId = "TestClient",
                        DisplayName = "My TestClient application"
                    };

                    await manager.CreateAsync(application, "388D45FA-B36B-4988-BA59-B187D329C207", cancellationToken);
                }
            }
        }
    }
}