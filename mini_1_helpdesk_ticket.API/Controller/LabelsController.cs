using Microsoft.AspNetCore.Mvc;
using mini_1_helpdesk_ticket.API.Models;
using Labels = mini_1_helpdesk_ticket.Application.Labels;

namespace mini_1_helpdesk_ticket.API.Controller;

[ApiController]
[Route("api/labels")]
public class LabelsController : ControllerBase
{
    private readonly Labels.IService _labelService;

    public LabelsController(Labels.IService labelService)
    {
        _labelService = labelService;
    }

    [HttpGet]
    public async Task<IActionResult> ListLabels(CancellationToken ct)
    {
        var result = await _labelService.GetLabelsAsync(ct);

        return Ok(ApiResponseFactory.Base(
            result,
            HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLabel(
        [FromBody] Labels.Request.CreateLabelRequest request,
        CancellationToken ct)
    {
        var result = await _labelService.CreateLabelAsync(request, ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFactory.Base(
                result,
                HttpContext.TraceIdentifier));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteLabels(
        [FromBody] IReadOnlyCollection<Guid> labelIds,
        CancellationToken ct)
    {
        var deletedCount = await _labelService.DeleteLabelsAsync(labelIds, ct);

        return Ok(ApiResponseFactory.Base(
            new { DeletedCount = deletedCount },
            HttpContext.TraceIdentifier));
    }
}
