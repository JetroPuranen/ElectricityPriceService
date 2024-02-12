namespace ElectricityPriceService.Controllers;
public class ElectricityPrices(MyDbContext dbContext)
{
    private readonly MyDbContext _dbContext = dbContext;

    public List<SpotPrice> GetElectricityPricesFromRange(DateTime startDate, DateTime endDate, int pageSize, int page)
    {
        try
        {
            var query = _dbContext.SpotPrices
                .Where(price => price.StartDate >= startDate && price.EndDate <= endDate)
                .OrderBy(price => price.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return query;
        }
        catch (Exception ex)
        {
            // Käsittele virheet asianmukaisesti (esim. logita ja palauta null tai tyhjä lista)
            Console.WriteLine($"Virhe: {ex.Message}");
            return new List<SpotPrice>();
        }
    }
}
