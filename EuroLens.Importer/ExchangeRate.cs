// Exchange rate: how many units of a currency was worth 1 euro, on a given date
public record ExchangeRate(DateOnly Date, string Currency, decimal Rate);

