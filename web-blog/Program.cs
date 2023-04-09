using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using web_blog.Context;
using web_blog.Entities;
using web_blog.Repository;
using web_blog.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});


// For Identity
builder.Services.AddIdentity<BlogUser, IdentityRole>()
    .AddEntityFrameworkStores<BlogDbContext>()
    .AddDefaultTokenProviders();

// adding Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters =
        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]))
        };
});

// For entity framework
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("web_blog"))
);

// For auto mapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// inject repository
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ArticleRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ArticleCategoryRepository>();
// inject service
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<ImageService>();

// paging
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UriService>(o =>
{
    var accessor = o.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext.Request;
    var uri = string.Concat(request.Scheme, "://", request.Host.ToUriComponent());
    return new UriService(uri);
});
builder.Services.AddControllers();


builder.Services.AddCors(p => p.AddPolicy("corsPolicy", policy =>
{
    policy.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("corsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();