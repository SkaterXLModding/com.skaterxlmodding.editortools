#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SXLNewMap : EditorWindow
{
    private string m_mapName;
    private bool m_multiSceneMode = false;
    private bool m_includeBlockinScene = false;

    [MenuItem("SkaterXL/New Map Wizard...")]
    public static void ShowWindow()
    {
        var wnd = GetWindow(typeof(SXLNewMap), true, "SkaterXL New Map Wizard");
        wnd.maxSize = wnd.minSize = new Vector2(400, 130);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Label("This tool generates a new single scene or multi-scene map setup for Skater XL.");
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        m_mapName = EditorGUILayout.TextField("Map Name:", m_mapName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        m_multiSceneMode = EditorGUILayout.Toggle("Multi-Scene Setup:", m_multiSceneMode);
        GUILayout.EndHorizontal();

        if (m_multiSceneMode)
        {
            GUILayout.BeginHorizontal();
            m_includeBlockinScene = EditorGUILayout.Toggle("Include Block-in Scene:", m_includeBlockinScene);
            GUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Create New Map"))
        {
            if (string.IsNullOrEmpty(m_mapName))
                EditorUtility.DisplayDialog("New Map Wizard", "The Map Name field is empty. Map Creation Aborted.", "Okay");
            CreateMap(m_mapName);
        }
    }

    Scene CreateScene(string name, bool additive, bool setActive)
    {
        string sceneRoot = name.Split('_')[0];
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
        scene.name = name;
        if (setActive)
            EditorSceneManager.SetActiveScene(scene);

        System.IO.Directory.CreateDirectory($"Assets/Scenes/{sceneRoot}");
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneRoot}/{name}.unity");
        return scene;
    }

    void CreateBundleConfig(string name, List<Scene> sceneList)
    {
        AssetBundleConfiguration config = ScriptableObject.CreateInstance<AssetBundleConfiguration>();
        config.levelName = name;
        config.baseLevel = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneList[0].path);
        List<Scene> subLevels = sceneList.GetRange(1, sceneList.Count - 1);
        config.subLevels = subLevels.Select(x => AssetDatabase.LoadAssetAtPath<SceneAsset>(x.path)).ToArray();

        AssetDatabase.CreateAsset(config, $"Assets/Scenes/{name}/{name}_config.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void AddCoreContent(Scene scene)
    {
        EditorSceneManager.SetActiveScene(scene);
        
        GameObject gameplayRoot = new GameObject("_Gameplay");
        GameObject editorCamera = new GameObject("Editor Camera", typeof(Camera));
        editorCamera.tag = "EditorOnly";
        editorCamera.transform.SetParent(gameplayRoot.transform);

        string guid = AssetDatabase.FindAssets("t:prefab SpawnPoint")[0];
        if (!string.IsNullOrEmpty(guid))
        {
            GameObject spawnPoint = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            spawnPoint.transform.SetParent(gameplayRoot.transform);
            spawnPoint.name = "SpawnPoint";
        }

        GameObject grindablesRoot = new GameObject("_Grindables");
    }

    void AddLightingContent(Scene scene)
    {
        EditorSceneManager.SetActiveScene(scene);

        GameObject lightingRoot = new GameObject("_Lighting");
        // Directional Light
        GameObject directionalLight = new GameObject("Directional Light", typeof(Light));
        directionalLight.transform.SetParent(lightingRoot.transform);
        directionalLight.GetComponent<Light>().type = LightType.Directional;
        // Sky and Fog Volume
        GameObject volumeObject = new GameObject("Sky and Fog Volume", typeof(Volume));
        volumeObject.transform.SetParent(lightingRoot.transform);

        // Create the volume profile and attach it to the volume component
        VolumeProfile volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        volumeProfile.name = $"{scene.name}_profile";

        AssetDatabase.CreateAsset(volumeProfile, $"Assets/Scenes/{scene.name.Split('_')[0]}/{scene.name}_profile.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        volumeObject.GetComponent<Volume>().profile = volumeProfile;
    }

    void AddEnvironmentContent(Scene scene)
    {
        EditorSceneManager.SetActiveScene(scene);
        // Environment Related content?
    }

    void CreateMap(string name)
    {
        List<Scene> sceneList = new List<Scene>();

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        if (m_multiSceneMode)
        {
            Scene sceneCore = CreateScene($"{name}_core", false, true);
            AddCoreContent(sceneCore);
            Scene sceneLighting = CreateScene($"{name}_lighting", true, true);
            AddLightingContent(sceneLighting);
            Scene sceneEnvironment = CreateScene($"{name}_environment", true, true);
            AddEnvironmentContent(sceneEnvironment);

            sceneList.AddRange(new[] {sceneCore, sceneLighting, sceneEnvironment});

            if (m_includeBlockinScene)
            {
                Scene sceneBlockin = CreateScene($"{name}_blockin", true, true);
                sceneList.Add(sceneBlockin);
            }
        }
        else
        {
            Scene scene = CreateScene($"{name}", false, true);
            AddCoreContent(scene);
            AddLightingContent(scene);
            AddEnvironmentContent(scene);
            sceneList.Add(scene);
        }

        EditorSceneManager.SaveScenes(sceneList.ToArray());

        // Create AssetBundleConfig
        CreateBundleConfig(name, sceneList);

        // Set 0th scene to active
        EditorSceneManager.SetActiveScene(EditorSceneManager.GetSceneAt(0));
    }
}
