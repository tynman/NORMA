﻿
START TRANSACTION ISOLATION LEVEL SERIALIZABLE, READ WRITE;

CREATE SCHEMA BlogDemo DEFAULT CHARACTER SET UTF8;

SET SCHEMA 'BLOGDEMO';

CREATE TABLE BlogDemo.BlogEntry
(
	blogEntryId INTEGER NOT NULL,
	entryTitle CHARACTER VARYING(30) NOT NULL,
	entryBody CHARACTER LARGE OBJECT NOT NULL,
	postedDate TIMESTAMP NOT NULL,
	firstName CHARACTER VARYING(30) NOT NULL,
	lastName CHARACTER VARYING(30) NOT NULL,
	blogCommentParentEntryIdBlogEntry_Id INTEGER,
	CONSTRAINT BlogEntry_PK PRIMARY KEY(blogEntryId)
);

CREATE TABLE BlogDemo."User"
(
	firstName CHARACTER VARYING(30) NOT NULL,
	lastName CHARACTER VARYING(30) NOT NULL,
	username CHARACTER VARYING(30) NOT NULL,
	password CHARACTER(32) NOT NULL,
	CONSTRAINT User_PK PRIMARY KEY(firstName, lastName)
);

CREATE TABLE BlogDemo.BlogLabel
(
	blogLabelId INTEGER GENERATED ALWAYS AS IDENTITY(START WITH 1 INCREMENT BY 1) NOT NULL,
	title CHARACTER LARGE OBJECT,
	CONSTRAINT BlogLabel_PK PRIMARY KEY(blogLabelId)
);

CREATE TABLE BlogDemo.BlogEntryLabel
(
	blogEntryId INTEGER NOT NULL,
	blogLabelId INTEGER NOT NULL,
	CONSTRAINT BlogEntryLabel_PK PRIMARY KEY(blogEntryId, blogLabelId)
);

ALTER TABLE BlogDemo.BlogEntry ADD CONSTRAINT BlogEntry_FK1 FOREIGN KEY (firstName, lastName) REFERENCES BlogDemo."User" (firstName, lastName) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE BlogDemo.BlogEntry ADD CONSTRAINT BlogEntry_FK2 FOREIGN KEY (blogCommentParentEntryIdBlogEntry_Id) REFERENCES BlogDemo.BlogEntry (blogEntryId) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE BlogDemo.BlogEntryLabel ADD CONSTRAINT BlogEntryLabel_FK1 FOREIGN KEY (blogEntryId) REFERENCES BlogDemo.BlogEntry (blogEntryId) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE BlogDemo.BlogEntryLabel ADD CONSTRAINT BlogEntryLabel_FK2 FOREIGN KEY (blogLabelId) REFERENCES BlogDemo.BlogLabel (blogLabelId) ON DELETE RESTRICT ON UPDATE RESTRICT;

COMMIT WORK;
