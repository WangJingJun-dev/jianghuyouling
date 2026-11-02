*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Locate Main Menu And Record Selection
    Connect Unium
    ${main}=    Unium Query    scene//ViewMainMenu
    ${mainJson}=    Evaluate    json.dumps($main, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/main-menu-view.json    ${mainJson}
    ${record}=    Unium Query    scene//ViewRecordSelect
    ${recordJson}=    Evaluate    json.dumps($record, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/record-select-view.json    ${recordJson}
    ${start}=    Unium Query    scene//ViewStart
    ${startJson}=    Evaluate    json.dumps($start, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/start-view.json    ${startJson}
    ${home}=    Unium Query    scene//ViewHome
    ${homeJson}=    Evaluate    json.dumps($home, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/home-view.json    ${homeJson}
    ${logo}=    Unium Query    scene//ViewLogoShow
    ${logoJson}=    Evaluate    json.dumps($logo, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/logo-view.json    ${logoJson}
    ${generic}=    Unium Query    scene//MainMenu
    ${genericJson}=    Evaluate    json.dumps($generic, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/generic-main-menu.json    ${genericJson}
