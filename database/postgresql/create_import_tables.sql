CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.api_transactions
(
    id serial NOT NULL,
    ticks_utc BIGINT NOT NULL,
    timestamp_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((ticks_utc - 621355968000000000) / 10000000)) STORED,
    source TEXT NOT NULL,
    category TEXT NOT NULL,
    sub_category TEXT NULL,
    request_uri TEXT NOT NULL,
    request_method TEXT NOT NULL,
    request_payload TEXT NULL,
    request_headers TEXT NOT NULL,
    response_headers TEXT NOT NULL,
    response_status_code TEXT NOT NULL,
    response_body TEXT NOT NULL,
    process_id UUID NULL,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS api_transactions_source_idx ON public.api_transactions (source, category, timestamp_utc);
CREATE INDEX IF NOT EXISTS api_transactions_process_id_idx ON public.api_transactions (process_id);
