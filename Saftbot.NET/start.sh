#!/bin/sh
while :
do
dotnet ./Saftbot.NET.dll
echo "Saftbot crashed, rebooting..."
sleep 20
done
