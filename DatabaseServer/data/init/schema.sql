use db;

CREATE TABLE user
(
userid VARCHAR(25),
money INTEGER,
PRIMARY KEY (userid)
) COMMENT='user table holds userid and how much money';

CREATE TABLE stocks
(
userid VARCHAR(25),
stock VARCHAR(3),
price INTEGER,
PRIMARY KEY (userid,stock)
) COMMENT='table for which user owns what stock';

CREATE TABLE transactions
(
id INTEGER AUTO_INCREMENT,
userid VARCHAR(25),
stock VARCHAR(3),
price INTEGER,
transType TEXT,
transTime TEXT,
PRIMARY KEY (id)
) COMMENT='table for pending buy/sell transactions';

CREATE TABLE triggers
(
userid VARCHAR(25),
stock VARCHAR(3),
amount INTEGER,
triggerType VARCHAR(4),
triggerAmount INTEGER,
PRIMARY KEY(userid,stock,triggerType)
) COMMENT='table for buy/sell triggers';