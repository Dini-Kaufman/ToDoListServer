using TodoApi; 
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models; // נוסיף את הספרייה של Swagger
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
var builder = WebApplication.CreateBuilder(args);

// // הוספת DbContext ושימוש במחרוזת חיבור מתוך appsettings.json
// var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
// השגת מחרוזת החיבור ממשתנה סביבה, ואם היא לא קיימת - שימוש בברירת מחדל מה-AppSettings.json
var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? 
                        builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// הוספת Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ToDo API",
        Version = "v1",
        Description = "API לניהול משימות"
    });
});

builder.Services.AddCors(options =>
{
      options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

//  הפעלת Swagger
//if (app.Environment.IsDevelopment()) // מציג את Swagger רק בסביבת פיתוח
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API V1");
    });
//}

app.UseCors("AllowAll");

// שליפת כל המשימות
app.MapGet("/items", async (ToDoDbContext db) =>
    await db.Items.ToListAsync());

// הוספת משימה חדשה
app.MapPost("/items", async (ToDoDbContext db, Item newItem) =>
{
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
});

// עדכון משימה
app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item updatedItem) =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();

    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// מחיקת משימה
app.MapDelete("/items/{id}", async (ToDoDbContext db, int id) =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/", () => "ToDoListServer API is running! 🚀");

Console.WriteLine("Connection String: " + builder.Configuration.GetConnectionString("ToDoDB"));
app.Run();

