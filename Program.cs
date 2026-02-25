using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using BookkeepingApp.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<FileProcessingService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.WebHost.UseUrls("https://localhost:5000");

var app = builder.Build();

var url = "https://localhost:5000";


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

app.Run(url);

