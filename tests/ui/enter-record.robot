*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Click Real Free Mode Button And Inspect Record Buttons
    Connect Unium
    Sleep    15s
    Unium Invoke    simulate.clickNormalised    0.5195    0.364
    Sleep    5s
    ${record}=    Unium Query    scene//ViewRecordSelect
    ${recordJson}=    Evaluate    json.dumps($record, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/enter-record-view.json    ${recordJson}
    ${buttons}=    Unium Query    scene//*.Button.name
    ${buttonsJson}=    Evaluate    json.dumps($buttons, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/enter-record-buttons.json    ${buttonsJson}
    ${active}=    Unium Query    scene//*.Button.gameObject.activeInHierarchy
    ${activeJson}=    Evaluate    json.dumps($active, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/enter-record-active.json    ${activeJson}
