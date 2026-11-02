*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Wait For Real Main Menu Buttons
    Connect Unium
    Sleep    15s
    ${buttons}=    Unium Query    scene//*.Button.name
    ${json}=    Evaluate    json.dumps($buttons, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/main-menu-buttons.json    ${json}
    ${active}=    Unium Query    scene//*.Button.gameObject.activeInHierarchy
    ${activeJson}=    Evaluate    json.dumps($active, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/main-menu-buttons-active.json    ${activeJson}
    ${interactable}=    Unium Query    scene//*.Button.interactable
    ${interactableJson}=    Evaluate    json.dumps($interactable, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/main-menu-buttons-interactable.json    ${interactableJson}
