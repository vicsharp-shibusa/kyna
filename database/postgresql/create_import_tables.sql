CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.api_transactions
(
    id serial NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at_unix_ms BIGINT NOT NULL,
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

CREATE INDEX IF NOT EXISTS api_transactions_source_idx ON public.api_transactions (source, category, created_at);
CREATE INDEX IF NOT EXISTS api_transactions_process_id_idx ON public.api_transactions (process_id);

CREATE TABLE IF NOT EXISTS public.remote_files
(
    source TEXT NOT NULL,
    provider TEXT NOT NULL,
    location text NOT NULL,
    name TEXT NOT NULL,
    update_date DATE NOT NULL,
    size BIGINT NOT NULL,
    hash_code TEXT NULL,
    process_id UUID NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at_unix_ms BIGINT NOT NULL,
    updated_at_unix_ms BIGINT NOT NULL,
    PRIMARY KEY (source, provider, location, name)
);