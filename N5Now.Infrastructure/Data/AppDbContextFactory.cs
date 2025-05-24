using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace N5Now.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Asegúrate que sea la misma ruta que usas en Docker
            optionsBuilder.UseSqlite("Data Source=/app/data/app.db");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
