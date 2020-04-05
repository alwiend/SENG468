@ECHO OFF

SET tag=latest

IF NOT "%~1"=="" SET tag=%1

START docker push bberthelet/add_service_image:%tag%
START docker push bberthelet/buy_service_image:%tag%
START docker push bberthelet/buy_trigger_service_image:%tag%
START docker push bberthelet/display_summary_service_image:%tag%
START docker push bberthelet/quote_service_image:%tag%
START docker push bberthelet/sell_service_image:%tag%
START docker push bberthelet/sell_trigger_service_image:%tag%

START docker push bberthelet/audit_server_image:%tag%
START docker push bberthelet/web_server_image:%tag%

START docker push bberthelet/quote_server_image:latest
START docker push bberthelet/workload_generator_image:latest

PAUSE