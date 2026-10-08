using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildHelper
{
    public static void Check()
    {
        bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        Debug.Log("=== HEADLESS_CHECK: WebGL Supported = " + supported + " ===");
    }

    public static void BuildWebGL()
    {
        Debug.Log("=== SWITCHING BUILD TARGET TO WEBGL ===");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "buildv1.7.1";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log("=== STARTING HEADLESS WEBGL BUILD TO " + buildPath + " ===");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("=== WEBGL BUILD SUCCEEDED: " + summary.totalSize + " bytes in " + summary.totalTime.TotalSeconds + "s ===");
        }
        else
        {
            Debug.LogError("=== WEBGL BUILD FAILED: " + summary.totalErrors + " errors, result: " + summary.result + " ===");
            EditorApplication.Exit(1);
        }
    }
}

