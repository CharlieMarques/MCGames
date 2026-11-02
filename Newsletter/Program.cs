using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newsletter.Data;
using Newsletter.Models;
using Newsletter.Repositories;
using Newsletter.Services;
using Newsletter.Integrations.Steam;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin()   // Permite que cualquier HTML llame a tu API
              .AllowAnyMethod()   // Permite GET, POST, PUT, DELETE
              .AllowAnyHeader();  // Permite enviar Tokens de autorización en los headers
    });
});
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IPlatformRepository, PlatformRepository>();
builder.Services.AddScoped<IPlatformService, PlatformService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddOptions<SteamNewGamesOptions>()
    .Bind(builder.Configuration.GetSection(SteamNewGamesOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddHttpClient<ISteamStoreClient, SteamStoreClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ISteamNewGamesService, SteamNewGamesService>();
builder.Services.AddHostedService<SteamNewGamesWorker>();
builder.Services.AddHostedService<SteamCronService>();
builder.Services.AddDbContext<NewsletterDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("NewsletterDbContext")));
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<NewsletterDbContext>()
    .SetApplicationName("MCGames");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsync(
            "Demasiados pedidos. Esper\u00e1 unos minutos y volv\u00e9 a intentar.", cancellationToken);
    options.AddPolicy("EmailSending", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
});
builder.Services.AddIdentityCore<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProvider<User>.ProviderName;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<NewsletterDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddTokenProvider<PasswordResetTokenProvider<User>>(PasswordResetTokenProvider<User>.ProviderName);

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>

    {
        //options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });


builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api Games", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticacion con Jwt",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
}

    );
var app = builder.Build();
app.MapGet("/", (HttpContext context) =>
{
    context.Response.Redirect("/swagger/index.html", permanent: false);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI();
    app.UseSwagger();
}

app.UseCors("PermitirFrontend");

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] rolesNombres = { "Admin", "Usuario", "Moderador" };
    foreach (var rol in rolesNombres)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            await roleManager.CreateAsync(new IdentityRole(rol));
        }
    }
}
app.Run();
