@ECHO off
ECHO TeleBot Publisher
ECHO.

REM Connection information:
SET Server=192.168.1.21
SET Username=pi
SET Password=ok
SET ProjectDir=TeleBot
SET BuildDir=bin\Raspbian
SET UploadTo=/opt/TeleBot

TITLE Build project...
ECHO Build project...
CD %ProjectDir%
dotnet publish -c Release -r linux-arm -o %BuildDir%

ECHO.
SET /p upload="Upload into Raspbian? [y/N] : "
IF NOT "%upload%" == "y" (
	GOTO exit
)
SET /p update="Update all library? [y/N] : "

TITLE Stop bot service...
CD ..
ECHO Prepare SSH...
SET SSHCommands="ssh_commands.txt"

ECHO.
ECHO SSH: Stoping TeleBot Service...
ECHO sudo systemctl stop telebot> %SSHCommands%
putty.exe -ssh %Username%@%Server% -pw %Password% -m %SSHCommands%

TITLE Uploading...
ECHO.
ECHO Prepare FTP...
SET FTPCommands="ftp_commands.txt"
ECHO open %Server%> %FTPCommands%
ECHO %Username%>> %FTPCommands%
ECHO %Password%>> %FTPCommands%
ECHO binary>> %FTPCommands%
ECHO cd %UploadTo%>> %FTPCommands%
ECHO prompt>> %FTPCommands%

IF "%update%" == "y" (
	ECHO mdel *>> %FTPCommands%
    ECHO mput "%~dp0%ProjectDir%\%BuildDir%\*">> %FTPCommands%
) ELSE (
    ECHO mdel TeleBot*>> %FTPCommands%
    ECHO mput "%~dp0%ProjectDir%\%BuildDir%\TeleBot*">> %FTPCommands%
)
ECHO quit>> %FTPCommands%

ECHO.
ECHO Upload via FTP...
ftp -s:%FTPCommands%

TITLE Start bot service...
ECHO.
ECHO SSH: Starting TeleBot Service...
ECHO chmod +x %UploadTo%/TeleBot> %SSHCommands%
ECHO sudo systemctl start telebot>> %SSHCommands%
putty.exe -ssh %Username%@%Server% -pw %Password% -m %SSHCommands%

ECHO.
ECHO Clean up...
IF EXIST %SSHCommands% DEL %SSHCommands%
IF EXIST %FTPCommands% DEL %FTPCommands%

TITLE Finish!
ECHO.
TIMEOUT 10