
CREATE PROC [dbo].[sp_PampMetalPriceSync_DeleteUnneededRecords]
AS
	/*
	https://maginus.atlassian.net/browse/BULL-1490

	As Royal Mint
	I want unneeded historic indicative prices to be deleted
	so that data storage is well managed.

	In BULL-82 we cache indicative prices, and in BULL-412 we show historic prices as a Metal chart.
	Suggest we retain:

		* all indicative prices for the last 3 days (today and the previous 2 days)
		* the last indicative price of each 15 minutes of the day for the last 7 days
		* the last indicative price of each hour of the day for the last 200 days (~6 months)
		* the last indicative price of the day for ever.
		  All other indicative prices can be deleted.

	This could be a scheduled job which runs on a daily basis.
	This will give more than enough for support and metal price chart, without storing too much.

	----------------------------------------------------------------------------
	CR: https://maginus.atlassian.net/browse/BULL-2038
	Taking this into account, and aiming for simplicity rather that deleting every row we don't need, I think we should retain the following customer buy prices:
		* all prices up to 4 days
		* For days 4 to 7 keep 1st price in each 20 minutes, plus the first instance of the min and max for each day
		* days 8-200 keep 1st price each hour, plus the first instance of the min and max for each day
		* days 201+ keep 1st price each day, plus the first instance of the min and max for each day 
	*/

	-- AVOID DELETEING HIGHEST/LOWEST PRICE
	DECLARE @tblTemp TABLE(
		Currency nvarchar(50), 
		MaxGoldPrice decimal(18, 2), 
		MinGoldPrice decimal(18, 2), 
		MaxSilverPrice decimal(18, 2), 
		MinSilverPrice decimal(18, 2),
		MaxPlatinumPrice decimal(18, 2),
		MinPlatinumPrice decimal(18, 2))

	-- 1. All time
	INSERT INTO @tblTemp
	SELECT Currency, 
		Max(GoldPrice) as MaxGoldPrice, Min(GoldPrice) as MinGoldPrice,
		Max(SilverPrice) as MaxSilverPrice, Min(SilverPrice) as MinSilverPrice,
		Max(PlatinumPrice) as MaxPlatinumPrice, Min(PlatinumPrice) as MinPlatinumPrice
	FROM custom_PampMetalPriceSync 
	WHERE CustomerBuy=1 
	GROUP BY Currency
		, DATEPART(day, CreatedDate), DATEPART(MONTH, CreatedDate), DATEPART(YEAR, CreatedDate) -- First instance of the min and max for each day


	--------------------------- START DELETE --------------------------------------------
	--------------------------- LAST 7 DAYS --------------------------------------------
	;WITH cte
	AS(
		-- Select records to be retained
		SELECT DATEPART(year, CreatedDate) as [Year],
			 DATEPART(month, CreatedDate) as [Month],
			 DATEPART(day, CreatedDate) as [Day],
			 DATEPART(hour, CreatedDate) as [Hour], 
			 CAST(DATEPART(MINUTE, CreatedDate) / 20 as INT) as [MinuteStep],
			 Min(CreatedDate) as MinCreatedDate
		FROM custom_PampMetalPriceSync 
		WHERE CustomerBuy = 1
		AND DATEADD(DAY, 7, CreatedDate)>=GETUTCDATE() -- In the last 7 days
		AND DATEADD(DAY, 4, CreatedDate)<GETUTCDATE() -- Except first 4 days
		GROUP BY DATEPART(year, CreatedDate), DATEPART(month, CreatedDate), DATEPART(day, CreatedDate),
		 DATEPART(hour, CreatedDate), 
		 CAST(DATEPART(MINUTE, CreatedDate) / 20 as INT)
	)
	-- Start deleting used data for the last 7 days 
	DELETE T 
	FROM custom_PampMetalPriceSync T
	WHERE DATEADD(DAY, 7, T.CreatedDate)>=GETUTCDATE() -- In the last 7 days
		AND DATEADD(DAY, 4, T.CreatedDate)<GETUTCDATE() -- Except for the last 4 days
		AND NOT EXISTS(SELECT 1 FROM CTE WHERE cte.MinCreatedDate = T.CreatedDate)
		AND NOT EXISTS(
			SELECT 1 FROM @tblTemp tmp 
			WHERE tmp.Currency = T.Currency
					AND (tmp.MaxGoldPrice = T.GoldPrice
					OR tmp.MinGoldPrice = T.GoldPrice

					OR tmp.MaxSilverPrice = SilverPrice
					OR tmp.MinSilverPrice = SilverPrice

					OR tmp.MaxPlatinumPrice = T.PlatinumPrice
					OR tmp.MinPlatinumPrice = T.PlatinumPrice
					)
		)

	--------------------------- IN LAST 200 DAYS --------------------------------------------
	;WITH cte 
	AS(
		-- SELECT Records to be retained
		SELECT 
			DATEPART(year, CreatedDate) as [Year],
			DATEPART(month, CreatedDate) as [Month],
			DATEPART(day, CreatedDate) as [Day],
			DATEPART(hour, CreatedDate) as [Hour], 
			Min(CreatedDate) as MinCreatedDate
		FROM custom_PampMetalPriceSync 
		WHERE CustomerBuy = 1 AND DATEADD(DAY, 200, CreatedDate) >= GETUTCDATE() -- In last 200 days
			 AND DATEADD(DAY, 7, CreatedDate)<GETUTCDATE() -- Except for the last 7 days
		GROUP BY DATEPART(year, CreatedDate), 
				DATEPART(month, CreatedDate), 
				DATEPART(day, CreatedDate),
				DATEPART(hour, CreatedDate)
	)
	-- Start deleting unneeded data for the last 200 days
	DELETE T
	FROM custom_PampMetalPriceSync T
	WHERE DATEADD(DAY, 200, T.CreatedDate) >= GETUTCDATE() -- In last 200 days
		AND DATEADD(DAY, 7, T.CreatedDate)<GETUTCDATE() -- Except for the last 7 days
		AND NOT EXISTS(SELECT 1 FROM CTE WHERE cte.MinCreatedDate = T.CreatedDate)
		AND NOT EXISTS(
			SELECT 1 FROM @tblTemp tmp 
			WHERE tmp.Currency = T.Currency
					AND (tmp.MaxGoldPrice = T.GoldPrice
					OR tmp.MinGoldPrice = T.GoldPrice

					OR tmp.MaxSilverPrice = T.SilverPrice
					OR tmp.MinSilverPrice = T.SilverPrice

					OR tmp.MaxPlatinumPrice = T.PlatinumPrice
					OR tmp.MinPlatinumPrice = T.PlatinumPrice
					)
		)

	--------------------------- AFTER 200 DAYS --------------------------------------------
	;WITH cte 
	AS(
		-- SELECT Records to be retained
		SELECT 
			DATEPART(year, CreatedDate) as [Year],
			 DATEPART(month, CreatedDate) as [Month],
			 DATEPART(day, CreatedDate) as [Day],
			 Min(CreatedDate) as MinCreatedDate
		FROM custom_PampMetalPriceSync 
		WHERE CustomerBuy = 1 AND DATEADD(DAY, 200, CreatedDate) < GETUTCDATE() -- After 200 days
		GROUP BY DATEPART(year, CreatedDate), DATEPART(month, CreatedDate), DATEPART(day, CreatedDate)
	)
	-- Start deleting unneded data after 200 days
	DELETE T
	FROM custom_PampMetalPriceSync T
	WHERE DATEADD(DAY, 200, T.CreatedDate) < GETUTCDATE() -- After 200 days
		AND NOT EXISTS(SELECT 1 FROM CTE WHERE cte.MinCreatedDate = T.CreatedDate)
		AND NOT EXISTS(
			SELECT 1 FROM @tblTemp tmp 
			WHERE tmp.Currency = T.Currency
					AND (tmp.MaxGoldPrice = T.GoldPrice
					OR tmp.MinGoldPrice = T.GoldPrice

					OR tmp.MaxSilverPrice = T.SilverPrice
					OR tmp.MinSilverPrice = T.SilverPrice

					OR tmp.MaxPlatinumPrice = T.PlatinumPrice
					OR tmp.MinPlatinumPrice = T.PlatinumPrice
					)
		)
