using Microsoft.AspNetCore.Mvc;
using mini_1_helpdesk_ticket.API.Models;
using Tickets = mini_1_helpdesk_ticket.Application.Tickets;

namespace mini_1_helpdesk_ticket.API.Controller;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly Tickets.IService _ticketService;

    public TicketsController(Tickets.IService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(
        [FromBody] Tickets.Request.CreateTicketRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.CreateTicketAsync(request, ct);

        return CreatedAtAction(
            nameof(GetTicket),
            new { id = result.Id },
            ApiResponseFactory.Base(
                result,
                HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] Tickets.Request.TicketFilter filter,
        CancellationToken ct)
    {
        var result = await _ticketService.GetTicketsAsync(filter, ct);

        return Ok(ApiResponseFactory.BasePagination(
            result.Items,
            filter.PageIndex,
            filter.PageSize,
            result.TotalCount,
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(
        Guid id,
        CancellationToken ct)
    {
        var result = await _ticketService.GetTicketAsync(id, ct);

        return Ok(ApiResponseFactory.Base(
            result,
            HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTicket(
        Guid id,
        [FromBody] Tickets.Request.UpdateTicketRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.UpdateTicketAsync(id, request, ct);

        return Ok(ApiResponseFactory.Base(
            result,
            HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] Tickets.Request.AddCommentRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.AddCommentAsync(id, request, ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFactory.Base(
                result,
                HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/labels")]
    public async Task<IActionResult> ReplaceLabels(
        Guid id,
        [FromBody] Tickets.Request.ReplaceTicketLabelsRequest request,
        CancellationToken ct)
    {
        var result = await _ticketService.ReplaceLabelsAsync(id, request, ct);

        return Ok(ApiResponseFactory.Base(
            result,
            HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTicket(
        Guid id,
        CancellationToken ct)
    {
        await _ticketService.DeleteTicketAsync(id, ct);
        return NoContent();
    }
}
