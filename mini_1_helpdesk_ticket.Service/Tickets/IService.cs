using mini_1_helpdesk_ticket.Service.Models;

namespace mini_1_helpdesk_ticket.Service.Tickets;

public interface IService
{

    Task<Response.TicketDetailResponse> CreateTicket(
        Request.CreateTicketRequest request,
        CancellationToken ct);
    
    Task<Response.TicketDetailResponse> GetTicket(Guid id, CancellationToken ct);

    Task<BasePaginationResponse> GetTickets(
        Request.TicketFilter filter,
        CancellationToken ct);

    Task<Response.TicketDetailResponse> UpdateTicket(
        Guid id,
        Request.UpdateTicketRequest request,
        CancellationToken ct);

    Task<Response.CommentResponse> AddCommentTicket(
        Guid ticketId,
        Request.AddCommentRequest request,
        CancellationToken ct);

    Task<Response.TicketLabelsResponse> ReplaceLabelsTicket(
        Guid ticketId,
        Request.ReplaceTicketLabelsRequest request,
        CancellationToken ct);

    Task DeleteTicket(Guid id, CancellationToken ct);
}