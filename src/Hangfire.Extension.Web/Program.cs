using Hangfire;
using Hangfire.Extension.Web.DependencyInjection;
using Hangfire.Extension.Web.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy => policy.RequireAssertion(_ => true));
builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();
});
builder.Services.AddHangfireExtension("/recurring-jobs");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePages();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHangfireExtension("/recurring-jobs");
app.MapRazorPages()
    .WithStaticAssets();

app.Run();

public partial class Program;
