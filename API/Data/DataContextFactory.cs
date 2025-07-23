using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();

            // Use the same database provider as your app, e.g. SQLite here
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=datingapp_db_s8cl_user;Password=xUpnGUGfrkK8SWTG6tpC4n9SQW5iwiIo;Database=datingapp");


            return new DataContext(optionsBuilder.Options);
        }
    }
}
