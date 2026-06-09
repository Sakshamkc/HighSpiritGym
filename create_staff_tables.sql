-- Create Staff and StaffAttendances tables
-- Run this on the VPS MySQL database

CREATE TABLE IF NOT EXISTS `Staff` (
    `StaffID` int NOT NULL AUTO_INCREMENT,
    `FullName` varchar(200) NOT NULL,
    `Phone` varchar(20) NOT NULL,
    `Gender` varchar(10) NULL,
    `Age` int NULL,
    `Position` varchar(100) NULL,
    `Photo` longblob NULL,
    `QrToken` varchar(64) NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `JoinDate` datetime(6) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`StaffID`),
    INDEX `IX_Staff_QrToken` (`QrToken`),
    INDEX `IX_Staff_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `StaffAttendances` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StaffID` int NOT NULL,
    `StaffName` varchar(200) NOT NULL,
    `CheckInTime` datetime(6) NOT NULL,
    `CheckOutTime` datetime(6) NULL,
    `Notes` varchar(500) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_StaffAttendances_StaffID` (`StaffID`),
    INDEX `IX_StaffAttendances_CheckInTime` (`CheckInTime` DESC),
    CONSTRAINT `FK_StaffAttendances_Staff` FOREIGN KEY (`StaffID`) REFERENCES `Staff` (`StaffID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
