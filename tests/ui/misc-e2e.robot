*** Settings ***
Library    unity_game_automation.library.UniumLibrary
Library    OperatingSystem

*** Test Cases ***
Enter World And Inspect Misc Interaction In One Session
    Connect Unium
    ${record}=    Unium Query    scene//ViewRecordSelect
    ${recordJson}=    Evaluate    json.dumps($record, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-record-select.json    ${recordJson}
    Run Keyword And Ignore Error    Unium Invoke    scene//ViewRecordSelect.OnRecordEnterGame()
    Run Keyword And Ignore Error    Unium Invoke    scene//ViewRecordSelect.OnEvent_RecordEnterGame()
    Sleep    15s
    ${eventWindow}=    Unium Query    scene//ViewEventWindow
    ${eventJson}=    Evaluate    json.dumps($eventWindow, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-event-window.json    ${eventJson}
    ${toggle}=    Unium Query    scene//JHYL_MiscInteractionToggle
    ${toggleJson}=    Evaluate    json.dumps($toggle, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-misc-toggle-before.json    ${toggleJson}
    Run Keyword And Ignore Error    Unium Click    scene//JHYL_MiscInteractionToggle
    Sleep    2s
    ${content}=    Unium Query    scene//JHYL_MiscInteractionContent
    ${contentJson}=    Evaluate    json.dumps($content, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-misc-content-after.json    ${contentJson}
    ${active}=    Unium Query    scene//JHYL_MiscInteractionContent.activeInHierarchy
    ${activeJson}=    Evaluate    json.dumps($active, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-misc-active-after.json    ${activeJson}
    ${rect}=    Unium Query    scene//JHYL_MiscInteractionContent.RectTransform
    ${rectJson}=    Evaluate    json.dumps($rect, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-misc-rect-after.json    ${rectJson}
    ${groups}=    Unium Invoke    scene//CToggleGroup.GetActiveIndex()
    ${groupsJson}=    Evaluate    json.dumps($groups, ensure_ascii=False)    json
    Create File    E:/Work/VibeCoding/TaiWuDev/UnityGameAutomation/.e2e-results/e2e-toggle-groups.json    ${groupsJson}
