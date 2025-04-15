```sql
select source, code, count(*) as Count
FROM eod_adjusted_prices
where code in ('AAPL','SPY','QQQ','DJIA')
GROUP BY source, code;
```
---

WITH Prices AS (
    SELECT id, timestamp_utc, category, sub_category,
	ROW_NUMBER() OVER (PARTITION BY source --, category, sub_category
					    ORDER BY id desc) AS rn
    FROM api_transactions where source = 'eodhd.com'
		and category = 'EOD Prices'
		and sub_category = 'AAPL.US'
)
SELECT *
FROM Prices
WHERE rn = 1;

WITH Splits AS (
    SELECT id, timestamp_utc, category, sub_category,
	ROW_NUMBER() OVER (PARTITION BY source
					    ORDER BY id desc) AS rn
    FROM api_transactions where source = 'eodhd.com'
		and category = 'Splits'
		and sub_category = 'AAPL.US'
)
SELECT *
FROM Splits
WHERE rn = 1;

WITH Prices AS
(SELECT id, category, sub_category, ROW_NUMBER() OVER
(PARTITION BY source, category, sub_category ORDER BY id desc) AS rn
FROM api_transactions where source = 'eodhd.com'
and category in ('EOD Prices','Splits'))
SELECT *
FROM Prices
WHERE rn = 1;

select distinct result_direction, count(*), min(result_duration_calendar_days), 
max(result_duration_calendar_days),
avg(result_duration_calendar_days) from backtest_results
group by result_direction


SELECT signal_name, result_direction, COUNT(*)
from backtest_results
group BY signal_name, result_direction
ORDER BY signal_name;

select signal_name, category, sub_category,
number_signals, success_percentage, success_duration_calendar_days
from backtest_stats
where number_signals > 10
order by success_percentage desc, success_duration_calendar_days asc