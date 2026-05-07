namespace Unisonmusic.Api.Models;

public sealed record ClientDeviceInfo(
    string? DeviceType,
    string? Model,
    string? Os);
