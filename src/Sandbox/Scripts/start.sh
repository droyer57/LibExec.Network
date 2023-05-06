#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: $0 <number_of_instances>"
  exit 1
fi

num_instances=$1

function cleanup {
  pkill Sandbox
}

trap cleanup SIGINT SIGTERM

for i in $(seq 1 "$num_instances"); do
  port=$((5030 + i - 1))
  echo "Starting Instance$i..."
  dotnet run -c "Instance$i" --urls "http://localhost:$port" &
done

wait