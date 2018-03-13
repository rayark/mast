#!/bin/bash
 mcs -langversion:4 -sdk:2 -target:library -out:Rayark.Mast.DLL $(find Assets/Plugins/Rayark/Mast -path Assets/Plugins/Rayark/Mast/Editor -prune -o -name "*.cs" -print)