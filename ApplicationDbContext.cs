using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ECommerce_api.Models;

namespace ECommerce_api
{
    public class ApplicationDbContext : DbContext
    {
        //protected override void OnConfiguring(Db_contextOptionsBuilder options)
        //{
        //    //connection t database
        //    options.UseSqlServer(" Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=ECommerce api; Integrated Security=true; TrustServerCertificate=True ");
        //}

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
         : base(options) { }



        // register the models 

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<Review> Reviews { get; set; }

        
    }
}
