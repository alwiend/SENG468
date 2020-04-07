# seng468

# Build individual component
Navigate into the component folder and execute
docker build -t <component name> -f Dockerfile ../..
or 
run the build.bat file

# Build entire stack
Run build_all.bat

# Start entire stack
Navigate into Swarm Files 
docker stack deploy -c dev-stack.yaml dev
or
docker stack deploy -c prod-stack.yaml prod

The dev stack runs on a single machine while the prod stack is distributed.
For the prod stack to run the distributed machines must be
connected via a docker swarm.
Each distributed machine must be tagged to identify them.
One of each of the following types must exist within the swarm
web-server
audit-server
database-server
transaction-server

# Stop entire stack
docker stack rm dev
or
docker stack rm prod
