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
    string HistoryText,
    DateTimeOffset? PurchasedAt = null,
    bool RecentInquiry = false,
    int? ModelYear = null);

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
    {
        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            return false;
        }

        // Organizer rows are the whole visible line. "Glenn" in the customer
        // name, or "Bordine" in the store name, is not the assignee. When the
        // line names who it is assigned to, only that clause can pass the hold.
        const string marker = "assigned to ";
        var at = assignedTo.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var subject = at >= 0 ? assignedTo[(at + marker.Length)..] : assignedTo;
        return subject.Contains("Bordine", StringComparison.OrdinalIgnoreCase)
            || subject.Contains("Glenn", StringComparison.OrdinalIgnoreCase);
    }

    public static string? HoldReason(OrganizerRow row, DateTimeOffset nowEt)
    {
        var consent = Consent.BlockReason(row.HistoryText);
        if (consent is not null)
        {
            return consent;
        }

        var ownership = Policy.OwnershipBlock(nowEt, row.PurchasedAt, row.RecentInquiry, row.ModelYear);
        if (ownership is not null)
        {
            return ownership;
        }

        if (row.Queue == WorkQueue.Database && row.PurchasedAt is null && !row.RecentInquiry)
        {
            return "Ownership duration unproven for a database row.";
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
