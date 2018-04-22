@echo off
cd TeleBot
dotnet publish -c Release -r linux-arm -o bin/Raspbian
timeout 5