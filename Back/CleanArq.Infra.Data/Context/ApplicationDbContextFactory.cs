using CleanArq.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.Data.MySqlClient;

namespace CleanArq.Infra.Data.Context
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DockerConnection_Dev")
                ?? "Server=127.0.0.1;Port=3319;Uid=devuser;Pwd=design_time_only;Charset=utf8;Pooling=true;Connection Timeout=120;SslMode=Disabled;";

            var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
            {
                Database = Environment.GetEnvironmentVariable("Database__Name") ?? "CleanArchDB01"
            };

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySQL(connectionStringBuilder.ConnectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
