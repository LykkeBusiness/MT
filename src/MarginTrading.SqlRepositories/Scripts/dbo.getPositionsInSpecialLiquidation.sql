-- Copyright (c) 2019 BNP Paribas Arbitrage. All rights reserved.

CREATE
OR ALTER PROCEDURE
[dbo].[getPositionsInSpecialLiquidation]
    @positions as [dbo].[PositionListDataType] READONLY
    AS
-- =====================================================================================
-- Description:	Validate if provided positions are in the process of Special Liquidation.
-- Returns the ones which are already used for Special Liquidation.
-- =====================================================================================

BEGIN
SELECT positions.Id as PositionId
FROM [dbo].[MarginTradingExecutionInfo] e
    CROSS APPLY
    (
    SELECT
    REPLACE(value, '"', '') as Id
    FROM STRING_SPLIT(
        REPLACE(
            REPLACE(
                JSON_QUERY(e.Data, '$.PositionIds'), 
                '[', 
                ''), 
            ']', 
            ''), 
        ',')
    ) positions
WHERE
    e.OperationName = 'SpecialLiquidation'
  AND
    JSON_VALUE(e.Data
    , '$.State') NOT IN ('OnTheWayToFail'
    , 'Failed'
    , 'Cancelled')

INTERSECT

SELECT Id
FROM @positions
END
