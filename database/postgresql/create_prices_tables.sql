CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.splits
(
  source TEXT NOT NULL,
  code TEXT NOT NULL,
  date_split DATE NOT NULL,
  before_split DOUBLE PRECISION NOT NULL,
  after_split DOUBLE PRECISION NOT NULL,
  factor DOUBLE PRECISION NOT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  process_id UUID NULL,
  PRIMARY KEY (source, code, date_split)
);

CREATE TABLE IF NOT EXISTS public.eod_prices
(
  source TEXT NOT NULL,
  code TEXT NOT NULL,
  date_eod DATE NOT NULL,
  open NUMERIC(19,4) NOT NULL,
  high NUMERIC(19,4) NOT NULL,
  low NUMERIC(19,4) NOT NULL,
  close NUMERIC(19,4) NOT NULL,
  volume BIGINT NOT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  process_id UUID NULL,
  PRIMARY KEY (source, code, date_eod)
);

CREATE TABLE IF NOT EXISTS public.eod_adjusted_prices
(
  source TEXT NOT NULL,
  code TEXT NOT NULL,
  date_eod DATE NOT NULL,
  open NUMERIC(19,4) NOT NULL,
  high NUMERIC(19,4) NOT NULL,
  low NUMERIC(19,4) NOT NULL,
  close NUMERIC(19,4) NOT NULL,
  volume BIGINT NOT NULL,
  factor DOUBLE PRECISION NOT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  process_id UUID NULL,
  PRIMARY KEY (source, code, date_eod)
);

