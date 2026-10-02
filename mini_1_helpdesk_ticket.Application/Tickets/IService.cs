using mini_1_helpdesk_ticket.Application.Common.Models;

namespace mini_1_helpdesk_ticket.Application.Tickets;

public interface IService
{
    Task<Response.TicketDetailResponse> CreateTicketAsync(
        Request.CreateTicketRequest request,
        CancellationToken ct = default);

    Task<Response.TicketDetailResponse> GetTicketAsync(
        Guid id,
        CancellationToken ct = default);

    Task<PaginationResult<Response.TicketListItemResponse>> GetTicketsAsync(
        Request.TicketFilter filter,
        CancellationToken ct = default);

    Task<Response.TicketDetailResponse> UpdateTicketAsync(
        Guid id,
        Request.UpdateTicketRequest request,
        CancellationToken ct = default);

    Task<Response.CommentResponse> AddCommentAsync(
        Guid ticketId,
        Request.AddCommentRequest request,
        CancellationToken ct = default);

    Task<Response.TicketLabelsResponse> ReplaceLabelsAsync(
        Guid ticketId,
        Request.ReplaceTicketLabelsRequest request,
        CancellationToken ct = default);

    Task DeleteTicketAsync(
        Guid id,
        CancellationToken ct = default);
}
