-- =============================================
-- EventManagerDB - Database Setup Script
-- Run this script in SSMS to create the database,
-- tables, stored procedures, views, and seed data.
--
-- Prerequisites:
--   Create a SQL login before running this script:
--   CREATE LOGIN EventAdmin WITH PASSWORD = '<your_password>';
-- =============================================

CREATE DATABASE EventManagerDB;
GO

USE EventManagerDB;
GO

--DELETE FROM Events;
--GO

--DELETE FROM Participants;
--GO

--DELETE FROM Registrations;
--GO

-- Create user for the login
CREATE USER EventAdmin FOR LOGIN EventAdmin;
ALTER ROLE db_owner ADD MEMBER EventAdmin;
GO

-- =============================================
-- TABLES
-- =============================================

CREATE TABLE Events (
    EventId      INT IDENTITY(1,1) PRIMARY KEY,
    Title        NVARCHAR(200)  NOT NULL UNIQUE,
    EventDate    DATETIME       NOT NULL,
    Location     NVARCHAR(200)  NOT NULL,
    EventType    NVARCHAR(100)  NOT NULL
);

ALTER TABLE Events
ADD CONSTRAINT UQ_Events_Title UNIQUE (Title);

CREATE TABLE Participants (
    ParticipantId  INT IDENTITY(1,1) PRIMARY KEY,
    FirstName      NVARCHAR(100)  NOT NULL,
    LastName       NVARCHAR(100)  NOT NULL,
    Email          NVARCHAR(200)  NOT NULL
);

CREATE TABLE Registrations (
    RegistrationId  INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    ParticipantId   INT NOT NULL,
    Status          NVARCHAR(50) NOT NULL DEFAULT 'Unconfirmed',
    CONSTRAINT FK_Registrations_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Registrations_Participants FOREIGN KEY (ParticipantId)
        REFERENCES Participants(ParticipantId) ON DELETE CASCADE
);
GO

-- =============================================
-- VIEW: Registration details with joined data
-- =============================================

CREATE VIEW vw_RegistrationDetails AS
SELECT
    r.RegistrationId,
    e.Title        AS EventTitle,
    e.EventDate,
    e.Location,
    e.EventType,
    p.FirstName,
    p.LastName,
    p.Email,
    r.Status
FROM Registrations r
INNER JOIN Events e ON r.EventId = e.EventId
INNER JOIN Participants p ON r.ParticipantId = p.ParticipantId;
GO

-- =============================================
-- STORED PROCEDURE: Add a participant
-- =============================================

CREATE PROCEDURE sp_AddParticipant
    @FirstName  NVARCHAR(100),
    @LastName   NVARCHAR(100),
    @Email      NVARCHAR(200)
AS
BEGIN
    INSERT INTO Participants (FirstName, LastName, Email)
    VALUES (@FirstName, @LastName, @Email);

    SELECT SCOPE_IDENTITY() AS NewParticipantId;
END;
GO

-- =============================================
-- STORED PROCEDURE: Register participant for event
-- (uses a transaction)
-- =============================================

CREATE PROCEDURE sp_RegisterParticipant
    @EventId        INT,
    @ParticipantId  INT,
    @Status         NVARCHAR(50) = 'Unconfirmed'
AS
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Check if already registered
        IF EXISTS (
            SELECT 1 FROM Registrations
            WHERE EventId = @EventId AND ParticipantId = @ParticipantId
        )
        BEGIN
            RAISERROR('Participant is already registered for this event.', 16, 1);
            RETURN;
        END

        INSERT INTO Registrations (EventId, ParticipantId, Status)
        VALUES (@EventId, @ParticipantId, @Status);

        COMMIT TRANSACTION;
        SELECT SCOPE_IDENTITY() AS NewRegistrationId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- SAMPLE DATA
-- =============================================

INSERT INTO Events (Title, EventDate, Location, EventType) VALUES
('Tech Conference 2026',      '2026-05-15 10:00', 'Convention Center',   'Conference'),
('C# Workshop',               '2026-06-01 14:00', 'University Hall',     'Workshop'),
('Hackathon Spring 2026',     '2026-04-20 09:00', 'Tech Hub',            'Competition'),
('Dev Community Meetup',      '2026-07-10 18:00', 'Cafe Digital',        'Meetup'),
('AI & ML Summit',            '2026-08-22 09:30', 'Grand Hall',          'Conference'),
('Web Dev Bootcamp',          '2026-06-15 10:00', 'Online',              'Workshop'),
('Cybersecurity Forum',       '2026-09-05 11:00', 'Security Center',     'Conference'),
('Mobile Dev Meetup',         '2026-07-25 18:30', 'StartUp Hub',         'Meetup'),
('Data Science Competition',  '2026-10-10 09:00', 'University Hall',     'Competition'),
('Cloud Computing Workshop',  '2026-11-03 13:00', 'Tech Hub',            'Workshop');

INSERT INTO Participants (FirstName, LastName, Email) VALUES
('John',    'Smith',     'john.smith@gmail.com'),
('Alice',   'Johnson',   'alice.johnson@outlook.com'),
('Bob',     'Williams',  'bob.williams@gmail.com'),
('Emma',    'Brown',     'emma.brown@yahoo.com'),
('Michael', 'Davis',     'michael.davis@gmail.com'),
('Sofia',   'Martinez',  'sofia.martinez@hotmail.com'),
('Liam',    'Wilson',    'liam.wilson@outlook.com'),
('Olivia',  'Taylor',    'olivia.taylor@gmail.com'),
('Noah',    'Anderson',  'noah.anderson@ukr.net'),
('Ava',     'Thomas',    'ava.thomas@icloud.com');

INSERT INTO Registrations (EventId, ParticipantId, Status) VALUES
(1,  1,  'Confirmed'),
(1,  2,  'Confirmed'),
(1,  3,  'Unconfirmed'),
(1,  4,  'Confirmed'),
(2,  5,  'Confirmed'),
(2,  6,  'Unconfirmed'),
(2,  7,  'Confirmed'),
(3,  1,  'Confirmed'),
(3,  8,  'Confirmed'),
(3,  9,  'Unconfirmed'),
(4,  2,  'Confirmed'),
(4,  10, 'Confirmed'),
(5,  3,  'Unconfirmed'),
(5,  4,  'Confirmed'),
(5,  5,  'Confirmed'),
(6,  6,  'Confirmed'),
(6,  7,  'Unconfirmed'),
(7,  8,  'Confirmed'),
(7,  9,  'Confirmed'),
(8,  10, 'Unconfirmed'),
(8,  1,  'Confirmed'),
(9,  2,  'Confirmed'),
(9,  3,  'Confirmed'),
(10, 4,  'Unconfirmed'),
(10, 5,  'Confirmed');
GO

-- DBCC CHECKIDENT ('Events',        RESEED, 0);
-- DBCC CHECKIDENT ('Participants',  RESEED, 0);
-- DBCC CHECKIDENT ('Registrations', RESEED, 0);