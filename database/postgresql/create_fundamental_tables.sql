CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.entities
(
  source TEXT NOT NULL,
  code TEXT NOT NULL,
  type TEXT NOT NULL,
  name TEXT NOT NULL,
  exchange TEXT NOT NULL,
  country TEXT NOT NULL,
  currency TEXT NOT NULL,
  delisted BOOLEAN DEFAULT false,
  ignored BOOLEAN NOT NULL DEFAULT false,
  has_splits BOOLEAN NOT NULL DEFAULT false,
  has_dividends BOOLEAN NOT NULL DEFAULT false,
  has_price_actions BOOLEAN NOT NULL DEFAULT false,
  has_fundamentals BOOLEAN NOT NULL DEFAULT false,
  last_price_action_date DATE NULL,
  last_fundamental_date DATE NULL,
  next_fundamental_date DATE NULL,
  ignored_reason TEXT NULL,
  sector TEXT NULL,
  industry TEXT NULL,
  gic_sector TEXT NULL,
  gic_group TEXT NULL,
  gic_industry TEXT NULL,
  gic_sub_industry TEXT NULL,
  web_url TEXT NULL,
  phone TEXT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  PRIMARY KEY (source, code)
);