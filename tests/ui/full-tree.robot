*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Capture Full Native Scene Tree Once
    Connect Unium
    ${tree}=    Unium Query    scene//*
    ${json}=    Evaluate    json.dumps($tree, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/full-scene-tree.json    ${json}
