using Hangfire;
using Hangfire.RecurringJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy => policy.RequireAssertion(_ => true));
builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();
});
builder.Services.AddHangfireRecurringJobs("/recurring-jobs");

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStatusCodePages();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHangfireRecurringJobsApi();
app.MapRazorPages();

app.Run();

public partial class Program;
