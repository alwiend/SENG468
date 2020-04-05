START .\TransactionMicroservices\AddService\build.bat %1
START .\TransactionMicroservices\BuyService\build.bat %1
START .\TransactionMicroservices\BuyTriggerService\build.bat %1
START .\TransactionMicroservices\DisplaySummaryService\build.bat %1
START .\TransactionMicroservices\QuoteService\build.bat %1
START .\TransactionMicroservices\SellService\build.bat %1
START .\TransactionMicroservices\SellTriggerService\build.bat %1

START .\AuditServer\build.bat %1
START .\WebServer\build.bat %1
START .\QuoteServer\QuoteServer\build.bat %1
START .\WorkloadGenerator\build.bat %1

PAUSE