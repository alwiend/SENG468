# seng468

# Create custom network to assign static ip to services
docker network create -d nat --subnet=172.1.0.0/16 day_trade_net

# Build individual component
Run build.bat within the project folder

# Build entire stack
Run build_all.bat

# Start entire stack
Run start_all.bat

# Stop entire stack
Run stop_all.bat