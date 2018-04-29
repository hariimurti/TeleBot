#!/bin/bash

app_name=TeleBot
app_desc="Telegram Bot"

# Make sure only root can run our script
if [[ $EUID -ne 0 ]]; then
  echo "This script must be run as root" 1>&2
  exit 1
fi

# Do not change this
app_dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
service_path=/etc/systemd/system/$app_name.service

# Disable & Remove service
if [[ $1 == "-uninstall" ]]; then
  sudo systemctl stop $app_name
  sudo systemctl disable $app_name
  sudo rm -f $service_path
  exit
fi

# Create service
echo "[Unit]" > $service_path
echo "Description=$app_desc" >> $service_path
echo "Documentation=man:$app_name" >> $service_path
echo "After=network-online.target" >> $service_path
echo "[Service]" >> $service_path
echo "Type=simple" >> $service_path
echo "User=pi" >> $service_path
echo "Group=pi" >> $service_path
echo "UMask=007" >> $service_path
echo "ExecStart=$app_dir/$app_name" >> $service_path
echo "Restart=on-failure" >> $service_path
echo "TimeoutStopSec=300" >> $service_path
echo "[Install]" >> $service_path
echo "WantedBy=multi-user.target" >> $service_path

# Enable & Start service
sudo systemctl daemon-reload
sudo systemctl enable $app_name
sudo systemctl start $app_name
