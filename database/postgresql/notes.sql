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

