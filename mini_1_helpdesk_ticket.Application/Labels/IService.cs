namespace mini_1_helpdesk_ticket.Application.Labels;

public interface IService
{
    Task<IReadOnlyList<Response.LabelResponse>> GetLabelsAsync(
        CancellationToken ct = default);

    Task<Response.LabelResponse> CreateLabelAsync(
        Request.CreateLabelRequest request,
        CancellationToken ct = default);

    Task<int> DeleteLabelsAsync(
        IReadOnlyCollection<Guid> labelIds,
        CancellationToken ct = default);

}