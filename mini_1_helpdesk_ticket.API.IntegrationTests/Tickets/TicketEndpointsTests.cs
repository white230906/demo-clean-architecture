using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using mini_1_helpdesk_ticket.API.Models;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Application.Common.Models;
using mini_1_helpdesk_ticket.Domain.Tickets;
using TicketApp = mini_1_helpdesk_ticket.Application.Tickets;
using Xunit;

namespace mini_1_helpdesk_ticket.API.IntegrationTests.Tickets;

public sealed class TicketEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task CreateTicket_WhenPriorityIsOmitted_UsesMediumAndReturns201()
    {
        var createdTicket = NewTicketDetail();
        var service = new StubTicketService
        {
            Ticket = createdTicket
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                Title = "Cannot connect",
                Description = "VPN is unavailable"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            $"/api/tickets/{createdTicket.Id}",
            response.Headers.Location?.AbsolutePath);
        Assert.NotNull(service.ReceivedCreateRequest);
        Assert.Equal(TicketPriority.Medium, service.ReceivedCreateRequest.Priority);
    }

    [Fact]
    public async Task GetTickets_ReturnsPaginationMetadata()
    {
        var service = new StubTicketService
        {
            TicketPage = new PaginationResult<TicketApp.Response.TicketListItemResponse>(
                [NewTicketListItem()],
                totalCount: 3)
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/tickets?pageIndex=2&pageSize=1");

        var body = await response.Content.ReadFromJsonAsync<
            BasePaginationResponse<TicketApp.Response.TicketListItemResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(2, body.Value.PageIndex);
        Assert.Equal(1, body.Value.PageSize);
        Assert.Equal(3, body.Value.TotalCount);
        Assert.Equal(3, body.Value.TotalPages);
        Assert.True(body.Value.HasPreviousPage);
        Assert.True(body.Value.HasNextPage);
    }

    [Fact]
    public async Task GetTicket_WhenApplicationReturnsNotFound_Returns404()
    {
        var service = new StubTicketService
        {
            GetTicketException = new AppException(
                ErrorType.NotFound,
                "TICKET_NOT_FOUND",
                "Không tìm thấy ticket.")
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("TICKET_NOT_FOUND", error.MessageCode);
    }

    [Fact]
    public async Task AddComment_Returns201()
    {
        var ticketId = Guid.NewGuid();
        var service = new StubTicketService
        {
            Comment = new TicketApp.Response.CommentResponse
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AuthorName = "Alice",
                Content = "Please check the VPN gateway.",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/comments",
            new
            {
                AuthorName = "Alice",
                Content = "Please check the VPN gateway."
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(ticketId, service.ReceivedCommentTicketId);
        Assert.Equal("Alice", service.ReceivedCommentRequest?.AuthorName);
    }

    [Fact]
    public async Task UpdateTicket_PassesRouteAndBodyToApplication()
    {
        var ticketId = Guid.NewGuid();
        var service = new StubTicketService();

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new
            {
                Title = "VPN issue updated",
                Description = "New description",
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress,
                AssigneeName = "Alice",
                RowVersion = 7
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ticketId, service.ReceivedUpdateTicketId);
        Assert.Equal("VPN issue updated", service.ReceivedUpdateRequest?.Title);
        Assert.Equal((uint)7, service.ReceivedUpdateRequest?.RowVersion);
    }

    [Fact]
    public async Task ReplaceLabels_PassesLabelIdsToApplication()
    {
        var ticketId = Guid.NewGuid();
        var labelIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var service = new StubTicketService();

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}/labels",
            new { LabelIds = labelIds });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ticketId, service.ReceivedReplaceLabelsTicketId);
        Assert.Equal(labelIds, service.ReceivedReplaceLabelsRequest?.LabelIds);
    }

    [Fact]
    public async Task DeleteTicket_Returns204()
    {
        var ticketId = Guid.NewGuid();
        var service = new StubTicketService();

        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ticketId, service.DeletedTicketId);
    }

    private static TestWebApplicationFactory CreateFactory(
        TicketApp.IService service)
    {
        return new TestWebApplicationFactory(services =>
        {
            services.RemoveAll<TicketApp.IService>();
            services.AddSingleton<TicketApp.IService>(service);
        });
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static TicketApp.Response.TicketDetailResponse NewTicketDetail()
    {
        return new TicketApp.Response.TicketDetailResponse
        {
            Id = Guid.NewGuid(),
            Code = "TCK-0001",
            Title = "Cannot connect",
            Description = "VPN is unavailable",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            RowVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static TicketApp.Response.TicketListItemResponse NewTicketListItem()
    {
        return new TicketApp.Response.TicketListItemResponse
        {
            Id = Guid.NewGuid(),
            Code = "TCK-0001",
            Title = "Cannot connect",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            RowVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class StubTicketService : TicketApp.IService
    {
        public TicketApp.Response.TicketDetailResponse Ticket { get; init; } =
            NewTicketDetail();

        public PaginationResult<TicketApp.Response.TicketListItemResponse> TicketPage
            { get; init; } = new([], 0);

        public TicketApp.Response.CommentResponse Comment { get; init; } = new()
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            AuthorName = "Author",
            Content = "Content",
            CreatedAt = DateTimeOffset.UtcNow
        };

        public AppException? GetTicketException { get; init; }
        public TicketApp.Request.CreateTicketRequest? ReceivedCreateRequest { get; private set; }
        public Guid? ReceivedUpdateTicketId { get; private set; }
        public TicketApp.Request.UpdateTicketRequest? ReceivedUpdateRequest { get; private set; }
        public Guid? ReceivedCommentTicketId { get; private set; }
        public TicketApp.Request.AddCommentRequest? ReceivedCommentRequest { get; private set; }
        public Guid? ReceivedReplaceLabelsTicketId { get; private set; }
        public TicketApp.Request.ReplaceTicketLabelsRequest? ReceivedReplaceLabelsRequest
            { get; private set; }
        public Guid? DeletedTicketId { get; private set; }

        public Task<TicketApp.Response.TicketDetailResponse> CreateTicketAsync(
            TicketApp.Request.CreateTicketRequest request,
            CancellationToken ct = default)
        {
            ReceivedCreateRequest = request;
            return Task.FromResult(Ticket);
        }

        public Task<TicketApp.Response.TicketDetailResponse> GetTicketAsync(
            Guid id,
            CancellationToken ct = default)
        {
            if (GetTicketException is not null)
            {
                throw GetTicketException;
            }

            return Task.FromResult(Ticket);
        }

        public Task<PaginationResult<TicketApp.Response.TicketListItemResponse>> GetTicketsAsync(
            TicketApp.Request.TicketFilter filter,
            CancellationToken ct = default)
        {
            return Task.FromResult(TicketPage);
        }

        public Task<TicketApp.Response.TicketDetailResponse> UpdateTicketAsync(
            Guid id,
            TicketApp.Request.UpdateTicketRequest request,
            CancellationToken ct = default)
        {
            ReceivedUpdateTicketId = id;
            ReceivedUpdateRequest = request;
            return Task.FromResult(Ticket);
        }

        public Task<TicketApp.Response.CommentResponse> AddCommentAsync(
            Guid ticketId,
            TicketApp.Request.AddCommentRequest request,
            CancellationToken ct = default)
        {
            ReceivedCommentTicketId = ticketId;
            ReceivedCommentRequest = request;
            return Task.FromResult(Comment);
        }

        public Task<TicketApp.Response.TicketLabelsResponse> ReplaceLabelsAsync(
            Guid ticketId,
            TicketApp.Request.ReplaceTicketLabelsRequest request,
            CancellationToken ct = default)
        {
            ReceivedReplaceLabelsTicketId = ticketId;
            ReceivedReplaceLabelsRequest = request;
            return Task.FromResult(new TicketApp.Response.TicketLabelsResponse
            {
                TicketId = ticketId
            });
        }

        public Task DeleteTicketAsync(
            Guid id,
            CancellationToken ct = default)
        {
            DeletedTicketId = id;
            return Task.CompletedTask;
        }
    }
}
