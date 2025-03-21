#!/usr/bin/env bash

cd /app
export IS_CI=true

echo "# Activating license..."
xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' \
    unity-editor \
    -logFile /dev/stdout \
    -batchmode \
    -nographics \
    -username "$UNITY_USERNAME" -password "$UNITY_PASSWORD" -serial "$UNITY_SERIAL" &> /dev/null

echo ""
make client
EXIT_CODE=$?

echo ""
if [ $EXIT_CODE != 0 ]; then
  echo "# Failed! (Error code: $UNITY_EXIT_CODE)";
  echo ""
fi

echo "# Returning license..."
xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' \
    unity-editor \
    -logFile /dev/stdout \
    -batchmode \
    -nographics \
    -returnlicense \
    -username "$UNITY_USERNAME" -password "$UNITY_PASSWORD" &> /dev/null
    
if [ $EXIT_CODE -eq 0 ]; then
  echo "# Done!"
else
  exit 1
fi