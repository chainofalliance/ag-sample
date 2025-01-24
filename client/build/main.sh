#!/usr/bin/env bash
set -eu

{
if [[ -v IS_CI ]]; then
  UNITY_EXECUTABLE=unity-editor
fi

START_TIME=$(date "+%s")

BASE_WORKDIR=$(pwd)

export SCRIPTING_BACKEND=IL2CPP
export BUILD_TARGET=WebGL
export SUB_BUILD_TARGET=Player
export BUILD_NAME=TicTacToe
export BUILD_PATH=$(pwd)/Builds/$BUILD_TARGET/
export VERSION_NUMBER_VAR=$VERSION
mkdir -p $BUILD_PATH

LOG_FILE=$BUILD_PATH/build.log

echo "########################################"
echo "# Building ${BUILD_NAME}..."
echo "# Target:  $BUILD_TARGET"
echo "# Path:    $BUILD_PATH"
echo "# Version: $VERSION"
echo "# Logfile: $LOG_FILE"

"${UNITY_EXECUTABLE}" \
  -quit \
  -batchmode \
  -nographics \
  -projectPath ./TicTacToe \
  -buildTarget $BUILD_TARGET \
  -standaloneBuildSubtarget $SUB_BUILD_TARGET \
  -customBuildTarget $BUILD_TARGET \
  -customBuildName ${BUILD_NAME} \
  -customBuildPath $BUILD_PATH \
  -executeMethod EditorCommands.PerformCIBuild \
  -logFile $LOG_FILE &

UNITY_PID=$!

echo -n "# "
while kill -0 $UNITY_PID &> /dev/null; do
    echo -n '.'
    sleep 2
done

wait $UNITY_PID
UNITY_EXIT_CODE=$?

echo ""
echo "#"

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "# Success!";
  echo "# You can now start another build."
else
  echo "# Failed! (Error code: $UNITY_EXIT_CODE)";
  exit 1
fi

echo "# "

$(pwd)/build/upload.sh

echo "#"

END_TIME=$(date "+%s")
ELAPSED_TIME=$((END_TIME-START_TIME))
ELAPSED_TIME_STR=$(date -d@${ELAPSED_TIME} -u +%H:%M:%S)
echo "# Total deploy time: ${ELAPSED_TIME_STR}"
echo "########################################"
} 2>&1 | tee $(pwd)/build/build.log