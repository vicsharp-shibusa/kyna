CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.logs
(
    id serial NOT NULL,
    ticks_utc BIGINT NOT NULL,
    timestamp_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((ticks_utc - 621355968000000000) / 10000000)) STORED,
    process_id UUID NULL,
    log_level TEXT NULL,
    message TEXT NULL,
    exception TEXT NULL,
    scope TEXT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.app_events
(
    id serial NOT NULL,
    ticks_utc BIGINT NOT NULL,
    timestamp_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((ticks_utc - 621355968000000000) / 10000000)) STORED,
    process_id UUID NULL,
    event_id INTEGER NOT NULL,
    event_name TEXT NULL,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS logs_timestamp_idx ON public.logs (timestamp_utc);
CREATE INDEX IF NOT EXISTS logs_process_id_idx ON public.logs (process_id);

CREATE INDEX IF NOT EXISTS events_timestamp_idx ON public.app_events (timestamp_utc);
CREATE INDEX IF NOT EXISTS events_process_id_idx ON public.app_events (process_id);