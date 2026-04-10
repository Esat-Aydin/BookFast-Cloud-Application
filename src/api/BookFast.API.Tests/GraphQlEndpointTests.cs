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

namespace BookFast.API.Tests;

public sealed class GraphQlEndpointTests
{
    private static readonly Guid AmsterdamBoardRoomId = Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901");
    private static readonly Guid UtrechtCollaborationHubId = Guid.Parse("A8B70B66-676C-4A1D-9EA6-865A0B918A72");

    [Fact]
    public async Task PostRoomsQuery_ShouldApplyFilteringAndPaging()
    {
        await using SqliteBookFastApiFactory factory = new();
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
    public async Task PostRoomsQuery_ShouldApplyExplicitSorting()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            """
            query {
            rooms(sortBy: CAPACITY_DESCENDING, first: 3) {
                code
                capacity
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement rooms = document.RootElement
            .GetProperty("data")
            .GetProperty("rooms");

        Assert.Equal(3, rooms.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", rooms[0].GetProperty("code").GetString());
        Assert.Equal(12, rooms[0].GetProperty("capacity").GetInt32());
    }

    [Fact]
    public async Task PostRoomsQuery_ShouldReturnGraphQlError_WhenPageSizeIsOutsideAllowedRange()
    {
        await using SqliteBookFastApiFactory factory = new();
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
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(2);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, AmsterdamBoardRoomId, "GraphQL Recruiter Demo", startUtc, endUtc);

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
    public async Task PostReservationsQuery_ShouldApplyLocationFilterAndSorting()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset earlierStartUtc = DateTimeOffset.UtcNow.AddHours(2);
        DateTimeOffset laterStartUtc = earlierStartUtc.AddHours(3);

        await CreateReservationAsync(client, earlierStartUtc, earlierStartUtc.AddHours(1));
        await CreateReservationAsync(client, laterStartUtc, laterStartUtc.AddHours(1));

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
            reservations(location: "Amsterdam HQ - Floor 5", sortBy: START_UTC_DESCENDING, reservedByContains: "GraphQL Recruiter", first: 5) {
                startUtc
                roomCode
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement reservations = document.RootElement
            .GetProperty("data")
            .GetProperty("reservations");

        Assert.Equal(2, reservations.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", reservations[0].GetProperty("roomCode").GetString());
        Assert.True(
            reservations[0].GetProperty("startUtc").GetDateTimeOffset() >
            reservations[1].GetProperty("startUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task PostRoomAvailabilityQuery_ShouldReturnConflict_WhenReservationExists()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(3);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, AmsterdamBoardRoomId, "GraphQL Recruiter Demo", startUtc, endUtc);

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

    [Fact]
    public async Task PostRoomAvailabilityOverviewQuery_ShouldSummarizeConflictsPerRoom()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(4);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
            roomAvailabilityOverview(fromUtc: "{{startUtc:O}}", toUtc: "{{endUtc:O}}", location: "Amsterdam HQ - Floor 5", first: 5) {
                roomCode
                isAvailable
                conflictCount
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement overview = document.RootElement
            .GetProperty("data")
            .GetProperty("roomAvailabilityOverview");

        Assert.Equal(1, overview.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", overview[0].GetProperty("roomCode").GetString());
        Assert.False(overview[0].GetProperty("isAvailable").GetBoolean());
        Assert.Equal(1, overview[0].GetProperty("conflictCount").GetInt32());
    }

    [Fact]
    public async Task PostOccupancyOverviewQuery_ShouldAggregateByLocation()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(5);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
            occupancyOverview(fromUtc: "{{startUtc:O}}", toUtc: "{{endUtc:O}}", location: "Amsterdam HQ - Floor 5", first: 5) {
                location
                totalRooms
                reservedRooms
                availableRooms
                roomOccupancyRate
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement overview = document.RootElement
            .GetProperty("data")
            .GetProperty("occupancyOverview");

        Assert.Equal(1, overview.GetArrayLength());
        Assert.Equal("Amsterdam HQ - Floor 5", overview[0].GetProperty("location").GetString());
        Assert.Equal(1, overview[0].GetProperty("totalRooms").GetInt32());
        Assert.Equal(1, overview[0].GetProperty("reservedRooms").GetInt32());
        Assert.Equal(0, overview[0].GetProperty("availableRooms").GetInt32());
        Assert.Equal(1d, overview[0].GetProperty("roomOccupancyRate").GetDouble());
    }

    private static Task CreateReservationAsync(
        HttpClient client,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        return CreateReservationAsync(client, AmsterdamBoardRoomId, "GraphQL Recruiter Demo", startUtc, endUtc);
    }

    private static async Task CreateReservationAsync(
        HttpClient client,
        Guid roomId,
        string reservedBy,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/reservations",
            new
            {
                roomId,
                reservedBy,
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
}
