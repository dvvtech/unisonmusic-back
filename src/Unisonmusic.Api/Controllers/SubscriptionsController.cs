using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unisonmusic.Api.Extensions;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Controllers;

[Route("subscriptions")]
[ApiController]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentAccountId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var subscription = await _subscriptionService.GetSubscriptionAsync(userId.Value, cancellationToken);
        return Ok(subscription);
    }

    [HttpPost("create-payment")]
    public async Task<ActionResult<CreatePaymentResponse>> CreatePayment(
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentAccountId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var response = await _subscriptionService.CreatePaymentAsync(userId.Value, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("yookassa-callback")]
    [AllowAnonymous]
    public async Task<ActionResult> YooKassaCallback(
        [FromBody] YooKassaCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Event != "payment.succeeded" || request?.Object?.Metadata?.UserId is not { } userIdStr ||
            !int.TryParse(userIdStr, out var userId) ||
            string.IsNullOrWhiteSpace(request?.Object?.Id))
        {
            return Ok();
        }

        try
        {
            await _subscriptionService.ActivateSubscriptionAsync(userId, request.Object.Id, cancellationToken);
        }
        catch
        {
        }

        return Ok();
    }
}

public class YooKassaCallbackRequest
{
    public string? Event { get; set; }

    public YooKassaPaymentObject? Object { get; set; }
}

public class YooKassaPaymentObject
{
    public string? Id { get; set; }

    public YooKassaMetadata? Metadata { get; set; }
}

public class YooKassaMetadata
{
    public string? UserId { get; set; }
}
