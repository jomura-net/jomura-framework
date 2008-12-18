CREATE DATABASE [Jomura.Framework.Test]
GO

USE [Jomura.Framework.Test]
GO

CREATE TABLE [TestTable01](
	[id] [bigint] NOT NULL,
	[value] [nvarchar](max) NULL,
    PRIMARY KEY ( [id] )
)

CREATE DATABASE [Jomura.Framework.Test2]
GO

USE [Jomura.Framework.Test2]
GO

CREATE TABLE [TestTable02](
	[id] [bigint] NOT NULL,
	[value] [nvarchar](max) NULL,
    PRIMARY KEY ( [id] )
)
