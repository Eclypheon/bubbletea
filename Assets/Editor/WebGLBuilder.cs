using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class WebGLBuilder
{
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "docs";

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[WebGLBuilder] Build succeeded: " + summary.totalSize + " bytes");
        }
        else
        {
            Debug.LogError("[WebGLBuilder] Build failed with " + summary.totalErrors + " errors");
            EditorApplication.Exit(1);
        }
    }
}
