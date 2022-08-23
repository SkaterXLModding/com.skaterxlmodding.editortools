using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SXLBundleBuilder : EditorWindow
{
    private const string m_BuilderConfigPath = "Assets/Editor/SkaterXL_BundleBuilder/bundleBuilderConfiguration.asset";

    SerializedObject m_SerializedBuilderConfig;
    SerializedProperty m_ConfigPath;
    SerializedProperty m_CurrentConfig;

    SerializedObject m_SerializedBundleObject;
    SerializedProperty m_baseLevel;
    SerializedProperty m_subLevels;

    private BundleBuilderConfig m_BundleBuilderConfig;
    private bool openOnComplete = false;
    private bool generateLighting = false;


    [MenuItem("SkaterXL/Asset Bundles/Asset Bundle Builder...")]
    public static void ShowWindow()
    {
        var wnd = GetWindow(typeof(SXLBundleBuilder), true, "Asset Bundle Builder");
        wnd.maxSize = wnd.minSize = new Vector2(600, 320);
    }

    void OnEnable()
    {
        m_BundleBuilderConfig = (BundleBuilderConfig)AssetDatabase.LoadAssetAtPath(m_BuilderConfigPath, typeof(BundleBuilderConfig));

        if (!m_BundleBuilderConfig)
        {
            m_BundleBuilderConfig = ScriptableObject.CreateInstance<BundleBuilderConfig>();
            AssetDatabase.CreateAsset(m_BundleBuilderConfig, m_BuilderConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        m_SerializedBuilderConfig = new SerializedObject(m_BundleBuilderConfig);
        m_ConfigPath = m_SerializedBuilderConfig.FindProperty("configPath");
        m_CurrentConfig = m_SerializedBuilderConfig.FindProperty("currentConfig");
    }

    void InitializeBundleConfig(AssetBundleConfiguration configuration)
    {
        m_SerializedBundleObject = new SerializedObject(configuration);
        m_baseLevel = m_SerializedBundleObject.FindProperty("baseLevel");
        m_subLevels = m_SerializedBundleObject.FindProperty("subLevels");
    }

    void SaveContent()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); 
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Label("This tool combines all scenes specified into a single Scene Asset Bundle for SkaterXL.\nIf there are no sub levels, the base level will be used alone.");
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        m_CurrentConfig.objectReferenceValue = EditorGUILayout.ObjectField(m_CurrentConfig.objectReferenceValue, typeof(AssetBundleConfiguration), true) as AssetBundleConfiguration;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.Label("Scene Options:", EditorStyles.boldLabel);

        if (m_CurrentConfig.objectReferenceValue)
        {
            InitializeBundleConfig(m_CurrentConfig.objectReferenceValue as AssetBundleConfiguration);
            if (m_SerializedBuilderConfig.ApplyModifiedProperties())
            {
                m_ConfigPath.stringValue = AssetDatabase.GetAssetPath(m_BundleBuilderConfig);
                EditorUtility.SetDirty(m_BundleBuilderConfig);
                SaveContent();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.PropertyField(m_baseLevel, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.PropertyField(m_subLevels, true);
            GUILayout.EndHorizontal();

            if (m_SerializedBundleObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(m_CurrentConfig.objectReferenceValue);
                SaveContent();  
            }     

            GUILayout.Space(24);

            GUILayout.Label("Build Options:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            generateLighting = EditorGUILayout.Toggle("Generate Lighting", generateLighting);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            openOnComplete = EditorGUILayout.Toggle("Open On Completion", openOnComplete);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (GUILayout.Button("Generate AssetBundle"))
            {
                // Do generation steps
                AssetBundleConfiguration config = m_CurrentConfig.objectReferenceValue as AssetBundleConfiguration;
                if (!config.baseLevel) return;
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                CombineScenesAndBuildBundle(config);
            }
        }
        GUILayout.EndVertical();
    }

    private void CombineScenesAndBuildBundle(AssetBundleConfiguration config)
    {
        List<Scene> toMerge = new List<Scene>();
        // Load base scene
        string coreScenePath = AssetDatabase.GetAssetPath(config.baseLevel);
        Debug.Log(coreScenePath);
        Scene baseLevel = EditorSceneManager.OpenScene(coreScenePath, OpenSceneMode.Single);

        foreach (SceneAsset scene in config.subLevels)
        {
            string subLevelPath = AssetDatabase.GetAssetPath(scene);
            Scene subLevel = EditorSceneManager.OpenScene(subLevelPath, OpenSceneMode.Additive);
            EditorSceneManager.MergeScenes(subLevel, baseLevel);
        }

        string[] substring = Application.dataPath.Split('/');
        string projectName = config.levelName;
        if (string.IsNullOrEmpty(projectName)) return;
        
        string newPath = $"{Path.GetDirectoryName(baseLevel.path)}/{projectName}.unity";
        Debug.Log(newPath);

        if (generateLighting)
        {
            Lightmapping.Bake();
        }

        EditorSceneManager.SaveScene(baseLevel, newPath);

        // Create the bundle name for the scene file
        AssetImporter.GetAtPath(newPath).SetAssetBundleNameAndVariant(projectName, "");
        BuildAssetBundle(config);

        if (openOnComplete)
        {
            EditorSceneManager.OpenScene(newPath, OpenSceneMode.Single);
        }
        else
        {
            EditorSceneManager.OpenScene(coreScenePath, OpenSceneMode.Single);
            foreach (SceneAsset scene in config.subLevels)
            {
                string subLevelPath = AssetDatabase.GetAssetPath(scene);
                EditorSceneManager.OpenScene(subLevelPath, OpenSceneMode.Additive);
            }
        }
        EditorWindow view = EditorWindow.GetWindow<SceneView>();
        view.Repaint();
    }

    private void BuildAssetBundle(AssetBundleConfiguration config)
    {
        string bundleDirectory = "Assets/AssetBundles";
        if (!Directory.Exists(bundleDirectory))
        {
            Directory.CreateDirectory(bundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(bundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        foreach(string name in AssetDatabase.GetAllAssetBundleNames())
        {
            if (name == config.levelName.ToLower())
            {
                string docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                File.Copy($"{Application.dataPath}\\AssetBundles\\{name}", $"{docFolder}\\SkaterXL\\Maps\\{name}", true);
            }
        }
    }
}
