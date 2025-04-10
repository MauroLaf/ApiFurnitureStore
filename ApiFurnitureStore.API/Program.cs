using ApiFurnitureStore.API.Configuration;
using ApiFurnitureStore.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//agregamos las dependencias y le decimos a que cadena y gestor conectaremos
//con las options que configuramos en context las configuramos aqui
builder.Services.AddDbContext<ApiFurnitureStoreContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("APiFurnitureStoreContext")));
//inyecto en este contenedor de dependencias la clase jwtconfig (es de tipo configuration)le digo el tipo de la clase que voy a configurar, en base a lo que esta en upsettings
//leera lo que esta en la section jwtconfig y lo mapee al objeto <jwtConfig>
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

//agregamos el addautentication y agregaremos options
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwtf =>
    {
        var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtConfig:Secret").Value);
        jwtf.SaveToken = true; //para que almacene el token si es valido
        jwtf.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, //normalmente deberia estar en true
            ValidateAudience = true, //deberia tamb estar true para validar el destinatario
            RequireExpirationTime = false, //por ahora dejaremos falso deberia estar true
            ValidateLifetime = true,
        };
    });
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
        .AddEntityFrameworkStores<ApiFurnitureStoreContext>();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
