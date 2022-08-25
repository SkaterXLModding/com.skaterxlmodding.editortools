using System;

using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;


abstract class IBundleBuilderPreprocess : IProcessSceneWithReport
{
    public int callbackOrder { get {return 0; } }

    public abstract void ProcessScene();

    public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
    {
        if (!SXLBundleBuilder.RunPreprocess) return;
        
        ProcessScene();
    }

}