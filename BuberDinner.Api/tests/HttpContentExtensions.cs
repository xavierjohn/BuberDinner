namespace BuberDinner.Api.Tests;
using System.Text.Json;

internal static class HttpContentExtensions
{
    private static readonly JsonSerializerOptions defaultOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T?> ReadAsAsync<T>(this HttpContent content, JsonSerializerOptions? options = null)
    {
        using (Stream contentStream = await content.ReadAsStreamAsync())
        {
            return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? defaultOptions);
        }
    }


    internal static Task<T?> ReadAsExample<T>(this HttpContent content, T example) => content.ReadAsAsync<T>();
}
