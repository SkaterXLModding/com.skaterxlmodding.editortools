using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "BundleConfiguration", menuName = "SkaterXL/Create Asset Bundle Configuration")]
public class AssetBundleConfiguration : ScriptableObject
{
    public string levelName;
    public SceneAsset baseLevel;
    public SceneAsset[] subLevels;
}


