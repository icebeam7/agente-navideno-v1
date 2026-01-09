using System.ComponentModel;
using System.Text.Json;

namespace AgenteApp.Tools;

public static class NavidadTools
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    private const string WeatherApiKey = "a041e55adf330de58f412e24607b06ef"; 
    
    private static JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    
    private static async Task<WeatherResponse?> GetWeather(string city)
    {
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={WeatherApiKey}&units=metric";
        var response = await httpClient.GetStringAsync(url);
        var weather = JsonSerializer.Deserialize<WeatherResponse>(response, options);

        return weather;
    }

    [Description("Obtener información meteorológica actual para una ciudad específica")]
    public static async Task<string> GetWeatherAsync(
        [Description("Nombre de la ciudad (por ejemplo, 'Lima', 'Londres')")] string city)
    {
        try
        {
            var weather = await GetWeather(city);

            if (weather?.Main != null && weather?.Weather?.Length > 0)
            {
                return $"{weather.Name}: {weather.Main.Temp}°C, {weather.Weather[0].Description}. " +
                       $"Sensación térmica: {weather.Main.Feels_Like}°C, Humedad: {weather.Main.Humidity}%";
            }
            
            return $"No se pudo obtener información del clima para {city}";
        }
        catch (Exception ex)
        {
            return $"Error al obtener el clima: {ex.Message}";
        }
    }

    [Description("Sugerir regalos navideños según el presupuesto y las condiciones climáticas actuales")]
    public static async Task<string> SuggestWeatherBasedGiftAsync(
        [Description("Presupuesto en USD")] decimal budget,
        [Description("Ciudad para consultar el clima (por ejemplo, 'Lima', 'Londres')")] string city)
    {
        var temperature = 20.0;
        var description = "despejado";

        try
        {
            var weather = await GetWeather(city);
            
            temperature = weather?.Main?.Temp ?? temperature;
            description = weather?.Weather?[0]?.Main?.ToLower() ?? description;
            
            var suggestion = GetGiftSuggestion(budget, temperature, description);
            
            var weatherInfo = weather?.Name != null 
                ? $"Clima en {weather.Name}: {temperature}°C, {weather?.Weather?[0].Description}\n\n"
                : "Usando clima estimado\n\n";
                
            return $"{weatherInfo}Sugerencia de regalo (${budget}):\n{suggestion}";
        }
        catch (Exception)
        {
            var suggestion = GetGiftSuggestion(budget, temperature, description);
            return $"Sugerencia de regalo (${budget}):\n{suggestion}";
        }
    }

    [Description("Obtener datos curiosos navideños desde un archivo JSON para responder preguntas")]
    public static async Task<string> GetChristmasFactAsync()
    {
        try
        {
            var jsonPath = Path.Combine("assets", "datos.json");
            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var facts = JsonSerializer.Deserialize<ChristmasFactData[]>(jsonContent, options);
            
            if (facts != null && facts.Length > 0)
            {
                var random = new Random();
                var randomFact = facts[random.Next(facts.Length)];
                return $"Dato navideño: {randomFact.Fact}";
            }
            
            return "Dato navideño: ¿Sabías que la tradición del árbol de Navidad comenzó en Alemania en el siglo XVI?";
        }
        catch (Exception)
        {
            return "Dato navideño: ¿Sabías que la tradición del árbol de Navidad comenzó en Alemania en el siglo XVI?";
        }
    }

    [Description("Obtener recetas navideñas aleatorias")]
    public static async Task<string> GetChristmasRecipeAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(
                "https://www.themealdb.com/api/json/v1/1/filter.php?a=British");
            var meals = JsonSerializer.Deserialize<MealResponse>(response, options);

            if (meals?.Meals?.Length > 0)
            {
                var random = new Random();
                var meal = meals.Meals[random.Next(meals.Meals.Length)];
                
                // Obtener detalles de la receta
                var detailResponse = await httpClient.GetStringAsync(
                    $"https://www.themealdb.com/api/json/v1/1/lookup.php?i={meal.IdMeal}");
                var mealDetail = JsonSerializer.Deserialize<MealDetailResponse>(detailResponse, options);
                
                if (mealDetail?.Meals?.Length > 0)
                {
                    var details = mealDetail.Meals[0];
                    var instructions = details.StrInstructions != null 
                        ? details.StrInstructions.Substring(0, Math.Min(200, details.StrInstructions.Length))
                        : "No hay instrucciones disponibles";
                    
                    return $"Receta navideña sugerida: **{details.StrMeal}**\n\n" +
                           $"Instrucciones: {instructions}...\n\n" +
                           $"Imagen: {details.StrMealThumb}";
                }
            }
            return "Receta navideña: ¡Prueba preparar galletas de jengibre caseras con especias navideñas!";
        }
        catch (Exception)
        {
            return "Receta navideña: ¡Prueba preparar galletas de jengibre caseras con especias navideñas!";
        }
    }

    private static string GetGiftSuggestion(decimal budget, double temperature, string weatherCondition)
    {
        var isWinter = temperature < 15;
        var isRainy = weatherCondition.Contains("rain") || weatherCondition.Contains("storm");
        var isSnowy = weatherCondition.Contains("snow");

        return budget switch
        {
            < 20 when isWinter => "Taza térmica navideña + chocolate caliente premium",
            < 20 when isRainy => "Paraguas con temática navideña + calcetines impermeables",
            < 20 => "Vela con aroma navideño + tarjeta personalizada",
            
            < 50 when isSnowy => "Guantes térmicos + bufanda navideña de lana",
            < 50 when isWinter => "Suéter navideño acogedor + gorro de invierno",
            < 50 when isRainy => "Chaqueta impermeable + botas resistentes al agua",
            < 50 => "Libro bestseller + vela aromática de temporada",
            
            < 100 when isWinter => "Manta eléctrica premium + pantuflas de lujo",
            < 100 when isRainy => "Kit de spa en casa + difusor de aceites esenciales",
            < 100 => "Accesorios tecnológicos + cargador inalámbrico",
            
            < 200 when isWinter => "Abrigo de invierno de calidad + accesorios térmicos",
            < 200 => "Reloj deportivo inteligente + audífonos inalámbricos",
            
            _ when isWinter => "Kit completo de invierno: abrigo premium, botas térmicas y accesorios",
            _ => "Experiencia premium: spa, cena gourmet o el último gadget tecnológico"
        };
    }
}

public class WeatherResponse
{
    public string? Name { get; set; }
    public MainWeather? Main { get; set; }
    public Weather[]? Weather { get; set; }
}

public class MainWeather
{
    public double Temp { get; set; }
    public double Feels_Like { get; set; }
    public int Humidity { get; set; }
}

public class Weather
{
    public string? Main { get; set; }
    public string? Description { get; set; }
}

public class ChristmasFactResponse
{
    public string? Fact { get; set; }
}

public class ChristmasFactData
{
    public int ID { get; set; }
    public string? Fact { get; set; }
}

public class MealResponse
{
    public Meal[]? Meals { get; set; }
}

public class MealDetailResponse
{
    public MealDetail[]? Meals { get; set; }
}

public class Meal
{
    public string? IdMeal { get; set; }
    public string? StrMeal { get; set; }
    public string? StrMealThumb { get; set; }
}

public class MealDetail
{
    public string? StrMeal { get; set; }
    public string? StrInstructions { get; set; }
    public string? StrMealThumb { get; set; }
}