CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.entities
(
  source TEXT NOT NULL,
  code TEXT NOT NULL,
  type TEXT NULL,
  name TEXT NULL,
  exchange TEXT NULL,
  country TEXT NULL,
  currency TEXT NULL,
  delisted BOOLEAN NOT NULL DEFAULT false,
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
  created_at TIMESTAMP WITH TIME ZONE NOT NULL,
  updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
  PRIMARY KEY (source, code)
);
