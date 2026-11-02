*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    Process

*** Test Cases ***
Capture Main Menu Before Entering World
    Connect Unium
    Run Process    curl.exe    --silent    --show-error    --http1.0    --max-time    15    --output    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/main-menu.png    http://127.0.0.1:8342/utils/screenshot
