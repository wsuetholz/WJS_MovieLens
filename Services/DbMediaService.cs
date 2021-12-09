using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WJS_MovieLens.Model;

namespace WJS_MovieLens.Services
{
    //public static class DataTableExtensions
    //{
    //    public static DataTable ToDataTable<T>(this DbSet<T> data)
    //    {
    //        PropertyDescriptorCollection properties =
    //            TypeDescriptor.GetProperties(typeof(T));
    //        DataTable table = new DataTable();
    //        foreach (PropertyDescriptor prop in properties)
    //            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
    //        foreach (T item in data)
    //        {
    //            DataRow row = table.NewRow();
    //            foreach (PropertyDescriptor prop in properties)
    //                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
    //            table.Rows.Add(row);
    //        }
    //        return table;
    //    }
    //}
    //
    
    public class DbMediaService : DbContext
    {
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<Occupation> Occupations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserMovie> UserMovies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseLazyLoadingProxies().UseSqlServer(@"Server=bitsql.wctc.edu;Database=mmcarthey_22097_Movie;User ID=mmcarthey;Password=000075813;");
            optionsBuilder.UseSqlServer(@"Server=bitsql.wctc.edu;Database=mmcarthey_12090_Movie;User ID=mmcarthey;Password=000075813;");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlServer(configuration.GetConnectionString("MovieLensContext"));
        }
    }
}
