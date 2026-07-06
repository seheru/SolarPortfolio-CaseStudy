using Microsoft.EntityFrameworkCore;
using SolarBackend.Models;

namespace SolarBackend.Data;

// DbContext sınıfından miras alıyoruz. Bu Microsoft'un veritabanı motorudur.
public class AppDbContext : DbContext
{
    // Constructor (Yapıcı Metot): Dışarıdan gelen veritabanı ayarlarını (IP, Şifre) motora verir.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // EĞER BİR YERE DbSet YAZARSAK: 
    // Entity Framework arka planda gider ve PostgreSQL'de "Users" adında bir tablo oluşturur.
    // Artık biz SQL INSERT/SELECT yazmak yerine, bu 'Users' listesine eleman ekleyip çıkaracağız.
    public DbSet<User> Users { get; set; }
}