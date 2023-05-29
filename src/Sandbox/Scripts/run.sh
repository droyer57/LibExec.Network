#!/bin/bash

function cleanup {
  pkill Sandbox
}

trap cleanup SIGINT SIGTERM

dotnet /usr/lib/dotnet/sdk/7.0.105/MSBuild.dll ../../../

for arg in "$@"; do
  ./Sandbox "$arg" &
done

wait