namespace Titan.ChiefOfStaff;

public sealed record SpecialistResult(
    string Specialist,
    string Status,
    string Rooftop,
    IReadOnlyList<string> Items,
    IReadOnlyList<string> Blocks,
    IReadOnlyList<string> Evidence);

public static class SpecialistCore
{
    public const int CatalogSize = 51;

    public static IReadOnlyList<string> SpecialistNames { get; } =
    [
        "queue", "profile", "drafting", "compliance", "appointment", "crm-proof", "audit"
    ];

    public static IReadOnlyList<SpecialistResult> Run(
        string rooftop,
        IReadOnlyList<OrganizerRow> rows,
        DateTimeOffset nowEt,
        IReadOnlyList<GroundedDraftResult> drafts,
        IReadOnlyList<string> crmProof)
    {
        if (!Policy.IsAllowedRooftop(rooftop))
        {
            return SpecialistNames.Select(name => new SpecialistResult(name, "blocked", "none", [], ["rooftop not 28206/28546"], [])).ToArray();
        }

        var queueItems = rows.Select(row => $"{row.Queue}:{row.CustomerKey}").ToArray();
        var holds = rows.Select(row => WorkOrder.HoldReason(row, nowEt)).OfType<string>().Distinct().ToArray();
        var ready = rows.Where(row => WorkOrder.HoldReason(row, nowEt) is null).ToArray();
        var draftBodies = drafts.Where(d => d.Ok && d.Body is not null).Select(d => d.Body!).ToArray();
        var draftBlocks = drafts.Where(d => d.Block is not null).Select(d => d.Block!).ToArray();
        var appointmentBlocks = ready.Select(row => Policy.AppointmentSetBlock(row.HistoryText)).OfType<string>().Distinct().ToArray();
        var auditBlocks = new List<string>();
        if (drafts.Any(d => d.Ok && d.Body is not null && Consent.PromisesForbiddenTerms(d.Body)))
        {
            auditBlocks.Add("forbidden promise in draft");
        }

        return
        [
            new SpecialistResult("queue", "ok", rooftop, queueItems, [], [ $"ranks={rows.Count}" ]),
            new SpecialistResult("profile", ready.Length == 0 ? "blocked" : "ok", rooftop, ready.Select(r => r.CustomerKey).ToArray(), holds, []),
            new SpecialistResult("drafting", draftBodies.Length == 0 ? "blocked" : "ok", rooftop, draftBodies, draftBlocks, []),
            new SpecialistResult("compliance", holds.Length == 0 ? "ok" : "blocked", rooftop, [], holds, [ $"ownershipDays={Policy.OwnershipScreenDays}" ]),
            new SpecialistResult("appointment", appointmentBlocks.Length == 0 && ready.Length > 0 ? "ok" : "blocked", rooftop, [], appointmentBlocks, []),
            new SpecialistResult("crm-proof", crmProof.Count == 0 ? "blocked" : "ok", rooftop, crmProof.ToArray(), crmProof.Count == 0 ? ["CRM history is the only send count"] : [], []),
            new SpecialistResult("audit", auditBlocks.Count == 0 ? "ok" : "blocked", rooftop, [], auditBlocks, [ "fail-closed" ])
        ];
    }
}
