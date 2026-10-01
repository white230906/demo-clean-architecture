using Microsoft.AspNetCore.Mvc;
using mini_1_helpdesk_ticket.Service.Models;
using mini_1_helpdesk_ticket.Service.Tickets;

namespace mini_1_helpdesk_ticket.API.Controller;

[ApiController]
[Route("api/tickets")]
public class TicketsController: ControllerBase
{
    private readonly IService  _ticketService;
    
    public TicketsController(IService ticketService) =>
        _ticketService = ticketService;

    [HttpPost]
    public async Task<IActionResult> CreateTicket(Request.CreateTicketRequest request, CancellationToken ct)
    {
        var result = await _ticketService.CreateTicket(request, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetListTickets([FromQuery] Request.TicketFilter filter, CancellationToken ct)
    {
        var result = await _ticketService.GetTickets(filter, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var result = await _ticketService.GetTicket(id, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTicket(
        Guid id,
        Request.UpdateTicketRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.UpdateTicket(id, request, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        Request.AddCommentRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.AddCommentTicket(id, request, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/labels")]
    public async Task<IActionResult> ReplaceLabels(
        Guid id,
        Request.ReplaceTicketLabelsRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.ReplaceLabelsTicket(id, request, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken ct)
    {
        await  _ticketService.DeleteTicket(id, ct);
        return NoContent();
    }
}
