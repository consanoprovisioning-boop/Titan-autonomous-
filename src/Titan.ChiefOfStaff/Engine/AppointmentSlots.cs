namespace Titan.ChiefOfStaff;

public static class AppointmentSlots
{
    public static IReadOnlyList<string> NextTwo(DateTimeOffset nowEt, bool customerRequestedTuesday = false)
    {
        var slots = new List<string>(2);
        var cursor = nowEt.Date.AddHours(10);
        if (cursor <= nowEt)
        {
            cursor = cursor.AddDays(1);
        }

        for (var i = 0; i < 21 && slots.Count < 2; i++)
        {
            var candidate = new DateTimeOffset(cursor.AddDays(i).Date.AddHours(10), nowEt.Offset);
            if (!Policy.AppointmentWindowOpen(candidate, customerRequestedTuesday))
            {
                continue;
            }

            slots.Add($"{candidate:dddd} at {candidate:h:mm tt}");
        }

        return slots;
    }
}
