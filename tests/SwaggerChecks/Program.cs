using CafeManagement.API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddLogging();
services.AddControllers().AddApplicationPart(typeof(TableFoodController).Assembly);
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cafe Management API", Version = "v1" }));
using var app = builder.Build();
var provider = app.Services;
using var scope = provider.CreateScope();
var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
if (!swagger.Paths.ContainsKey("/api/TableFood/transfer") || !swagger.Paths.ContainsKey("/api/TableFood"))
    throw new Exception("Missing table endpoints.");
if (swagger.Paths.Keys.Any(path => path.Contains("Validate", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Validation helper exposed as endpoint.");
if (!swagger.Paths.ContainsKey("/api/Customer/manage") || !swagger.Paths.ContainsKey("/api/Customer/import") || !swagger.Paths.ContainsKey("/api/Bill/{billId}/customer")) throw new Exception("Missing customer endpoints.");
Console.WriteLine($"PASS: Swagger v1 generated with {swagger.Paths.Count} paths; table endpoints present and validation helper excluded.");
