using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;


public static class SXLProjectSetup
{
    private static string[] m_SXLProjectTags = new string[] {
        "Concrete",
        "Wood",
        "Metal",
        "SpawnPoint",
        "AutoRevert",
        "AutoPumpAndRevert",
        "Grind_Concrete",
        "Grind_Metal",
        "Surface_Concrete",
        "Surface_Wood",
        "Surface_Brick",
        "Surface_Tarmac",
        "AutoRevert_Concrete",
        "AutoPumpAndRevert_Concrete",
        "Surface_Grass"
    };

    [MenuItem("SkaterXL/Create Project Tags", false, 1)]
    public static void RunSXLSetup()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        
        if (tagManager == null) return;

        AssignTagsToProject(tagManager);
    }

    private static void AssignTagsToProject(SerializedObject manager)
    {
        Debug.Log("Adding Required SkaterXL Tags");

        SerializedProperty tagsProperty = manager.FindProperty("tags");

        for (int i = 0; i < m_SXLProjectTags.Length; ++i)
        {
            tagsProperty.InsertArrayElementAtIndex(i);
            SerializedProperty property = tagsProperty.GetArrayElementAtIndex(i);
            property.stringValue = m_SXLProjectTags[i];
        }
        manager.ApplyModifiedProperties();
    }
}
