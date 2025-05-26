@echo off

set "adb=C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"

:: "C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" logcat -s Unity

echo %cd%
echo .
"%adb%" devices

echo Waiting for devices

timeout /t 2 >nul

setlocal enabledelayedexpansion
for /f "skip=1 tokens=1" %%d in ('"%adb%" devices') do (
    set "deviceLine=%%d"
    
    echo !deviceLine!

    timeout /t 1 >nul

    "%adb%" -s !deviceLine! tcpip 5555
    timeout /t 3 /nobreak >nul

    set "IP="

    for /f "tokens=9" %%i in ('"%adb%" -s !deviceLine! shell ip route') do (
        echo Found IP: %%i
        set "IP=%%i"
    )

    if defined IP (
        echo Detected phone IP: !IP!

        echo Connecting
        "%adb%" connect !IP!:5555

        echo Installing
        "%adb%" -s !IP!:5555 install -r "GarPic.apk"

        echo Launching
        "%adb%" -s !IP!:5555 shell monkey -p com.DefaultCompany.com.unity.template.mobile2D -c android.intent.category.LAUNCHER 1

        echo App should be running on Android phone.
    )
)

echo Disconnecting connected devices
"%adb%" disconnect

pause
