namespace Unisonmusic.Api.DAL.Entities;

public class SubscriptionEntity
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string Plan { get; set; } = SubscriptionPlan.Basic;

    public string? YooKassaPaymentId { get; set; }

    public DateTime ActivatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

public static class SubscriptionPlan
{
    public const string Basic = "basic";
    public const string Plus = "plus";

    public static string DisplayName(string plan) => plan switch
    {
        Plus => "Unison+",
        _ => "Базовый"
    };
}
