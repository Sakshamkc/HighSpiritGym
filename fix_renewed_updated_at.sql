-- Fix UpdatedAt for customers with recent renewals that weren't tracked
-- Run this once to backfill existing renewed customers into "Recently Updated"

UPDATE Customers c
INNER JOIN (
    SELECT CustomerID, MAX(CreatedAt) AS LatestRenewal
    FROM CustomerMemberships
    WHERE CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
    GROUP BY CustomerID
) m ON c.CustomerID = m.CustomerID
SET c.UpdatedAt = m.LatestRenewal
WHERE c.UpdatedAt IS NULL OR c.UpdatedAt < m.LatestRenewal;
