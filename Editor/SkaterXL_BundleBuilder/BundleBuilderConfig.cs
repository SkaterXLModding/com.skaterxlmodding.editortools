using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

public class BundleBuilderConfig : ScriptableObject
{
    public string configPath;
    public AssetBundleConfiguration currentConfig;
}