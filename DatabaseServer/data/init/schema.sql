use db;

SET GLOBAL event_scheduler = ON;

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
transTime BIGINT,
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

CREATE PROCEDURE display_summary(
IN pUserId VARCHAR(25),
OUT pMoney INTEGER)

BEGIN
	SELECT money 
	INTO pMoney
	FROM user 
	WHERE userid = pUserId;
	
	IF pMoney IS NOT NULL THEN
		SELECT stock, price
		FROM stocks
		WHERE userid = pUserId;
	ELSE
		SET pMoney = -1;
	END IF;

END$$

CREATE PROCEDURE add_user(
IN pUserId VARCHAR(25),
IN pFunds INTEGER)

BEGIN	
	INSERT INTO user (userid, money) VALUES (pUserId,pFunds)
    ON DUPLICATE KEY UPDATE money = money + pFunds;
END$$

CREATE PROCEDURE buy_stock(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pServerTime BIGINT,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userMoney INTEGER DEFAULT(-1);
	
	SELECT money 
	INTO userMoney
	FROM user 
	WHERE userid = pUserId;
	
	IF userMoney < 0 THEN
		BEGIN
			SET message = "User does not exist";
			SET success = false;
		END;
	ELSEIF userMoney < pStockAmount THEN
		BEGIN
			SET message = "Insufficient money";
			SET success = false;
		END;
	ELSE
		BEGIN
			UPDATE user
			SET money = money - pStockAmount
			WHERE userid = pUserId;
			
			INSERT INTO transactions (userid, stock, price, transType, transTime)
			VALUES (pUserId, pStock, pStockAmount, 'BUY', pServerTime);
			
			SET success = true;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE buy_commit(
IN pUserId VARCHAR(25),
IN pServerTime BIGINT,
OUT success BOOLEAN,
OUT message TEXT,
OUT stockBuy VARCHAR(3),
OUT stockAmount INTEGER)

BEGIN
	DECLARE buyTime BIGINT;
	DECLARE buyId INTEGER;

	SELECT id, stock, price, transTime 
	INTO buyId, stockBuy, stockAmount, buyTime
	FROM transactions
    WHERE userid = pUserId AND transType='BUY'
	ORDER BY transTime DESC
	LIMIT 1;
	
	IF buyId IS NULL THEN
		BEGIN
			SET message = "No recent buys";
			SET success = false;
			SET stockBuy = "";
			SET stockAmount = 0;
		END;
	ELSEIF pServerTime - buyTime > 60000 THEN
		BEGIN
			SET message = "No recent buys";
			SET success = false;
		
			UPDATE user 
			SET money = money + stockAmount
			WHERE userid = pUserId;
		
			DELETE FROM transactions
			WHERE id = buyId;
		END;
	ELSE
		BEGIN
			INSERT INTO stocks (userid, stock, price) 
			VALUES (pUserId, stockBuy, stockAmount)
			ON DUPLICATE KEY UPDATE price = price + stockAmount;
			
			DELETE FROM transactions
			WHERE id = buyId;
			
			SET success = true;
			SET message = "";
		END;
	END IF;

END$$

CREATE PROCEDURE buy_cancel_stock(
IN pUserId VARCHAR(25),
IN pServerTime BIGINT,
OUT pStock VARCHAR(3),
OUT pStockAmount INTEGER,
OUT success BOOLEAN,
out message TEXT)

BEGIN	
	DECLARE buyTime BIGINT;
	DECLARE buyId INTEGER;

	SELECT id, stock, price, transTime 
	INTO buyId, pStock, pStockAmount, buyTime
	FROM transactions
    WHERE userid = pUserId AND transType='BUY'
	ORDER BY transTime DESC
	LIMIT 1;
	
	IF buyId IS NULL THEN
		BEGIN
			SET message = "No recent buys";
			SET success = false;
			SET pStock = "";
			SET pStockAmount = 0;
		END;
	ELSEIF pServerTime - buyTime > 60000 THEN
		BEGIN
			SET message = "No recent buys";
			SET success = false;
			
			UPDATE user 
			SET money = money + pStockAmount
			WHERE userid = pUserId;
			
			DELETE FROM transactions
			WHERE id = buyId;
		END;
	ELSE
		BEGIN
			UPDATE user 
			SET money = money + pStockAmount
			WHERE userid = pUserId;
			
			DELETE FROM transactions
			WHERE id = buyId;
			
			SET success = true;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE sell_stock(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pServerTime BIGINT,
OUT success BOOLEAN,
OUT message TEXT) 

BEGIN 
	DECLARE userStock INTEGER;
	
	SELECT price
	INTO userStock
	FROM stocks
	WHERE userid = pUserId AND stock = pStock;
	
	IF userStock IS NULL THEN
		BEGIN
			SET message = "User does not own that stock";
			SET success = FALSE;
		END;
	ELSEIF userStock < pStockAmount THEN	
		BEGIN
			SET message = "Insufficient amount of stock";
			SET success = FALSE;
		END;
	ELSE	
		BEGIN 
			UPDATE stocks
			SET price = price - pStockAmount
			WHERE userid = pUserId AND stock = pStock;
			
			INSERT INTO transactions (userid, stock, price, transType, transTime)
			VALUES (pUserId, pStock, pStockAmount, 'SELL', pServerTime);
			
			SET success = TRUE;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE sell_commit(
IN pUserId VARCHAR(25),
IN pServerTime BIGINT,
OUT success BOOLEAN,
OUT message TEXT,
OUT pStock VARCHAR(3),
OUT pStockAmount INTEGER)

BEGIN
	DECLARE sellTime BIGINT;
	DECLARE sellId INTEGER;

	SELECT id, stock, price, transTime 
	INTO sellId, pStock, pStockAmount, sellTime
	FROM transactions
    WHERE userid = pUserId AND transType='SELL'
	ORDER BY transTime DESC
	LIMIT 1;
	
	IF sellId IS NULL THEN
		BEGIN
			SET message = "No recent sells";
			SET success = false;
		END;
	ELSEIF pServerTime - sellTime > 60000 THEN
		BEGIN
			SET message = "No recent sells";
			SET success = false;
			
			UPDATE stocks
			SET price = price + pStockAmount
			WHERE userid = pUserId AND stock = pStock;
			
			DELETE FROM transactions
			WHERE id = sellId;
		END;
	ELSE
		BEGIN
			UPDATE user
			SET money = money + pStockAmount
			WHERE userid = pUserId;
			
			DELETE FROM transactions
			WHERE id = sellId;
			
			SET success = true;
			SET message = "";
		END;
	END IF;

END$$

CREATE PROCEDURE sell_cancel_stock(
IN pUserId VARCHAR(25),
IN pServerTime BIGINT,
OUT pStock VARCHAR(3),
OUT pStockAmount INTEGER,
OUT success BOOLEAN,
out message TEXT)

BEGIN	
	DECLARE sellTime BIGINT;
	DECLARE sellId INTEGER;

	SELECT id, stock, price, transTime 
	INTO sellId, pStock, pStockAmount, sellTime
	FROM transactions
    WHERE userid = pUserId AND transType='SELL'
	ORDER BY transTime DESC
	LIMIT 1;
	
	IF sellId IS NULL THEN
		BEGIN
			SET message = "No recent sells";
			SET success = false;
			SET pStock = "";
			SET pStockAmount = 0;
		END;
	ELSEIF pServerTime - sellTime > 60000 THEN
		BEGIN
			SET message = "No recent sells";
			SET success = false;
			
			UPDATE stocks
			SET price = price + pStockAmount
			WHERE userid = pUserId AND stock = pStock;
			
			DELETE FROM transactions
			WHERE id = sellId;
		END;
	ELSE
		BEGIN
			UPDATE stocks
			SET price = price + pStockAmount
			WHERE userid = pUserId AND stock = pStock;
			
			DELETE FROM transactions
			WHERE id = sellId;
			
			SET success = true;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE set_sell_amount(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userStock INTEGER;
	DECLARE userMoney INTEGER;
	
	SELECT money 
	INTO userMoney
	FROM user
	WHERE userid = pUserId;
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
	
	setAmount: BEGIN
		IF userMoney IS NULL THEN
			BEGIN
				SET success = FALSE;
				SET message = "User does not exist";
				LEAVE setAmount;
			END;
		ELSEIF userMoney < pStockAmount THEN 
			BEGIN
				SET success = FALSE;
				SET message = "Insuffiecient amount of stock for this transaction";
				LEAVE setAmount;
			END;
		END IF;
		IF userStock IS NULL THEN
			BEGIN
				UPDATE stocks
				SET price = price - pStockAmount
				WHERE userid = pUserId AND stock = pStock;
				
				INSERT INTO triggers (userid, stock, amount, triggerType)
				VALUES (pUserId, pStock, pStockAmount, 'SELL');
				
				SET success = TRUE;
				SET message = "";
			END;
		ELSE
			BEGIN
				SET message = "User already has a trigger set for this stock";
				SET success = FALSE;
			END;
		END IF;
	END;
END$$

CREATE PROCEDURE set_buy_amount(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pBuyAmount INTEGER,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userStock INTEGER;
	DECLARE userMoney INTEGER;
	
	SELECT money 
	INTO userMoney
	FROM user
	WHERE userid = pUserId;
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
	
	setAmount: BEGIN
		IF userMoney IS NULL THEN
			BEGIN
				SET success = FALSE;
				SET message = "User does not exist";
				LEAVE setAmount;
			END;
		ELSEIF userMoney < pBuyAmount THEN 
			BEGIN
				SET success = FALSE;
				SET message = "User has an insufficient amount of funds for this trigger";
				LEAVE setAmount;
			END;
		END IF;
		IF userStock IS NULL THEN
			BEGIN
				UPDATE user
				SET money = money - pBuyAmount
				WHERE userid = pUserId;
				
				INSERT INTO triggers (userid, stock, amount, triggerType)
				VALUES (pUserId, pStock, pBuyAmount, 'BUY');

				SET success = TRUE;
				SET message = "";
			END;
		ELSE
			BEGIN
				SET message = "User already has a trigger set for this stock";
				SET success = FALSE;
			END;
		END IF;
	END;
END$$

CREATE PROCEDURE cancel_set_sell(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userStock INTEGER;
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
	
	IF userStock IS NULL THEN
		BEGIN
			SET message = "User does not have a trigger set for this stock";
			SET success = FALSE;
		END;
	ELSE
		BEGIN
			UPDATE stocks 
			SET price = price + userStock
			WHERE userid = pUserId AND stock = pStock;
			
			DELETE FROM triggers
			WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
			
			SET success = TRUE;
			SET message = "";
		END;
	END IF;
END$$


CREATE PROCEDURE cancel_set_buy(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
OUT StockAmount INTEGER,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userStock INTEGER;
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
	
	IF userStock IS NULL THEN
		BEGIN
			SET message = "User does not have a trigger set for this stock";
			SET success = FALSE;
		END;
	ELSE
		BEGIN
			UPDATE user 
			SET money = money + userStock
			WHERE userid = pUserId;
			
			DELETE FROM triggers
			WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
			
			SET StockAmount = userStock;
			SET success = TRUE;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE set_trigger_amount(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pTriggerAmount INTEGER,
IN pTriggerType VARCHAR(4),
OUT stockAmount INTEGER,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE tAmount INTEGER;
	DECLARE trigAmount INTEGER;
	
	SELECT amount, triggerAmount
	INTO tAmount, trigAmount
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType;
	
	IF tAmount IS NULL THEN
		BEGIN
			SET stockAmount = 0;
			SET success = FALSE;
			SET message = "User does not have a trigger set for this stock";
		END;
	ELSEIF trigAmount = pTriggerAmount THEN
		BEGIN
			SET stockAmount = 0;
			SET success = FALSE;
			SET message = "Trigger for this stock is already set";
		END;
	ELSE
		BEGIN
			UPDATE triggers
			SET triggerAmount = pTriggerAmount
			WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType;
			
			SET stockAmount = tAmount;
			SET success = TRUE;
			SET message = "";
		END;
	END IF;
END$$

CREATE PROCEDURE sell_trigger(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pStockLeftover INTEGER)

BEGIN 
	UPDATE user 
	SET money = money + pStockAmount
	WHERE userid = pUserId;
	
	UPDATE stock 
	SET price = price + pStockLeftover;
	
	DELETE FROM triggers 
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
END$$

CREATE PROCEDURE buy_trigger(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pMoneyLeftover INTEGER)

BEGIN 
	UPDATE user
	SET money = money + pMoneyLeftover
	WHERE userid = pUserId;
	
	INSERT INTO stocks(userid, stock, price)
	VALUES(pUserId, pStock, pStockAmount)
	ON DUPLICATE KEY UPDATE price = price + pStockAmount;
	
	DELETE FROM triggers 
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
END$$

CREATE PROCEDURE check_trigger_exists(
IN pUserId Varchar(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pTriggerAmount INTEGER,
IN pTriggerType VARCHAR(4),
OUT success BOOLEAN)

BEGIN
	DECLARE ifExists INTEGER;
	
	SELECT EXISTS (
		SELECT 1 
		FROM triggers 
		WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType 
		AND amount = pStockAmount AND triggerAmount = pTriggerAmount;
	) 
	INTO success;
END$$

CREATE EVENT clear_expired_transactions
	ON SCHEDULE
		EVERY 1 MINUTE
	COMMENT 'Clears transactions from table that are expired and returns objects back to users ownership'
	DO
BEGIN
	DELETE FROM transactions WHERE (UNIX_TIMESTAMP() - transTime) < 60;
END$$

DELIMITER ;