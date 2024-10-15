// See https://aka.ms/new-console-template for more information
using System.Net.Http.Headers;

Console.WriteLine("Hello, World!");

using var client = new HttpClient();
using var request = new HttpRequestMessage()
{
    RequestUri = new Uri("https://api.weather.gov/gridpoints/SEW/124,69/forecast")
};
request.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse("Weather.Worker/0.0.0"));

var response = await client.SendAsync(request);
var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

Console.WriteLine(responseText);