# Building the Web Server image
docker build -t web_server_image -f Dockerfile .

# Create Web Server Container
docker create --net day_trade_net --ip 172.1.0.9 --name web_server web_server_image

# Start Web Server
docker start web_server

# Get Web Server IP Address
docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" web_server

# Monitor Web Server output
docker attach --sig-proxy=false web_server  
_use `Ctrl-C` to exit_

# Stop Quote Server
docker stop