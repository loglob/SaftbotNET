#!/usr/bin/bash

while [ true ]
do
sudo dotnet ./Saftbot.NET.dll
echo "a fatal error occured. Saftbot is being restarted. Check logs for more info"
sleep 20
done