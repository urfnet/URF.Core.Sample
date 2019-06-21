#region

using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Northwind.Data.Models;
using Northwind.Repository;
using Northwind.Service;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF;
using URF.Core.EF.Trackable;

#endregion

namespace Northwind.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => 
            Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddMvc();

            services.AddMvc()
                .AddJsonOptions(options =>
                    options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All);

            services.AddOData();

            var connectionString = Configuration.GetConnectionString(nameof(NorthwindContext));
            services.AddDbContext<NorthwindContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<DbContext, NorthwindContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITrackableRepository<Products>, TrackableRepository<Products>>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ITrackableRepository<OrderDetails>, TrackableRepository<OrderDetails>>();
            services.AddScoped<IOrderDetailService, OrderDetailService>();

            // Example: extending IRepository<TEntity>, scope: application-wide and IService<TEntity>, scope: ICustomerService
            services.AddScoped<IRepositoryX<Customers>, RepositoryX<Customers>>();
            services.AddScoped<ICustomerService, CustomerService>();


            services.AddScoped<ITrackableRepository<Categories>, TrackableRepository<Categories>>();
            services.AddScoped<ICategoriesService, CategoriesService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.Build();
            });

            var oDataConventionModelBuilder = new ODataConventionModelBuilder(app.ApplicationServices);

            var categoriesEntitySetConfiguration = oDataConventionModelBuilder.EntitySet<Categories>(nameof(Categories));
            categoriesEntitySetConfiguration.EntityType.HasKey(x => x.CategoryId);

            var customersEntitySetConfiguration = oDataConventionModelBuilder.EntitySet<Customers>(nameof(Customers));
            customersEntitySetConfiguration.EntityType.HasKey(x => x.CustomerId);
            customersEntitySetConfiguration.EntityType.Ignore(x => x.CustomerCustomerDemo);
            customersEntitySetConfiguration.EntityType.Ignore(x => x.Orders);

            var productsEntitySetConfiguration = oDataConventionModelBuilder.EntitySet<Products>(nameof(Products));
            productsEntitySetConfiguration.EntityType.HasKey(x => x.ProductId);
            productsEntitySetConfiguration.EntityType.Ignore(x => x.Category);
            productsEntitySetConfiguration.EntityType.Ignore(x => x.Supplier);

            var orderDetailsEntitySetConfiguration = oDataConventionModelBuilder.EntitySet<OrderDetails>(nameof(OrderDetails));
            orderDetailsEntitySetConfiguration.EntityType.HasKey(x => x.OrderId);
            orderDetailsEntitySetConfiguration.EntityType.HasKey(x => x.ProductId);
            orderDetailsEntitySetConfiguration.EntityType.Ignore(x => x.Order);


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