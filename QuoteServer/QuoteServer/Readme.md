# Building the Quote Server image
docker build -t quote_server_image -f Dockerfile .

# Create custom network to assign static ip (Only if it does not already exist)
docker network create -d nat --subnet=172.1.0.0/16 day_trade_net

# Create Quote Server Container
docker create --net day_trade_net --ip 172.1.0.10 --name quote_server quote_server_image

# Start Quote Server
docker start quote_server

# Monitor Quote Server output
docker attach --sig-proxy=false quote_server  
_use `Ctrl-C` to exit_

# Stop Quote Server
docker stop