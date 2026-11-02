*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Open System Settings And Inspect AI Controls
    Connect Unium
    Wait Until Keyword Succeeds    90s    2s    Unium Object Should Exist    scene//ViewResourceBar
    ${bar}=    Unium Query    scene//ViewResourceBar
    ${menuName}=    Evaluate    next(c["name"] for c in $bar[0]["children"] if "menu" in c["name"].lower())
    Unium Click    scene//ViewResourceBar/${menuName}
    Sleep    1
    ${option}=    Unium Query    scene//ViewSystemOption
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-system-option.json    ${option}
    ${settingName}=    Evaluate    next(c["name"] for c in $option[0]["children"] if "systemsetting" in c["name"].lower())
    Unium Click    scene//ViewSystemOption/${settingName}
    Sleep    2
    ${settings}=    Unium Query    scene//ViewSystemSetting
    ${settingsJson}=    Evaluate    json.dumps($settings, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-window.json    ${settingsJson}
    ${toggle}=    Unium Query    scene//JHYL_AiSettingsToggle
    ${toggleJson}=    Evaluate    json.dumps($toggle, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-ai-toggle.json    ${toggleJson}
    Unium Click    scene//JHYL_AiSettingsToggle
    Sleep    1
    Unium Click    scene//Toggle_1005
    Sleep    1
    ${input}=    Unium Query    scene//Group_1005//JHYL_TextInput
    ${inputJson}=    Evaluate    json.dumps($input, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-multiline-input.json    ${inputJson}
    ${interactable}=    Unium Property    scene//Group_1005//JHYL_TextInput/TMP_InputField.interactable
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-multiline-interactable.json    ${interactable}
    ${rect}=    Unium Property    scene//Group_1005//JHYL_TextInput/RectTransform.rect
    ${rectJson}=    Evaluate    json.dumps($rect, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/settings-multiline-rect.json    ${rectJson}
