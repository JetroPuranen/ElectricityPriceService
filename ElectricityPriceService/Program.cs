using ElectricityPriceService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading;
using ElectricityPriceService.Controllers;

public class Startup
{
    private static Timer _timer;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<MyDbContext>();
        services.AddControllers();
        services.AddScoped<ElectricityPrices>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapGet("/api/electricityprices", async context =>
            {
                // Hae tarvittavat parametrit pyynnöstä
                var startDateString = context.Request.Query["startDate"];
                var endDateString = context.Request.Query["endDate"];
                var pageSizeString = context.Request.Query["pageSize"];
                var pageString = context.Request.Query["page"];

                // Tarkistetaan parametrien oikeellisuus
                if (DateTime.TryParse(startDateString, out DateTime startDate) &&
                    DateTime.TryParse(endDateString, out DateTime endDate) &&
                    int.TryParse(pageSizeString, out int pageSize) &&
                    int.TryParse(pageString, out int page))
                {
                    var electricityPrices = context.RequestServices.GetRequiredService<ElectricityPrices>();
                    var prices = electricityPrices.GetElectricityPricesFromRange(startDate, endDate, pageSize, page);

                    var responseJson = JsonConvert.SerializeObject(prices);
                    await context.Response.WriteAsync(responseJson);
                }
                else
                {
                    // Palauta virhe, jos parametrit eivät ole oikein
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid parameters.");
                }
            });

            endpoints.MapGet("/api/priceDifference", async context =>
            {
                var startDateString = context.Request.Query["startDate"];
                var endDateString = context.Request.Query["endDate"];
                var fixedPriceString = context.Request.Query["fixedPrice"];

                if (DateTime.TryParse(startDateString, out DateTime startDate) &&
                    DateTime.TryParse(endDateString, out DateTime endDate) &&
                    decimal.TryParse(fixedPriceString, out decimal fixedPrice))
                {
                    var electricityPrices = context.RequestServices.GetRequiredService<ElectricityPrices>();
                    var priceDifference = electricityPrices.GetPriceDifferenceFromRange(startDate, endDate, fixedPrice);

                    var responseJson = JsonConvert.SerializeObject(priceDifference);
                    await context.Response.WriteAsync(responseJson);
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid parameters.");
                }
            });
            endpoints.MapGet("/api/hourlyPriceDifferences", async context =>
            {
                var dateStrings = context.Request.Query["datePairs"];
                var datePairs = dateStrings
                    .Select(dateString => DateTime.TryParse(dateString, out DateTime date) ? date : DateTime.MinValue)
                    .Where(date => date != DateTime.MinValue)
                    .ToArray();

                if (datePairs.Length == 2)
                {
                    var electricityPrices = context.RequestServices.GetRequiredService<ElectricityPrices>();
                    var hourlyPriceDifferences = electricityPrices.GetHourlyPriceDifferencesFromRange(datePairs);

                    if (hourlyPriceDifferences.Count == 2)
                    {
                        
                        var differenceSum = hourlyPriceDifferences[0].PriceDifference - hourlyPriceDifferences[1].PriceDifference;

                        
                        var responseJson = JsonConvert.SerializeObject(new { DifferenceSum = differenceSum });
                        await context.Response.WriteAsync(responseJson);
                    }
                    else
                    {
                        context.Response.StatusCode = 500; // Internal Server Error
                        await context.Response.WriteAsync("Unexpected error in calculating hourly differences.");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid number of date pairs. Provide exactly two date pairs.");
                }
            });
        });

        app.Map("/api/saveprices", app =>
        {
            app.Run(async context =>
            {
                var requestContent = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var prices = JsonConvert.DeserializeObject<List<SpotPrice>>(requestContent);

                Console.WriteLine("Deserialized JSON data:");
                foreach (var price in prices)
                {
                    Console.WriteLine($"ID: {price.Id}, Price: {price.Price}, StartDate: {price.StartDate}, EndDate: {price.EndDate}");
                }

                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

                    foreach (var price in prices)
                    {
                        if (!dbContext.SpotPrices.Any(p => p.Id == price.Id))
                        {
                            dbContext.SpotPrices.Add(new SpotPrice { Price = price.Price, StartDate = price.StartDate, EndDate = price.EndDate });
                        }
                        else
                        {
                            Console.WriteLine($"Data with ID {price.Id} already exists in the database. Skipping insertion.");
                        }
                    }

                    dbContext.SaveChanges();
                }

                Console.WriteLine($"Data saved to DB");
            });
        });


        _timer = new Timer(state => DisplayDatabaseInfo(app), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public static void Main(string[] args)
    {
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();

        host.Run();
    }

    private static void DisplayDatabaseInfo(IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            Console.WriteLine($"All data in database at {DateTime.Now}:");
            foreach (var prices in dbContext.SpotPrices)
            {
                Console.WriteLine($"ID: {prices.Id}, Price: {prices.Price.ToString("F3")}, StartDate: {prices.StartDate}, EndDate: {prices.EndDate}");
            }
        }
    }
}
