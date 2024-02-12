using ElectricityPriceService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
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
        services.AddScoped<ElectricityPrices>(); // Lis�tty ElectricityPrices palveluihin
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapGet("/api/electricityprices", async context =>
            {
                // Hae tarvittavat parametrit pyynn�st�
                var startDateString = context.Request.Query["startDate"];
                var endDateString = context.Request.Query["endDate"];
                var pageSizeString = context.Request.Query["pageSize"];
                var pageString = context.Request.Query["page"];

                //Tarkistetaan parametrien oikeellisuus
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
                    // Palauta virhe, jos parametrit eiv�t ole oikein
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid parameters.");
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
            //Here we can delete or update data
            //DataOperations.DeleteRecord(dbContext, 1042);
            //DataOperations.UpdateRecord(dbContext, 1050, 15.501M);
        }
    }
}
