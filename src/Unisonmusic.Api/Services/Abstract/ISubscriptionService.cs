using Unisonmusic.Api.Models.Dtos;

namespace Unisonmusic.Api.Services.Abstract;

public interface ISubscriptionService
{
    Task<SubscriptionDto> GetSubscriptionAsync(int userId, CancellationToken cancellationToken);

    Task<CreatePaymentResponse> CreatePaymentAsync(int userId, CancellationToken cancellationToken);

    Task ActivateSubscriptionAsync(int userId, string yooKassaPaymentId, CancellationToken cancellationToken);

    int GetMaxPlaylists(string plan);
    int GetMaxRoomMembers(string plan);
    bool HasBackgroundPlay(string plan);
}
