#!/bin/bash

output="$(git pull --ff-only 2>> updater.log)"

if [ "$?" != "0" ]; then
  echo "Return value was evil" 2>> updater.log
  exit 1
fi

if [ "$output" == "Already up-to-date." ]; then
  echo "Already up-to-date." 2>&1 >> updater.log
  exit 1
fi

gmcs -debug -out:VInt.exe ../VInt/Program.cs 2>&1 >>updater.log

echo "READY"

read -r steve

mono VInt.exe &
