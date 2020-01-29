use db;

CREATE TABLE user
(
id INTEGER AUTO_INCREMENT,
userid TEXT,
money INTEGER,
PRIMARY KEY (id)
) COMMENT='user table holds userid and how much money';