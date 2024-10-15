using AzLogs.Ingestion.WeatherServiceTransport;

using var httpClient = new HttpClient();
var weatherClient = new WeatherClient(httpClient);

var forecasts = await weatherClient.Gridpoint_ForecastAsync(NWSForecastOfficeId.SEW,124,69);

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(forecasts));