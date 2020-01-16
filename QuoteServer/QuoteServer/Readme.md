# Building the Quote Server image
docker build -t quoteserver -f Dockerfile .

# Create Quote Server Container
docker create -p 4444:4444 --name quote_server quoteserver

# Start Quote Server
docker start quote_server

# Monitor Quote Server output
docker attach --sig-proxy=false quote_server  
_use `Ctrl-C` to exit_

# Stop Quote Server
docker stop