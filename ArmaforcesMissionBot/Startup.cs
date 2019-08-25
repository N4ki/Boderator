using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
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
                foreach(var mission in missions.Missions.Where(x => x.Editing == false))
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

            routeBuilder.MapGet("/signup", async context =>
            {
                if (context.Request.Query.Keys.Contains("missionID") &&
                   context.Request.Query.Keys.Contains("userID") &&
                   context.Request.Query.Keys.Contains("teamID") &&
                   context.Request.Query.Keys.Contains("slotID"))
                {
                    var missions = Program.GetMissions();
                    ulong missionID = ulong.Parse(context.Request.Query["missionID"]);
                    ulong userID = ulong.Parse(context.Request.Query["userID"]);
                    ulong teamID = ulong.Parse(context.Request.Query["teamID"]);
                    string slotID = context.Request.Query["slotID"];

                    missions.BanAccess.Wait(-1);
                    try
                    {
                        if (missions.SignupBans.ContainsKey(userID) ||
                            missions.SpamBans.ContainsKey(userID))
                        {
                            context.Response.StatusCode = 503;
                            await context.Response.WriteAsync("Banned");
                        }
                    }
                    finally
                    {
                        missions.BanAccess.Release();
                    }

                    if(missions.Missions.Any(x => x.SignupChannel == missionID))
                    {
                        var mission = missions.Missions.Single(x => x.SignupChannel == missionID);

                        mission.Access.Wait(-1);
                        try
                        {
                            if(!mission.SignedUsers.Contains(userID))
                            {
                                if(mission.Teams.Any(x => x.TeamMsg == teamID))
                                {
                                    var team = mission.Teams.Single(x => x.TeamMsg == teamID);

                                    if(team.Slots.Any(x => x.Key == slotID && x.Value > team.Signed.Where(y => y.Value == x.Key).Count()))
                                    {
                                        var channel = Program.GetChannel(missionID);
                                        var teamMsg = await channel.GetMessageAsync(teamID) as IUserMessage;

                                        var embed = teamMsg.Embeds.Single();

                                        if (!mission.SignedUsers.Contains(userID))
                                        {
                                            var slot = team.Slots.Single(x => x.Key == slotID);
                                            team.Signed.Add(Program.GetGuildUser(userID).Mention, slot.Key);
                                            mission.SignedUsers.Add(userID);

                                            var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                            var newEmbed = new EmbedBuilder
                                            {
                                                Title = embed.Title,
                                                Description = newDescription,
                                                Color = embed.Color
                                            };

                                            await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                            await context.Response.WriteAsync("Success");
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            mission.Access.Release();
                        }
                    }

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Data invalid");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Wrong request");
                }
            });

            routeBuilder.MapGet("/signoff", async context =>
            {
                if (context.Request.Query.Keys.Contains("missionID") &&
                   context.Request.Query.Keys.Contains("userID") &&
                   context.Request.Query.Keys.Contains("teamID") &&
                   context.Request.Query.Keys.Contains("slotID"))
                {
                    var missions = Program.GetMissions();
                    ulong missionID = ulong.Parse(context.Request.Query["missionID"]);
                    ulong userID = ulong.Parse(context.Request.Query["userID"]);
                    ulong teamID = ulong.Parse(context.Request.Query["teamID"]);
                    string slotID = context.Request.Query["slotID"];

                    missions.BanAccess.Wait(-1);
                    try
                    {
                        if (missions.SignupBans.ContainsKey(userID) ||
                            missions.SpamBans.ContainsKey(userID))
                        {
                            context.Response.StatusCode = 503;
                            await context.Response.WriteAsync("Banned");
                        }
                    }
                    finally
                    {
                        missions.BanAccess.Release();
                    }

                    if (missions.Missions.Any(x => x.SignupChannel == missionID))
                    {
                        var mission = missions.Missions.Single(x => x.SignupChannel == missionID);

                        mission.Access.Wait(-1);
                        try
                        {
                            if (mission.SignedUsers.Contains(userID))
                            {
                                if (mission.Teams.Any(x => x.TeamMsg == teamID))
                                {
                                    var team = mission.Teams.Single(x => x.TeamMsg == teamID);

                                    if (team.Slots.Any(x => x.Key == slotID))
                                    {
                                        var channel = Program.GetChannel(missionID);
                                        var teamMsg = await channel.GetMessageAsync(teamID) as IUserMessage;

                                        var embed = teamMsg.Embeds.Single();

                                        if (mission.SignedUsers.Contains(userID))
                                        {
                                            var slot = team.Slots.Single(x => x.Key == slotID);
                                            team.Signed.Remove(Program.GetGuildUser(userID).Mention);
                                            mission.SignedUsers.Remove(userID);

                                            var newDescription = Helpers.MiscHelper.BuildTeamSlots(team);

                                            var newEmbed = new EmbedBuilder
                                            {
                                                Title = embed.Title,
                                                Description = newDescription,
                                                Color = embed.Color
                                            };

                                            await teamMsg.ModifyAsync(x => x.Embed = newEmbed.Build());
                                            await context.Response.WriteAsync("Success");
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            mission.Access.Release();
                        }
                    }

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Data invalid");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Wrong request");
                }
            });

            routeBuilder.MapGet("/emotes", context =>
            {
                var emotes = Program.GetEmotes();
                JArray emotesArray = new JArray();
                foreach(var emote in emotes)
                {
                    var emoteObj = new JObject();
                    emoteObj.Add("id", $"<:{emote.Name}:{emote.Id}>");
                    emoteObj.Add("url", emote.Url);

                    emotesArray.Add(emoteObj);
                }
                return context.Response.WriteAsync($"{emotesArray.ToString()}");
            });

            routeBuilder.MapGet("/users", context =>
            {
                var users = Program.GetUsers();
                JArray usersArray = new JArray();
                foreach(var user in users)
                {
                    var userObj = new JObject();
                    userObj.Add("id", user.Mention);
                    userObj.Add("name", user.Username);

                    usersArray.Add(userObj);
                }
                return context.Response.WriteAsync($"{usersArray.ToString()}");
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
