@echo off

cd TeleBot

dotnet publish -c Release -r linux-arm

timeout 5