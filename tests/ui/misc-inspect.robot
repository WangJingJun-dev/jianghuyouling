*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Inspect Misc Interaction Objects
    Connect Unium
    ${eventWindow}=    Unium Query    scene//ViewEventWindow
    ${eventJson}=    Evaluate    json.dumps($eventWindow, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/misc-event-window.json    ${eventJson}
    ${toggle}=    Unium Query    scene//JHYL_MiscInteractionToggle
    ${toggleJson}=    Evaluate    json.dumps($toggle, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/misc-toggle.json    ${toggleJson}
    ${content}=    Unium Query    scene//JHYL_MiscInteractionContent
    ${contentJson}=    Evaluate    json.dumps($content, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/misc-content.json    ${contentJson}
    ${character}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerPopUp/ViewCharacterMenu
    ${characterJson}=    Evaluate    json.dumps($character, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/misc-character-menu.json    ${characterJson}
