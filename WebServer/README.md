# Building the Web Server image
docker build -t webserver -f Dockerfile .

# Create Web Server Container
docker create -p 8080:80 --name web_server webserver

# Start Web Server
docker start web_server

# Get Web Server IP Address
docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" web_server

# Monitor Web Server output
docker attach --sig-proxy=false web_server  
_use `Ctrl-C` to exit_

# Stop Quote Server
docker stop