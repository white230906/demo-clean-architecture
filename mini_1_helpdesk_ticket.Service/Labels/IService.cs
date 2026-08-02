namespace mini_1_helpdesk_ticket.Service.Labels;

public interface IService
{
    Task<IReadOnlyList<Response.LabelResponse>> GetLabels(CancellationToken ct);
    Task<Response.LabelResponse> CreateLabel(
        Request.CreateLabelRequest request,
        CancellationToken ct);
    Task<string> DeleteLabels(List<Guid> labelIds, CancellationToken ct);
}