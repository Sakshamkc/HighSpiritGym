CREATE TABLE IF NOT EXISTS Attendances (
    AttendanceID INT AUTO_INCREMENT PRIMARY KEY,
    CustomerID INT NOT NULL,
    CustomerName VARCHAR(255) NOT NULL DEFAULT '',
    CheckInTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CheckOutTime DATETIME NULL,
    Notes TEXT NULL,
    INDEX IX_Attendances_CheckInTime (CheckInTime),
    INDEX IX_Attendances_CustomerID (CustomerID)
);

CREATE TABLE IF NOT EXISTS GymSchedules (
    ScheduleID INT AUTO_INCREMENT PRIMARY KEY,
    DayOfWeek VARCHAR(20) NOT NULL,
    ClassName VARCHAR(100) NOT NULL,
    StartTime VARCHAR(10) NOT NULL,
    EndTime VARCHAR(10) NOT NULL,
    Instructor VARCHAR(100) NULL,
    Category VARCHAR(50) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1
);
