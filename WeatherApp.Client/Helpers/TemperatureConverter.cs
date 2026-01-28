namespace WeatherApp.Client.Helpers
{
    public static class TemperatureConverter
    {
        public static double Convert(double celsius, string unit)
        {
            return unit?.ToLower() switch
            {
                "fahrenheit" => (celsius * 9 / 5) + 32,
                "kelvin" => celsius + 273.15,
                _ => celsius // Celsius is default
            };
        }

        public static string GetUnitSymbol(string unit)
        {
            return unit?.ToLower() switch
            {
                "fahrenheit" => "°F",
                "kelvin" => "K",
                _ => "°C"
            };
        }
    }
}
