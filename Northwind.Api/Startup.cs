using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Northwind.Data.Models;
using Northwind.Service;
using Urf.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF;
using URF.Core.EF.Trackable;

namespace Northwind.Api
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
            services.AddMvc()
                .AddJsonOptions(options =>
                    options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All);

            services.AddOData();

            var connectionString = Configuration.GetConnectionString(nameof(NorthwindContext));
            services.AddDbContext<NorthwindContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<DbContext, NorthwindContext>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<ITrackableRepository<Products>, TrackableRepository<Products>>();
            services.AddTransient<IProductService, ProductService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            var oDataConventionModelBuilder = new ODataConventionModelBuilder(app.ApplicationServices);
            var entitySetConfiguration = oDataConventionModelBuilder.EntitySet<Products>(nameof(Products));        
            entitySetConfiguration.EntityType.HasKey(x => x.ProductId);
            entitySetConfiguration.EntityType.Ignore(x => x.Category);
            entitySetConfiguration.EntityType.Ignore(x => x.Supplier);
            entitySetConfiguration.EntityType.Ignore(x => x.OrderDetails);

            app.UseMvc(routeBuilder =>
                {
                    routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
                    routeBuilder.MapODataServiceRoute("ODataRoute", "odata", oDataConventionModelBuilder.GetEdmModel()); 
                    routeBuilder.EnableDependencyInjection();
                }
            );
        }
    }
}
