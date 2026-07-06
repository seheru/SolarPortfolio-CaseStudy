using Microsoft.EntityFrameworkCore;
using SolarBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı bağlantımızı sisteme tanıtıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. GARSONLARI (CONTROLLERS) SİSTEME TANIT (İşte unuttuğumuz satır buydu!)
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. GELEN İSTEKLERİ GARSONLARA YÖNLENDİR (Ve bu satır)
app.MapControllers();

app.Run();