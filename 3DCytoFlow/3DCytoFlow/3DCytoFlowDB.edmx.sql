
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 12/28/2015 09:23:29
-- Generated from EDMX file: C:\Users\llavieri.CSISOFT\Documents\Senior Design\SeniorDesign\3DCytoFlow\3DCytoFlow\3DCytoFlowDB.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [master];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------


-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Users'
CREATE TABLE [dbo].[Users] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [FirstName] nvarchar(max)  NOT NULL,
    [Middle] nvarchar(max) NULL,
    [LastName] nvarchar(max)  NOT NULL,
    [DOB] datetime NOT NULL,
    [Login] nvarchar(max)  NOT NULL,
    [Password] nvarchar(max)  NOT NULL,
    [Email] nvarchar(max)  NOT NULL,
    [Phone] nvarchar(max)  NOT NULL,
    [Address] nvarchar(max)  NOT NULL,
    [City] nvarchar(max)  NOT NULL,
    [Zip] nvarchar(max)  NOT NULL,
    [UserRole_Id] int  NOT NULL
);
GO

-- Creating table 'UserRoles'
CREATE TABLE [dbo].[UserRoles] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'Patients'
CREATE TABLE [dbo].[Patients] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [FirstName] nvarchar(max)  NOT NULL,
    [Middle] nvarchar(max) NULL,
    [LastName] nvarchar(max)  NOT NULL,
    [DOB] datetime NOT NULL,
    [Email] nvarchar(max)  NOT NULL,
    [Phone] nvarchar(max)  NOT NULL,
    [Address] nvarchar(max)  NOT NULL,
    [City] nvarchar(max)  NOT NULL,
    [Zip] nvarchar(max)  NOT NULL,
    [User_Id] int  NOT NULL
);
GO

-- Creating table 'Analyses'
CREATE TABLE [dbo].[Analyses] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Date] datetime NOT NULL,
    [FcsFilePath] nvarchar(max)  NOT NULL,
    [FcsUploadDate] datetime NOT NULL,
    [ResultFilePath] nvarchar(max)  NOT NULL,
    [ResultDate] datetime NOT NULL,
    [Delta] float NOT NULL,
    [User_Id] int  NOT NULL,
    [Patient_Id] int  NOT NULL
);
GO

-- Creating table 'Clusters'
CREATE TABLE [dbo].[Clusters] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [X] float  NOT NULL,
    [Y] float  NOT NULL,
    [Z] float  NOT NULL,
    [Width] float NOT NULL,
    [Height] float NOT NULL,
    [Depth] float NOT NULL,
    [Analysis_Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'Users'
ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [PK_Users]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'UserRoles'
ALTER TABLE [dbo].[UserRoles]
ADD CONSTRAINT [PK_UserRoles]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Patients'
ALTER TABLE [dbo].[Patients]
ADD CONSTRAINT [PK_Patients]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Analyses'
ALTER TABLE [dbo].[Analyses]
ADD CONSTRAINT [PK_Analyses]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Clusters'
ALTER TABLE [dbo].[Clusters]
ADD CONSTRAINT [PK_Clusters]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UserRole_Id] in table 'Users'
ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [FK_UserRolesUser]
    FOREIGN KEY ([UserRole_Id])
    REFERENCES [dbo].[UserRoles]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserRolesUser'
CREATE INDEX [IX_FK_UserRolesUser]
ON [dbo].[Users]
    ([UserRole_Id]);
GO

-- Creating foreign key on [User_Id] in table 'Patients'
ALTER TABLE [dbo].[Patients]
ADD CONSTRAINT [FK_UserPatient]
    FOREIGN KEY ([User_Id])
    REFERENCES [dbo].[Users]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserPatient'
CREATE INDEX [IX_FK_UserPatient]
ON [dbo].[Patients]
    ([User_Id]);
GO

-- Creating foreign key on [User_Id] in table 'Analyses'
ALTER TABLE [dbo].[Analyses]
ADD CONSTRAINT [FK_UserAnalysis]
    FOREIGN KEY ([User_Id])
    REFERENCES [dbo].[Users]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserAnalysis'
CREATE INDEX [IX_FK_UserAnalysis]
ON [dbo].[Analyses]
    ([User_Id]);
GO

-- Creating foreign key on [Patient_Id] in table 'Analyses'
ALTER TABLE [dbo].[Analyses]
ADD CONSTRAINT [FK_PatientAnalysis]
    FOREIGN KEY ([Patient_Id])
    REFERENCES [dbo].[Patients]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_PatientAnalysis'
CREATE INDEX [IX_FK_PatientAnalysis]
ON [dbo].[Analyses]
    ([Patient_Id]);
GO

-- Creating foreign key on [Analysis_Id] in table 'Clusters'
ALTER TABLE [dbo].[Clusters]
ADD CONSTRAINT [FK_AnalysisCluster]
    FOREIGN KEY ([Analysis_Id])
    REFERENCES [dbo].[Analyses]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_AnalysisCluster'
CREATE INDEX [IX_FK_AnalysisCluster]
ON [dbo].[Clusters]
    ([Analysis_Id]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------