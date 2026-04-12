// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IntegrationEventJsonSerializer.cs
//  Project         : BookFast.Integration.Contracts
// ******************************************************************************

using System.Text.Json;

namespace BookFast.Integration.Contracts;

public static class IntegrationEventJsonSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static T Deserialize<T>(string payloadJson)
    {
        T? value = JsonSerializer.Deserialize<T>(payloadJson, SerializerOptions);
        if (value is null)
        {
            throw new InvalidOperationException($"Failed to deserialize integration event payload to {typeof(T).Name}.");
        }

        return value;
    }
}
