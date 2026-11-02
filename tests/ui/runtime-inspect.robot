*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Inspect Runtime UI Layers And Menu Objects
    Connect Unium
    Sleep    20
    ${layers}=    Unium Query    scene/Camera_UIRoot/Canvas
    ${layersJson}=    Evaluate    json.dumps($layers, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-canvas.json    ${layersJson}
    ${part}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerPart
    ${partJson}=    Evaluate    json.dumps($part, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-layer-part.json    ${partJson}
    ${back}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerBack
    ${backJson}=    Evaluate    json.dumps($back, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-layer-back.json    ${backJson}
    ${top}=    Unium Query    scene/Camera_UIRoot/Canvas/LayerVeryTop
    ${topJson}=    Evaluate    json.dumps($top, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-layer-top.json    ${topJson}
    ${bottom}=    Unium Query    scene//ViewBottom
    ${bottomJson}=    Evaluate    json.dumps($bottom, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-view-bottom.json    ${bottomJson}
    ${resource}=    Unium Query    scene//ResourceBar
    ${resourceJson}=    Evaluate    json.dumps($resource, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-resource-bar.json    ${resourceJson}
    ${option}=    Unium Query    scene//ViewSystemOption
    ${optionJson}=    Evaluate    json.dumps($option, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/runtime-system-option.json    ${optionJson}
