﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Users.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Users.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Users
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration) => this.Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IPasswordValidator<AppUser>, CustomPasswordValidator>();
            services.AddTransient<IUserValidator<AppUser>, CustomNameValidator>();
            services.AddSingleton<IClaimsTransformation, LocationClaimsTransformation>();
            services.AddTransient<IAuthorizationHandler, BlockUsersHandler>();
            services.AddTransient<IAuthorizationHandler, DocumentAuthorization>();

            services.AddAuthorization(opts =>
            {
                opts.AddPolicy("DCUsers", policyBuilder => 
                {
                    policyBuilder.RequireClaim(ClaimTypes.StateOrProvince, "DC");
                    policyBuilder.RequireRole("Users");
                });
                opts.AddPolicy("NoYJ", policyBuilder => 
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.AddRequirements(new BlockUsersRequirement("yj"));
                });
                opts.AddPolicy("AuthorsAndEditors", policyBuilder => 
                {
                    policyBuilder.AddRequirements(new DocumentAuthorizationRequirement
                    {
                        AllowAuthors = true,
                        AllowEditors = true
                    });
                });
            });

            services.AddAuthentication().AddGoogle(opt =>
            {
                opt.ClientId = "338992974621-fmso966e6k7dpik8fqo2j38v8eru6ie2.apps.googleusercontent.com";
                opt.ClientSecret = "wElTmddTAPBXk7ZM20FoY8_O";
            });

            services.AddDbContext<AppIdentityDbContext>(opts =>
            {
                opts.UseMySQL(this.Configuration["Data:SportStoreIdentity:ConnectionString"]);
            });

            services.AddIdentity<AppUser, IdentityRole>(opts =>
            {
                opts.Password.RequiredLength = 6;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireDigit = false;
                opts.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStatusCodePages();
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
            //AppIdentityDbContext.CreateAdminAccount(app.ApplicationServices, this.Configuration).Wait();
        }
    }
}
