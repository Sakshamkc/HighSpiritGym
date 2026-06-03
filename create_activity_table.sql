-- Create ActivityLogs table for HighSpirit Gym
-- Run this on the VPS MySQL database

CREATE TABLE IF NOT EXISTS `ActivityLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Action` varchar(50) NOT NULL,
    `EntityType` varchar(50) NOT NULL,
    `EntityId` int NOT NULL,
    `EntityName` varchar(200) NOT NULL,
    `Description` varchar(500) NULL,
    `PerformedBy` varchar(100) NOT NULL,
    `PerformedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Index for faster filtering
CREATE INDEX `IX_ActivityLogs_EntityType` ON `ActivityLogs` (`EntityType`);
CREATE INDEX `IX_ActivityLogs_PerformedAt` ON `ActivityLogs` (`PerformedAt` DESC);
