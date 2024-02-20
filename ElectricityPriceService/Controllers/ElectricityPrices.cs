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
            
            Console.WriteLine($"Error: {ex.Message}");
            return new List<SpotPrice>();
        }
    }
    public decimal GetPriceDifferenceFromRange(DateTime startDate, DateTime endDate, decimal fixedPrice)
    {
        try
        {
            var pricesInRange = _dbContext.SpotPrices
                .Where(price => price.StartDate >= startDate && price.EndDate <= endDate)
                .ToList();

            decimal totalSpotPrice = pricesInRange.Sum(price => price.Price);
            decimal priceDifference = totalSpotPrice - fixedPrice;

            return priceDifference;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 0; // or handle error accordingly
        }
    }
    public List<HourlyPriceDifference> GetHourlyPriceDifferencesFromRange(params DateTime[] datePairs)
    {
        try
        {
            var hourlyPriceDifferences = new List<HourlyPriceDifference>();

            foreach (var datePair in datePairs)
            {
                var startDate = datePair;
                var endDate = datePair.AddHours(1); 

                var priceDifference = _dbContext.SpotPrices
                    .Where(price => price.StartDate >= startDate && price.EndDate <= endDate)
                    .Sum(price => price.Price);


                hourlyPriceDifferences.Add(new HourlyPriceDifference
                {
                    Hour = startDate.Hour,
                    PriceDifference = priceDifference
                });
            }

            return hourlyPriceDifferences;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<HourlyPriceDifference>();
        }
    }
}
public class HourlyPriceDifference
{
    public int Hour { get; set; }
    public decimal PriceDifference { get; set; }
}
