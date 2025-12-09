using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Vahicle_Rental_System.Models;

namespace Vahicle_Rental_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}