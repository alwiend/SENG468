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

DELIMITER $$
CREATE PROCEDURE buy_stock(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pServerTime TEXT,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userMoney INTEGER DEFAULT(-1);
	
	SELECT money 
	INTO userMoney
	FROM user 
	WHERE userid = pUserId;
	
	IF userMoney < 0 THEN
		SET message = "User does not exist";
		SET success = false;
	ELSEIF userMoney < pStockAmount THEN
		SET message = "Insufficient money";
		SET success = false;
	ELSE
		BEGIN
			INSERT INTO transactions (userid, stock, price, transType, transTime)
			VALUES (pUserId, pStock, pStockAmount, 'BUY', pServerTime);
			
			UPDATE user
			SET money = money - pStockAmount
			WHERE userid = pUserId;
			
			SET success = true;
			SET message = "";
		END;
	END IF;
END$$

DELIMITER ;