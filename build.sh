#!/bin/bash
set -e

dotnet build -f netstandard2.0 -c Release build/Rayark.Mast.csproj
cp build/bin/Release/netstandard2.0/Rayark.Mast.dll pack/Assets/Plugins/Rayark/Mast


