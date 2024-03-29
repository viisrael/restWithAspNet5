using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using RestWithAspNet5.Business;
using RestWithAspNet5.Business.Implementation;
using RestWithAspNet5.Configurations;
using RestWithAspNet5.Hypermedia.Enricher;
using RestWithAspNet5.Hypermedia.Filters;
using RestWithAspNet5.Model.Context;
using RestWithAspNet5.Repository;
using RestWithAspNet5.Repository.Generic;
using RestWithAspNet5.Services;
using RestWithAspNet5.Services.Implementation;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestWithAspNet5
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var tokenConfigurations = new TokenConfiguration();

            new ConfigureFromConfigurationOptions<TokenConfiguration>(
                Configuration.GetSection("TokenConfigurations")
                ).Configure(tokenConfigurations);

            services.AddSingleton(tokenConfigurations);
            services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
            ).AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters { 
                    ValidateIssuer = true,
                    ValidateAudience = true, 
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = tokenConfigurations.Issuer,
                    ValidAudience = tokenConfigurations.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfigurations.Secret))
                };
            });

            services.AddAuthorization(auth => {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                                            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                            .RequireAuthenticatedUser().Build()
                              ) ;
            });

            services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            services.AddControllers();

            var connection = Configuration["MySqlConnection:MySqlConnectionString"];

            //if (Environment.IsDevelopment())
            //{
               // MigrateDataBase(connection);
            //}

            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.FormatterMappings.SetMediaTypeMappingForFormat("xml", MediaTypeHeaderValue.Parse("application/xml"));
                options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));
            }).AddXmlSerializerFormatters();

            services.AddDbContext<MySqlContext>(options => options.UseMySql(connection, ServerVersion.AutoDetect(connection)));

            var filterOptions = new HyperMediaFilterOptions();
            filterOptions.ContentResponseEnricherList.Add(new PersonEnricher());
            filterOptions.ContentResponseEnricherList.Add(new BookEnricher());

            services.AddSingleton(filterOptions);

            services.AddApiVersioning();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { 
                                                        Title = "REST API's From 0 to Azure With Docker",
                                                        Version = "v1",
                                                        Description= "API RESTFul developed in course 'REST API's From 0 to Azure with ASP.NET 5' ",
                                                        Contact = new OpenApiContact
                                                        {
                                                            Name = "Vially Israel",
                                                            Url = new Uri("https://github.com/Laoviah")
                                                        }
                                                   }
                );
            });


            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();


            services.AddScoped<IPersonBusiness, PersonBusinessImplementation>();
            services.AddScoped<IBooksBusiness, BooksBusinessImplementation>();
            services.AddScoped<ILoginBusiness, LoginBusinessImplementation>();
            services.AddScoped<IFileBusiness, FileBusinessImplementation>();

            services.AddTransient<ITokenService, TokenService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonRepository, PersonRepository>();

            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET From 0 to Azure with Docker");
            });

            var option = new RewriteOptions();
            option.AddRedirect("^$","swagger");
            app.UseRewriter(option);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("DefaultApi", "{controller=values}/{id}");
            });
        }

        private void MigrateDataBase(string connection)
        {
            try
            {
                var evolveConnection = new MySql.Data.MySqlClient.MySqlConnection(connection);
                var evolve = new Evolve.Evolve(evolveConnection, msg => Log.Information(msg))
                {
                    Locations = new List<string> { "database/migrations", "database/dataset" },
                    IsEraseDisabled = true,
                };

                evolve.Migrate();
            }
            catch (System.Exception ex)
            {
                Log.Error("Database migration failed", ex);
                throw;
            }
        }


    }
}
