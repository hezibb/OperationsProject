using Microsoft.EntityFrameworkCore;
using OperationsApi.Models;

namespace OperationsApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<OperationHistory> OperationHistories { get; set; }
    }
}
