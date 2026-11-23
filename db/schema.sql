
-- One row per date and currency (PK)
-- Units of the rate is the currency per 1 euro.
CREATE TABLE exchange_rates (
    rate_date  DATE           NOT NULL,
    currency   CHAR(3)        NOT NULL,
    rate       NUMERIC(18, 6) NOT NULL,
    PRIMARY KEY (rate_date, currency)
);