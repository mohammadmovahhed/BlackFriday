namespace BlackFriday.Api.DTOs;

public record CreateReservationRequest(
    Guid SaleEventId,
    string Sku,
    int Quantity,
    string CustomerId);