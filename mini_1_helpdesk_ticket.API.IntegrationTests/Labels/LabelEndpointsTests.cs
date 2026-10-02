using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using mini_1_helpdesk_ticket.API.Models;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using LabelApp = mini_1_helpdesk_ticket.Application.Labels;
using Xunit;

namespace mini_1_helpdesk_ticket.API.IntegrationTests.Labels;

public sealed class LabelEndpointsTests
{
    [Fact]
    public async Task GetLabels_ReturnsLabelsFromApplication()
    {
        var expected = NewLabelResponse("Network", "network", "#112233");
        var service = new StubLabelService
        {
            Labels = [expected]
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/labels");
        var body = await response.Content
            .ReadFromJsonAsync<BaseResponse<List<LabelApp.Response.LabelResponse>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.IsSuccess);

        var label = Assert.Single(body.Value!);
        Assert.Equal(expected.Id, label.Id);
        Assert.Equal("network", label.Slug);
    }

    [Fact]//test name wrong, have to trim before save
    public async Task CreateLabel_Returns201_AndPassesBodyToApplication()
    {
        var service = new StubLabelService
        {
            CreatedLabel = NewLabelResponse(
                "Mạng nội bộ",
                "mang-noi-bo",
                "#A1B2C3")
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/labels",
            new
            {
                Name = "  Mạng nội bộ  ",
                Color = "#A1B2C3"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.ReceivedCreateRequest);
        Assert.Equal("  Mạng nội bộ  ", service.ReceivedCreateRequest.Name);
        Assert.Equal("#A1B2C3", service.ReceivedCreateRequest.Color);
    }

    [Fact]
    public async Task CreateLabel_WhenApplicationRejects_Returns400()
    {
        var service = new StubLabelService
        {
            CreateException = new AppException(
                ErrorType.Validation,
                "LABEL_COLOR_INVALID",
                "Color không hợp lệ.")
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/labels",
            new { Name = "Network", Color = "red" });

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("LABEL_COLOR_INVALID", error.MessageCode);
    }

    [Fact]
    public async Task DeleteLabels_ReturnsDeletedCount()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var service = new StubLabelService
        {
            DeletedCount = ids.Length
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/labels")
        {
            Content = JsonContent.Create(ids)
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ids, service.ReceivedDeleteIds?.ToArray());
    }

    private static TestWebApplicationFactory CreateFactory(
        LabelApp.IService service)
    {
        return new TestWebApplicationFactory(services =>
        {
            services.RemoveAll<LabelApp.IService>();
            services.AddSingleton<LabelApp.IService>(service);
        });
    }

    private static LabelApp.Response.LabelResponse NewLabelResponse(
        string name,
        string slug,
        string color)
    {
        return new LabelApp.Response.LabelResponse
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Color = color,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class StubLabelService : LabelApp.IService
    {
        public IReadOnlyList<LabelApp.Response.LabelResponse> Labels { get; init; } = [];
        public LabelApp.Response.LabelResponse CreatedLabel { get; init; } =
            NewLabelResponse("Label", "label", "#112233");
        public AppException? CreateException { get; init; }
        public int DeletedCount { get; init; }

        public LabelApp.Request.CreateLabelRequest? ReceivedCreateRequest { get; private set; }
        public IReadOnlyCollection<Guid>? ReceivedDeleteIds { get; private set; }

        public Task<IReadOnlyList<LabelApp.Response.LabelResponse>> GetLabelsAsync(
            CancellationToken ct = default)
        {
            return Task.FromResult(Labels);
        }

        public Task<LabelApp.Response.LabelResponse> CreateLabelAsync(
            LabelApp.Request.CreateLabelRequest request,
            CancellationToken ct = default)
        {
            ReceivedCreateRequest = request;

            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(CreatedLabel);
        }

        public Task<int> DeleteLabelsAsync(
            IReadOnlyCollection<Guid> labelIds,
            CancellationToken ct = default)
        {
            ReceivedDeleteIds = labelIds;
            return Task.FromResult(DeletedCount);
        }
    }
}
