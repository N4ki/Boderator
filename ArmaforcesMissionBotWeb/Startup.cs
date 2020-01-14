using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ArmaforcesMissionBotWeb.HelperClasses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace ArmaforcesMissionBotWeb
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
            });

            services.AddRouting();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var trackPackageRouteHandler = new RouteHandler(context =>
            {
                var routeValues = context.GetRouteData().Values;
                return context.Response.WriteAsync(
                    $"Hello! Route values: {string.Join(", ", routeValues)}");
            });

            var routeBuilder = new RouteBuilder(app, trackPackageRouteHandler);

            routeBuilder.MapGet("/api/discord/callback", context =>
            {
                var code = context.Request.Query["code"];

                var redirect = $"{Program.SelfAddress}/api/discord/callback";

                var request = (HttpWebRequest)WebRequest.Create($"https://discordapp.com/api/oauth2/token?grant_type=authorization_code&code={code}&redirect_uri={redirect}");

                var postData = "";
                var data = Encoding.ASCII.GetBytes(postData);

                byte[] bytes = Encoding.GetEncoding(28591).GetBytes(Environment.GetEnvironmentVariable("AF_CLIENT_ID") + ":" + Environment.GetEnvironmentVariable("AF_SECRET"));

                request.Method = "POST";
                request.Headers.Add("Authorization", $"Basic {System.Convert.ToBase64String(bytes)}");

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                var token = JsonConvert.DeserializeObject<AccessTokenResponse>(responseString);

                CookieOptions options = new CookieOptions();
                options.Expires = DateTime.Now.AddSeconds(int.Parse(token.expires_in));

                context.Response.Cookies.Append("Token", token.access_token, options);

                context.Response.Redirect("/Index");

                return context.Response.WriteAsync("Logged");
            });

            var routes = routeBuilder.Build();
            app.UseRouter(routes);

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
