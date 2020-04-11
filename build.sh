#!/bin/bash
set -e

mcs -langversion:7 -sdk:4.7 -target:library -out:build/Assets/Plugins/Rayark/Mast/Rayark.Mast.dll $(find Assets/Plugins/Rayark/Mast -path Assets/Plugins/Rayark/Mast/Editor -prune -o -name "*.cs" -print)
