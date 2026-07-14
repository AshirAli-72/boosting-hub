using BoostingHub.backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoostingHub.backend.Data.DesignTimeContexts;

public class AppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = "Data Source=boostinghubdb.mssql.somee.com;Initial Catalog=boostinghubdb;User ID=ashir_ali_SQLLogin_1;Password=el3dgbdjiu;TrustServerCertificate=True;MultipleActiveResultSets=True";
        optionsBuilder.UseSqlServer(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
