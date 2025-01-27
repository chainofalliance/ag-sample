BUILD_PATH=$1
VERSION=$2

echo "# Removing previous version..."
ssh dennis@152.53.20.144 "cd client && ls -t | tail -n +2 | xargs rm -rf --"

echo "# Uploading build..."
scp -r $BUILD_PATH/* dennis@152.53.20.144:~/client/$VERSION

echo "# Stopping container..."
ssh dennis@152.53.20.144 "docker rm -f ttt-webgl"

echo "# Delete old images..."
ssh dennis@152.53.20.144 "docker rmi $(docker images 'ttt-webgl')"

echo "# Build image..."
ssh dennis@152.53.20.144 "docker build -t ttt-webgl ~/client/$VERSION"

echo "# Build start container..."
ssh dennis@152.53.20.144 "docker run --name ttt-webgl -p 8081:80 -d ttt-webgl"