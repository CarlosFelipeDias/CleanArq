using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArq.Domain.Interfaces;
using CleanArq.Infra.Data.Context;
using CleanArq.Infra.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace CleanArq.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DockerConnection_Dev")
                ?? throw new InvalidOperationException("Connection string 'DockerConnection_Dev' was not configured.");
            var databaseName = configuration["Database:Name"]
                ?? throw new InvalidOperationException("Database name 'Database:Name' was not configured.");
            var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            };

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySQL(connectionStringBuilder.ConnectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            return services;
        }
    }
}
