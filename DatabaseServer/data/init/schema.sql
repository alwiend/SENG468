use db;

CREATE TABLE user
(
id INTEGER AUTO_INCREMENT,
userid TEXT,
money INTEGER,
PRIMARY KEY (id)
) COMMENT='user table holds userid and how much money';

CREATE TABLE stocks
(
	id INTEGER AUTO_INCREMENT,
	userid TEXT,
	stock TEXT,
	price INTEGER,
	PRIMARY KEY(userid)
) COMMENT='table for which user owns what stock';