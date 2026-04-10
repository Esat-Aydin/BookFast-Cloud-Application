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
            rooms(sortBy: CAPACITY_DESCENDING, first: 2) {
                code
                capacity
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement rooms = document.RootElement
            .GetProperty("data")
            .GetProperty("rooms");

        Assert.Equal(2, rooms.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", rooms[0].GetProperty("code").GetString());
        Assert.Equal(12, rooms[0].GetProperty("capacity").GetInt32());
        Assert.Equal("UTR-COLLAB-02", rooms[1].GetProperty("code").GetString());
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
    public async Task PostReservationsQuery_ShouldFilterByLocationAndSortDescending()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset utrechtStartUtc = DateTimeOffset.UtcNow.AddHours(1);
        DateTimeOffset firstAmsterdamStartUtc = DateTimeOffset.UtcNow.AddHours(2);
        DateTimeOffset secondAmsterdamStartUtc = DateTimeOffset.UtcNow.AddHours(5);

        await CreateReservationAsync(
            client,
            UtrechtCollaborationHubId,
            "GraphQL Utrecht Consumer",
            utrechtStartUtc,
            utrechtStartUtc.AddHours(1));
        await CreateReservationAsync(
            client,
            AmsterdamBoardRoomId,
            "GraphQL Amsterdam First",
            firstAmsterdamStartUtc,
            firstAmsterdamStartUtc.AddHours(1));
        await CreateReservationAsync(
            client,
            AmsterdamBoardRoomId,
            "GraphQL Amsterdam Second",
            secondAmsterdamStartUtc,
            secondAmsterdamStartUtc.AddHours(1));

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            """
            query {
            reservations(
              location: "Amsterdam HQ - Floor 5"

              sortBy: START_UTC_DESCENDING

              first: 5
            ) {
                roomCode
                reservedBy
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement reservations = document.RootElement
            .GetProperty("data")
            .GetProperty("reservations");

        Assert.Equal(2, reservations.GetArrayLength());
        Assert.Equal("GraphQL Amsterdam Second", reservations[0].GetProperty("reservedBy").GetString());
        Assert.Equal("GraphQL Amsterdam First", reservations[1].GetProperty("reservedBy").GetString());
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
    public async Task PostRoomAvailabilityOverviewQuery_ShouldReturnAvailabilityAcrossRooms()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(3);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, AmsterdamBoardRoomId, "GraphQL Availability Demo", startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
            roomAvailabilityOverview(
              fromUtc: "{{startUtc:O}}"

              toUtc: "{{endUtc:O}}"

              sortBy: CAPACITY_DESCENDING

              first: 3
            ) {
                roomCode
                location
                capacity
                isAvailable
                conflictCount
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement overview = document.RootElement
            .GetProperty("data")
            .GetProperty("roomAvailabilityOverview");

        Assert.Equal(3, overview.GetArrayLength());
        Assert.Equal("AMS-BOARD-01", overview[0].GetProperty("roomCode").GetString());
        Assert.False(overview[0].GetProperty("isAvailable").GetBoolean());
        Assert.Equal(1, overview[0].GetProperty("conflictCount").GetInt32());
        Assert.True(overview[1].GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task PostOccupancyOverviewQuery_ShouldReturnLocationSummaries()
    {
        await using SqliteBookFastApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddHours(4);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await CreateReservationAsync(client, AmsterdamBoardRoomId, "GraphQL Occupancy Demo", startUtc, endUtc);

        using JsonDocument document = await ExecuteGraphQlAsync(
            client,
            $$"""
            query {
            occupancyOverview(
              fromUtc: "{{startUtc:O}}"

              toUtc: "{{endUtc:O}}"

              sortBy: LOCATION_ASCENDING

              first: 3
            ) {
                location
                totalRooms
                reservedRooms
                availableRooms
                totalCapacity
                reservedCapacity
                activeReservations
                roomOccupancyRate
                capacityOccupancyRate
              }
        }
        """);

        AssertNoGraphQlErrors(document);

        JsonElement summaries = document.RootElement
            .GetProperty("data")
            .GetProperty("occupancyOverview");

        Assert.Equal(3, summaries.GetArrayLength());
        Assert.Equal("Amsterdam HQ - Floor 5", summaries[0].GetProperty("location").GetString());
        Assert.Equal(1, summaries[0].GetProperty("totalRooms").GetInt32());
        Assert.Equal(1, summaries[0].GetProperty("reservedRooms").GetInt32());
        Assert.Equal(0, summaries[0].GetProperty("availableRooms").GetInt32());
        Assert.Equal(12, summaries[0].GetProperty("reservedCapacity").GetInt32());
        Assert.Equal(1, summaries[0].GetProperty("activeReservations").GetInt32());
        Assert.Equal(1, summaries[0].GetProperty("roomOccupancyRate").GetDouble());
        Assert.Equal(1, summaries[0].GetProperty("capacityOccupancyRate").GetDouble());
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
