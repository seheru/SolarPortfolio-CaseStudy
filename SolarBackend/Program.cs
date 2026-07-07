using Microsoft.EntityFrameworkCore;
using SolarBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// BİRİNCİ EKLEME (Kapıyı Angular'a açıyoruz)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Veritabanı bağlantımızı sisteme tanıtıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// İKİNCİ EKLEME (Güvenlik görevlisine talimat veriyoruz)
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
