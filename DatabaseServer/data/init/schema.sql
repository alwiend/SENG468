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
		FROM stocks;
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
		SET message = "No recent buys";
		SET success = false;
		SET stockBuy = "";
		SET stockAmount = 0;
	ELSEIF pServerTime - buyTime > 60000 THEN
		SET message = "No recent buys";
		SET success = false;
		SET stockBuy = "";
		SET stockAmount = 0;
		
		DELETE FROM transactions
		WHERE id = buyId;
	ELSE
		INSERT INTO stocks (userid, stock, price) 
		VALUES (pUserId, stockBuy, stockAmount)
		ON DUPLICATE KEY UPDATE price = price + stockAmount;
		
		DELETE FROM transactions
		WHERE id = buyId;
		
		SET success = true;
		SET message = "";
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
		SET message = "User does not own that stock";
		SET success = FALSE;
	ELSEIF userStock < pStockAmount THEN	
		SET message = "Insufficient amount of stock";
		SET success = FALSE;
	ELSE	
		BEGIN 
			INSERT INTO transactions (userid, stock, price, transType, transTime)
			VALUES (pUserId, pStock, pStockAmount, 'SELL', pServerTime);
			
			UPDATE stocks
			SET price = price - pStockAmount
			WHERE userid = pUserId AND stock = PStock;
			
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
		SET message = "No recent sells";
		SET success = false;
	ELSEIF pServerTime - sellTime > 60000 THEN
		SET message = "No recent sells";
		SET success = false;
		
		DELETE FROM transactions
		WHERE id = sellId;
	ELSE
		INSERT INTO stocks (userid, stock, price) 
		VALUES (pUserId, pStock, pStockAmount)
		ON DUPLICATE KEY UPDATE price = price + pStockAmount;
		
		DELETE FROM transactions
		WHERE id = sellId;
		
		SET success = true;
		SET message = "";
	END IF;

END$$

CREATE PROCEDURE sell_cancel_stock(
IN pUserId VARCHAR(25),
OUT pStock VARCHAR(3),
OUT pStockAmount INTEGER,
OUT pServerTime BIGINT,
OUT success BOOLEAN,
out message TEXT)

BEGIN	
	DECLARE done INT DEFAULT FALSE;
	DECLARE S, tempS VARCHAR(3);
	DECLARE SA, tempSA INTEGER;
	DECLARE ST, tempST TEXT;
	DECLARE minTime VARCHAR(10);

	DECLARE t CURSOR FOR
	SELECT stock, price, transTime
	FROM transactions
	WHERE userid = pUserId AND transType = 'SELL';
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
	SET minTime = 60;
	
	OPEN t;
	
	trans_loop: LOOP
		FETCH t INTO tempS, tempSA, tempST;
		IF done THEN
			LEAVE trans_loop;
		END IF;
		IF (UNIX_TIMESTAMP() - tempST) < minTime THEN
			SET S = tempS;
			SET SA = tempSA;
			SET ST = tempST;
			SET minTIme = UNIX_TIMESTAMP() - tempST;
		END IF;
	END LOOP;
	CLOSE t;
	IF SA IS NULL THEN
		SET success = FALSE;
		SET message = "User has no recent sell transactions to cancel";
	ELSE
		UPDATE stocks 
		SET price = price + SA
		WHERE userid = pUserId AND stock = S;
		
		SET @TRIGGER_AFTER_DELETE_ENABLED = FALSE;
		DELETE FROM transactions
		WHERE userid = pUserId AND stock = S AND price = SA AND transType = 'SELL' AND transTime = ST;
	
		SET success = TRUE;
		SET message = "";
		SET pStock = S;
		SET pStockAmount = SA;
		SET pServerTime = ST;
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
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
	
	SELECT money 
	INTO userMoney
	FROM user
	WHERE userid = pUserId;
	setAmount: BEGIN
		IF userMoney IS NULL THEN
			SET success = FALSE;
			SET message = "User does not exist";
			LEAVE setAmount;
		ELSEIF userMoney < pStockAmount THEN 
			SET success = FALSE;
			SET message = "Insuffiecient amount of stock for this transaction";
			LEAVE setAmount;
		END IF;
		IF userStock IS NULL THEN
			INSERT INTO triggers (userid, stock, amount, triggerType)
			VALUES (pUserId, pStock, pStockAmount, 'SELL');

			UPDATE stocks
			SET price = price - pStockAmount
			WHERE userid = pUserId AND stock = pStock;
			
			SET success = TRUE;
			SET message = "";
		ELSE
			SET message = "User already has a trigger set for this stock";
			SET success = FALSE;
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
	
	SELECT amount
	INTO userStock
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
	
	SELECT money 
	INTO userMoney
	FROM user
	WHERE userid = pUserId;
	setAmount: BEGIN
		IF userMoney IS NULL THEN
			SET success = FALSE;
			SET message = "User does not exist";
			LEAVE setAmount;
		ELSEIF userMoney < pBuyAmount THEN 
			SET success = FALSE;
			SET message = "User has an insufficient amount of funds for this trigger";
			LEAVE setAmount;
		END IF;
		IF userStock IS NULL THEN
			INSERT INTO triggers (userid, stock, amount, triggerType)
			VALUES (pUserId, pStock, pBuyAmount, 'BUY');

			UPDATE user
			SET money = money - pBuyAmount
			WHERE userid = pUserId;
			
			SET success = TRUE;
			SET message = "";
		ELSE
			SET message = "User already has a trigger set for this stock";
			SET success = FALSE;
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
		SET message = "User does not have a trigger set for this stock";
		SET success = FALSE;
	ELSE
		UPDATE stocks 
		SET price = price + userStock
		WHERE userid = pUserId AND stock = pStock;
		
		DELETE FROM triggers
		WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
		
		SET success = TRUE;
		SET message = "";
	END IF;
END$$


CREATE PROCEDURE cancel_set_buy(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
OUT StockAmount INTEGER,
OUT success BOOLEAN,
OUT message TEXT)

BEGIN
	DECLARE userTrigger INTEGER;
	
	SELECT amount
	INTO userTrigger
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
	
	IF userTrigger IS NULL THEN
		SET message = "User does not have a trigger set for this stock";
		SET success = FALSE;
	ELSE
		UPDATE user 
		SET money = money + userTrigger
		WHERE userid = pUserId;
		
		DELETE FROM triggers
		WHERE userid = pUserId AND stock = pStock AND triggerType = 'BUY';
		
		SET StockAmount = userTrigger;
		SET success = TRUE;
		SET message = "";
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
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType
	INTO tAmount, trigAmount;
	
	IF tAmount IS NULL THEN
		SET stockAmount = 0;
		SET success = FALSE;
		SET message = "User does not have a trigger set for this stock";
	ELSEIF trigAmount = pTriggerAmount THEN
		SET stockAmount = 0;
		SET success = FALSE;
		SET message = "Trigger for this stock is already set";
	ELSE
		UPDATE triggers
		SET triggerAmount = pTriggerAmount
		WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType;
		
		SET stockAmount = tAmount;
		SET success = TRUE;
		SET message = "";
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
	
	INSERT INTO stocks(userid, stock, price) 
	VALUES(pUserId, pStock, pStockLeftover)
	ON DUPLICATE KEY UPDATE price = price + pStockLeftover;
	
	DELETE FROM triggers 
	WHERE userid = pUserId AND stock = pStock AND triggerType = 'SELL';
END$$

CREATE PROCEDURE buy_trigger(
IN pUserId VARCHAR(25),
IN pStock VARCHAR(3),
IN pStockAmount INTEGER,
IN pMoneyLeftover INTEGER)

BEGIN 
	INSERT INTO stocks(userid, stock, price)
	VALUES(pUserId, pStock, pStockAmount)
	ON DUPLICATE KEY UPDATE price = price + pStockAmount;
	
	UPDATE user
	SET money = money + pMoneyLeftover
	WHERE userid = pUserId;
	
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
	
	SELECT 1
	INTO ifExists
	FROM triggers
	WHERE userid = pUserId AND stock = pStock AND triggerType = pTriggerType 
	AND amount = pStockAmount AND triggerAmount = pTriggerAmount;
	
	IF ifExists IS NULL THEN
		SET success = FALSE;
	ELSE 
		SET success = TRUE;
	END IF;
END$$

CREATE EVENT clear_expired_transactions
	ON SCHEDULE
		EVERY 1 MINUTE
	COMMENT 'Clears transactions from table that are expired and returns objects back to users ownership'
	DO
	BEGIN
		SET @TRIGGER_AFTER_DELETE_ENABLED = TRUE;
		DELETE FROM transactions WHERE (UNIX_TIMESTAMP() - transTime) < 60;
	END$$
	
CREATE TRIGGER transactions_after_delete
	AFTER DELETE
	ON transactions FOR EACH ROW
	trig: BEGIN
	IF (@TRIGGER_AFTER_DELETE_ENABLED = FALSE) THEN
		LEAVE trig;
	END IF;
	IF (OLD.transType = 'SELL') THEN
		INSERT INTO stocks(userid, stock, price)
		VALUES(OLD.userid, OLD.stock, OLD.price)
		ON DUPLICATE KEY UPDATE price = price + OLD.price;
	ELSEIF (OLD.transType = 'BUY') THEN
		INSERT INTO user(userid, money)
		VALUES(OLD.userid, OLD.price)
		ON DUPLICATE KEY UPDATE money = money + OLD.price;
	END IF;
END$$

DELIMITER ;