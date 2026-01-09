using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Models;
using System.Data.Common;

namespace LapTrinhWeb.Data
{
    public class DBContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    }
}