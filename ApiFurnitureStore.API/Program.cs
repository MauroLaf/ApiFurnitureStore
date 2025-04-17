using ApiFurnitureStore.API.Configuration;
using ApiFurnitureStore.API.Services;
using ApiFurnitureStore.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    //agrego codigo para seguir usando swager con authoraize
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Furniture_Store_API",
            Version = "v1"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = $@"JWT Authorization header using the Bearer Scheme.
                      Enter prefix(Bearer), space, and then your token.
                      Example: 'Bearer 123453rgfgrtefw'"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string []{}
        }
    });
    });

    //agregamos las dependencias y le decimos a que cadena y gestor conectaremos
    //con las options que configuramos en context las configuramos aqui
    builder.Services.AddDbContext<ApiFurnitureStoreContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("APiFurnitureStoreContext")));

    //inyecto en este contenedor de dependencias la clase jwtconfig (es de tipo configuration)le digo el tipo de la clase que voy a configurar, en base a lo que esta en upsettings
    //leera lo que esta en la section jwtconfig y lo mapee al objeto <jwtConfig>
    builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings")); //mapeo esta clase a options, le digo carga la sección SmtpSettings del appsettings.json dentro de una clase SmtpSettings y para que esté disponible a traves de IOptions<SmtpSettings>.
                                                                                                //Email
    builder.Services.AddSingleton<IEmailSender, EmailService>();


    var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtConfig:Secret").Value);
    var tokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, //normalmente deberia estar en true
        ValidateAudience = false, //deberia tamb estar true para validar el destinatario pero lo cambie y me no me mostraba informacion al autorizar con el token
        RequireExpirationTime = false, //por ahora dejaremos falso deberia estar true
        ValidateLifetime = true,
    };

    builder.Services.AddSingleton(tokenValidationParameters);

    //agregamos el addautentication y agregaremos options
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(jwtf =>
        {

            jwtf.SaveToken = true; //para que almacene el token si es valido
            jwtf.TokenValidationParameters = tokenValidationParameters;
        });

    // Configuración de identidad para los usuarios
    builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<ApiFurnitureStoreContext>();

    /*Nlog*/
    builder.Logging.ClearProviders();//para limpiar cualquier cosa que haya
    builder.Host.UseNLog();


    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();   // Swagger debe estar disponible en desarrollo
        app.UseSwaggerUI(); // Swagger UI para visualizar la documentación
    }

    app.UseHttpsRedirection();  // Redirigir tráfico HTTP a HTTPS

    // Agrega el middleware de autenticación para verificar el token JWT
    app.UseAuthentication();

    // Después de autenticar, se aplica autorización
    app.UseAuthorization();

    app.MapControllers(); // Mapea los controladores

    app.Run(); // Ejecuta la aplicación
}
catch (Exception e)
{
    logger.Error(e, "There has been an error");
    throw;
}
finally
{
    NLog.LogManager.Shutdown(); //termina de ejecutar log
}


