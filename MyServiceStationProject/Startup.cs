using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyServiceStation.Controllers;
using MyServiceStationProject.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyServiceStationProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).
                AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    //options.AccessDeniedPath = "/denied";
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnSigningIn = async context =>
                        {
                            //var principal = context.Principal;
                            //if (principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            //{
                            //    string login = principal.Claims.FirstOrDefault(c => c.Type == "username").Value;
                                
                            //    var val = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;

                            //    if (val == login)
                            //    {
                            //        var claimsIdentity = principal.Identity as ClaimsIdentity;
                            //        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                            //    }
                            //    else
                            //    {
                            //        var claimsIdentity = principal.Identity as ClaimsIdentity;
                            //        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "User"));
                            //    }
                            //}
                            await Task.CompletedTask;
                        },
                        OnSignedIn = async context =>
                        {
                            await Task.CompletedTask;
                        },
                        OnValidatePrincipal = async context =>
                        {
                            await Task.CompletedTask;
                        }
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
