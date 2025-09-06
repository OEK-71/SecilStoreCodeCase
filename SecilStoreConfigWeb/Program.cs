using SecilStoreCodeCase;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IConfigStore>(sp =>
{
    var cs  = builder.Configuration.GetConnectionString("SecilStoreCodeCase")
        ?? throw new InvalidOperationException("Connection string 'SecilStoreCodeCase' not found.");

    return new SqlConfigStore(cs);
});

builder.Services.AddSingleton(sp =>
{
    var cs = builder.Configuration.GetConnectionString("SecilStoreCodeCase")!;
    return new ConfigurationReader("SERVICE-A", cs, refreshTimerIntervalInMs: 5000);
});


//builder.Services.AddSingleton(sp =>
//{
//    var cs = builder.Configuration.GetConnectionString("SecilStoreCodeCase")!;
//    return new ConfigurationReader("SERVICE-B", cs, refreshTimerIntervalInMs: 5000);
//});
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Config}/{action=Index}/{id?}");
app.Run();