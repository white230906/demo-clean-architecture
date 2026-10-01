using Microsoft.AspNetCore.Mvc;
using mini_1_helpdesk_ticket.Service.Labels;
using mini_1_helpdesk_ticket.Service.Models;

namespace mini_1_helpdesk_ticket.API.Controller;

[ApiController]
[Route("api/labels")]
public class LabelsController: ControllerBase
{
    private readonly IService _labelService;
    public LabelsController(IService labelService)
      =>  _labelService = labelService;

    [HttpGet]
    public async Task<IActionResult> ListLabels(CancellationToken ct)
    {
        var result = await _labelService.GetLabels(ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLabel(Request.CreateLabelRequest request, CancellationToken ct)
    {
        var result = await _labelService.CreateLabel(request, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteLabel(List<Guid> labelIds, CancellationToken ct)
    {
        var result = await _labelService.DeleteLabels(labelIds, ct);
        return Ok(ApiResponseFactory.Base(result, traceId: HttpContext.TraceIdentifier));
    }
}
