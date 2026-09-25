namespace Titan.ChiefOfStaff;

public enum WorkQueue
{
    NewLeads,
    Planner,
    Overdue,
    Database
}

public sealed record OrganizerRow(
    string CustomerKey,
    string AssignedTo,
    WorkQueue Queue,
    DateTimeOffset? LastContact,
    DateTimeOffset? OpenedAt,
    bool TitanTouched,
    string HistoryText);

public static class WorkOrder
{
    public static IReadOnlyList<WorkQueue> MandatoryOrder { get; } =
    [
        WorkQueue.NewLeads,
        WorkQueue.Planner,
        WorkQueue.Overdue,
        WorkQueue.Database
    ];

    public static bool IsGlennAssigned(string assignedTo)
        => assignedTo.Contains("Bordine", StringComparison.OrdinalIgnoreCase)
           || assignedTo.Contains("Glenn", StringComparison.OrdinalIgnoreCase);

    public static string? HoldReason(OrganizerRow row, DateTimeOffset nowEt)
    {
        var consent = Consent.BlockReason(row.HistoryText);
        if (consent is not null)
        {
            return consent;
        }

        if (IsGlennAssigned(row.AssignedTo))
        {
            return null;
        }

        if (Policy.OtherConsultantHeldByRecentContact(nowEt, row.LastContact))
        {
            return "72h hold — another consultant already has qualifying contact.";
        }

        if (!Policy.OtherConsultantEligible(nowEt, row.LastContact, row.TitanTouched, row.OpenedAt))
        {
            return "96h silence not proven for another consultant / unassigned lead.";
        }

        return null;
    }
}
