CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public.backtests
(
  id UUID NOT NULL,
  name TEXT NOT NULL,
  type TEXT NOT NULL,
  source TEXT NOT NULL,
  description TEXT NOT NULL,
  entry_price_point TEXT NOT NULL,
  target_up_percentage DOUBLE PRECISION NOT NULL,
  target_up_price_point TEXT NOT NULL,
  target_down_percentage DOUBLE PRECISION NOT NULL,
  target_down_price_point TEXT NOT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  process_id UUID NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.backtest_results
(
  id UUID NOT NULL,
  backtest_id UUID NOT NULL,
  signal_name TEXT NOT NULL,
  code TEXT NOT NULL,
  industry TEXT NULL,
  sector TEXT NULL,
  entry_date DATE NOT NULL,
  entry_price_point TEXT NOT NULL,
  entry_price NUMERIC(22,4) NOT NULL,
  result_up_date DATE NULL,
  result_up_price_point TEXT NULL,
  result_up_price NUMERIC(22,4) NULL,
  result_down_date DATE NULL,
  result_down_price_point TEXT NULL,
  result_down_price NUMERIC(22,4) NULL,
  result_direction TEXT NULL,
  result_duration_trading_days INTEGER NULL,
  result_duration_calendar_days INTEGER NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.backtest_stats
(
  source TEXT NOT NULL,
  signal_name TEXT NOT NULL,
  category TEXT NOT NULL,
  sub_category TEXT NULL,
  number_entities INTEGER NOT NULL,
  number_signals INTEGER NOT NULL,
  success_percentage DOUBLE PRECISION NOT NULL,
  success_criterion TEXT NOT NULL,
  success_duration_trading_days INTEGER NULL,
  success_duration_calendar_days INTEGER NULL,
  process_id UUID NULL,
  backtest_id UUID NOT NULL,
  created_ticks_utc BIGINT NOT NULL,
  updated_ticks_utc BIGINT NOT NULL,
  created_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((created_ticks_utc - 621355968000000000) / 10000000)) STORED,
  updated_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((updated_ticks_utc - 621355968000000000) / 10000000)) STORED,
  PRIMARY KEY (backtest_id, source, signal_name, category, sub_category)
);