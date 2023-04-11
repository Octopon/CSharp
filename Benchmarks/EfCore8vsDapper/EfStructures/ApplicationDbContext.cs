using EfCore8vsDapper.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfCore8vsDapper.EfStructures
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
    }
}
