using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL;
using Unisonmusic.Api.DAL.Entities;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private const int BasicMaxPlaylists = int.MaxValue;
    private const int PlusMaxPlaylists = int.MaxValue;
    private const int BasicMaxRoomMembers = int.MaxValue;
    private const int PlusMaxRoomMembers = int.MaxValue;

    private readonly IDbContextFactory<UnisonmusicDbContext> _dbContextFactory;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IDbContextFactory<UnisonmusicDbContext> dbContextFactory,
        ILogger<SubscriptionService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<SubscriptionDto> GetSubscriptionAsync(int userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var plan = GetActivePlan(subscription);

        return new SubscriptionDto
        {
            Plan = plan,
            DisplayName = SubscriptionPlan.DisplayName(plan),
            IsActive = plan == SubscriptionPlan.Plus,
            ExpiresAtUtc = subscription?.ExpiresAtUtc,
            Limits = new SubscriptionLimitsDto
            {
                MaxPlaylists = GetMaxPlaylists(plan),
                MaxRoomMembers = GetMaxRoomMembers(plan),
                BackgroundPlay = HasBackgroundPlay(plan)
            }
        };
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(int userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (existing is not null && existing.Plan == SubscriptionPlan.Plus)
        {
            throw new InvalidOperationException("Unison+ уже приобретён.");
        }

        var paymentId = Guid.NewGuid().ToString("N");
        var confirmationUrl = $"https://yookassa.ru/checkout/payments/{paymentId}";

        _logger.LogInformation(
            "Created YooKassa payment stub for user {UserId}: {PaymentId}", userId, paymentId);

        return new CreatePaymentResponse
        {
            PaymentId = paymentId,
            ConfirmationUrl = confirmationUrl
        };
    }

    public async Task ActivateSubscriptionAsync(int userId, string yooKassaPaymentId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            existing.Plan = SubscriptionPlan.Plus;
            existing.YooKassaPaymentId = yooKassaPaymentId;
            existing.ActivatedAtUtc = DateTime.UtcNow;
            existing.ExpiresAtUtc = null;
        }
        else
        {
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                UserId = userId,
                Plan = SubscriptionPlan.Plus,
                YooKassaPaymentId = yooKassaPaymentId,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = null
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Activated Unison+ subscription for user {UserId} via payment {PaymentId}", userId, yooKassaPaymentId);
    }

    public int GetMaxPlaylists(string plan) =>
        plan == SubscriptionPlan.Plus ? PlusMaxPlaylists : BasicMaxPlaylists;

    public int GetMaxRoomMembers(string plan) =>
        plan == SubscriptionPlan.Plus ? PlusMaxRoomMembers : BasicMaxRoomMembers;

    public bool HasBackgroundPlay(string plan) => true;

    private static string GetActivePlan(SubscriptionEntity? subscription)
    {
        if (subscription is null)
        {
            return SubscriptionPlan.Basic;
        }

        if (subscription.Plan == SubscriptionPlan.Plus)
        {
            return SubscriptionPlan.Plus;
        }

        return SubscriptionPlan.Basic;
    }
}
