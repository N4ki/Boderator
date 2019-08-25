using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
using Newtonsoft.Json.Linq;

namespace ArmaforcesMissionBot
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

            routeBuilder.MapGet("/missions", context =>
            {
                var missions = Program.GetMissions();
                JArray missionArray = new JArray();
                foreach(var mission in missions.Missions)
                {
                    var objMission = new JObject();
                    objMission.Add("title", mission.Title);
                    objMission.Add("id", mission.SignupChannel);
                    objMission.Add("freeSlots", Helpers.MiscHelper.CountFreeSlots(mission));
                    objMission.Add("allSlots", Helpers.MiscHelper.CountAllSlots(mission));

                    missionArray.Add(objMission);
                }
                return context.Response.WriteAsync($"{missionArray.ToString()}");
            });

            routeBuilder.MapGet("/mission", context =>
            {
                if (context.Request.Query.Keys.Contains("id"))
                {
                    var missions = Program.GetMissions();

                    var mission = missions.Missions.Single(x => x.SignupChannel == ulong.Parse(context.Request.Query["id"].First()));

                    var serialized = JsonConvert.SerializeObject(mission);
                    return context.Response.WriteAsync($"{serialized}");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync("No `id` provided");
                }
            });

            routeBuilder.MapGet("/signup", context =>
            {
                if (context.Request.Query.Keys.Contains("missionID") &&
                   context.Request.Query.Keys.Contains("userID") &&
                   context.Request.Query.Keys.Contains("teamID") &&
                   context.Request.Query.Keys.Contains("slot"))
                {
                    return context.Response.WriteAsync("Signed up");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync("Wrong request");
                }
            });

            var routes = routeBuilder.Build();
            app.UseRouter(routes);

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
