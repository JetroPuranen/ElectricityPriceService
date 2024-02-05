using ElectricityPriceService;
using Newtonsoft.Json;

public class DataOperations
{
    public static void DeleteRecord(MyDbContext dbContext, int id)
    {
        var price = dbContext.SpotPrices.FirstOrDefault(p => p.Id == id);

        if (price != null)
        {
            dbContext.SpotPrices.Remove(price);
            dbContext.SaveChanges();

            Console.WriteLine($"Data with ID {id} has been deleted from the database.");
        }
        else
        {
            Console.WriteLine($"No data with ID {id} found in the database.");
        }
    }

    public static void UpdateRecord(MyDbContext dbContext, int id, decimal newPrice)
    {
        var price = dbContext.SpotPrices.FirstOrDefault(p => p.Id == id);

        if (price != null)
        {
            price.Price = newPrice;
            dbContext.SaveChanges();

            Console.WriteLine($"Data with ID {id} has been updated in the database.");
        }
        else
        {
            Console.WriteLine($"No data with ID {id} found in the database.");
        }
    }
}
    
