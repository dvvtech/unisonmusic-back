namespace Unisonmusic.Api.Models.Dtos;

public class CreatePaymentResponse
{
    public string PaymentId { get; set; } = string.Empty;

    public string ConfirmationUrl { get; set; } = string.Empty;
}
