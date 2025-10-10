using System;
using System.Globalization;

class TestDecimal
{
    static void Main()
    {
        string binancePrice = "2.82700000";

        if (decimal.TryParse(binancePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        {
            Console.WriteLine($"Parsed price: {price}");
            Console.WriteLine($"Price as string: {price.ToString()}");
            Console.WriteLine($"Price with G29: {price.ToString("G29", CultureInfo.InvariantCulture)}");

            // Test if division helps
            if (price > 1_000_000)
            {
                var corrected = price / 100_000_000m;
                Console.WriteLine($"Corrected price: {corrected}");
            }
            else
            {
                Console.WriteLine($"Price is already correct: {price}");
            }
        }
    }
}