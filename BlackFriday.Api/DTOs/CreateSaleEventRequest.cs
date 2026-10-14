namespace BlackFriday.Api.DTOs;

public record CreateSaleEventRequest(
    string Sku,
    int TotalCapacity,
    decimal OriginalPrice,
    decimal DiscountPrice,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);