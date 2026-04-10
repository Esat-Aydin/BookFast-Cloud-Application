// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : GraphQlEndpointTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BookFast.API.Tests;

public sealed class GraphQlEndpointTests
{
    private static readonly Guid AmsterdamBoardRoomId = Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901");

    [Fact]
    public async Task PostRoomsQuery_ShouldApplyFilteringAndPaging()
    {
        await using BookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            """
            query {
              rooms(search: "Amsterdam", minimumCapacity: 10, first: 1) {
                code
                name
                capacity
              }
            }
            """);

        AssertNoGraphQlErrors(document);

        JsonElement rooms = document.RootElement
            .GetProperty("data")
            .GetProperty("rooms");

        Assert.Equal(1, rooms.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", rooms[0].GetProperty("code").GetString());
        Assert.Equal(12, rooms[0].GetProperty("capacity").GetInt32());
    }

    [Fact]
    public async Task PostRoomsQuery_ShouldReturnGraphQlError_WhenPageSizeIsOutsideAllowedRange()
    {
        await using BookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            """
            query {
              rooms(first: 0) {
                code
              }
            }
            """);

        JsonElement errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.GetArrayLength() > 0);
        Assert.Equal(
            "PAGING_ARGUMENT_OUT_OF_RANGE",
            errors[0].GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostReservationsQuery_ShouldReturnCreatedReservation()
    {
        await using BookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(2);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
              reservations(roomId: "{{AmsterdamBoardRoomId}}", reservedByContains: "GraphQL Recruiter", first: 5) {
                roomCode
                reservedBy
                purpose
              }
            }
            """);

        AssertNoGraphQlErrors(document);

        JsonElement reservations = document.RootElement
            .GetProperty("data")
            .GetProperty("reservations");

        Assert.Equal(1, reservations.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", reservations[0].GetProperty("roomCode").GetString());
        Assert.Equal("GraphQL Recruiter Demo", reservations[0].GetProperty("reservedBy").GetString());
    }

    [Fact]
    public async Task PostRoomAvailabilityQuery_ShouldReturnConflict_WhenReservationExists()
    {
        await using BookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(3);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
              roomAvailability(roomId: "{{AmsterdamBoardRoomId}}", fromUtc: "{{startUtc:O}}", toUtc: "{{endUtc:O}}") {
                isAvailable
                conflicts {
                  reservedBy
                }
              }
            }
            """);

        AssertNoGraphQlErrors(document);

        JsonElement availability = document.RootElement
            .GetProperty("data")
            .GetProperty("roomAvailability");

        Assert.False(availability.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(1, availability.GetProperty("conflicts").GetArrayLength());
        Assert.Equal(
            "GraphQL Recruiter Demo",
            availability.GetProperty("conflicts")[0].GetProperty("reservedBy").GetString());
    }

    private static async Task CreateReservationAsync(
        HttpClient client,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/reservations",
            new
            {
                roomId = AmsterdamBoardRoomId,
                reservedBy = "GraphQL Recruiter Demo",
                purpose = "GraphQL reservation read model",
                startUtc,
                endUtc
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<JsonDocument> ExecuteGraphQlAsync(HttpClient client, string query)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/graphql", new { query });
        string payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return JsonDocument.Parse(payload);
    }

    private static void AssertNoGraphQlErrors(JsonDocument document)
    {
        Assert.False(
            document.RootElement.TryGetProperty("errors", out _),
            document.RootElement.GetRawText());
    }

    private sealed class BookFastApiFactory : WebApplicationFactory<Program>
    {
    }
}
