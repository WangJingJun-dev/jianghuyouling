*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Inspect Native NPC Navigation Objects
    Connect Unium
    ${adventure}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerMain/ViewAdventureRemake
    ${json}=    Evaluate    json.dumps($adventure, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/navigation-adventure.json    ${json}
    ${characters}=    Unium Query    scene//EventWindowCharacter
    ${charactersJson}=    Evaluate    json.dumps($characters, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/navigation-characters.json    ${charactersJson}
    ${menu}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerPopUp/ViewCharacterMenu/ViewCharacterMenuStack
    ${menuJson}=    Evaluate    json.dumps($menu, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/navigation-character-menu.json    ${menuJson}
