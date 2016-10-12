using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;


public class AndroidGradleBuilder {

	private const string outPath = "Build/Gradle";
	private static string projPath = Path.Combine(outPath, PlayerSettings.productName);


	private static List<Action<string>> postProcessExportActionList = new List<Action<string>>();

	public static void AddPostProcessExportAction(Action<string> action) {
		postProcessExportActionList.Add(action);
	}

	private static void postProcessExport() {
		foreach (var action in postProcessExportActionList) {
			action.Invoke(projPath);
		}
	}


	[MenuItem("Build/Gradle Build")]
	public static bool Build() {

		if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) {
			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
		}

		List<string> scenePathList = new List<string>();
		List<EditorBuildSettingsScene> sceneList = EditorBuildSettings.scenes.ToList();
		foreach (EditorBuildSettingsScene scene in sceneList) {
			if (scene.enabled && File.Exists(scene.path)) {
				scenePathList.Add(scene.path);
			}
		}
		if (scenePathList.Count == 0) {
			Debug.LogWarning("build scene is empty");
			return false;
		}
		if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)) {
			Debug.LogWarning("keystorepass is empty");
			return false;
		}
		if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)) {
			Debug.LogWarning("keyaliaspass is empty");
			return false;
		}

		var dirInfo = new DirectoryInfo(Path.Combine(EditorApplication.applicationPath, "../PlaybackEngines/AndroidPlayer/Tools/gradle/lib"));
		var gradleJarPath = dirInfo.GetFiles("gradle-launcher-*.jar").First();

		EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
		EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

		var developmentOption = (EditorUserBuildSettings.development) ? BuildOptions.Development : BuildOptions.None;

		BuildPipeline.BuildPlayer(
			scenePathList.ToArray(),
			outPath,
			BuildTarget.Android,
			BuildOptions.AcceptExternalModificationsToPlayer | developmentOption
		);

		postProcessExport();

		System.Diagnostics.ProcessStartInfo gradlePsi = new System.Diagnostics.ProcessStartInfo();
		gradlePsi.WorkingDirectory = projPath;
		gradlePsi.FileName = "java";
		gradlePsi.Arguments =  string.Format("-jar {0} build signingReport -Pandroid.injected.signing.store.file={1} -Pandroid.injected.signing.store.password={2} -Pandroid.injected.signing.key.alias={3} -Pandroid.injected.signing.key.password={4}",
		                                     gradleJarPath, Path.GetFullPath(PlayerSettings.Android.keystoreName), PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasName, PlayerSettings.keyaliasPass);

		gradlePsi.UseShellExecute = false;
		gradlePsi.RedirectStandardOutput = true;

		var p = System.Diagnostics.Process.Start(gradlePsi);
		p.WaitForExit();

		if (p.ExitCode == 0) {
			Debug.Log("build succeed");
			Debug.Log(p.StandardOutput.ReadToEnd());
			return true;

		} else {
			Debug.LogError("build failed");
			Debug.LogError(p.StandardOutput.ReadToEnd());
			return false;
		}
	}

	[MenuItem("Build/Gradle Build \x8B& Run")]
	public static bool BuildAndRun() {
		if (!Build()) {
			return false;
		}

		var adbPath = Path.Combine(EditorPrefs.GetString("AndroidSdkRoot"), "platform-tools/adb");
		var apkPath = Path.GetFullPath(Path.Combine(projPath, "build/outputs/apk/" + PlayerSettings.productName + "-debug.apk"));
		var installPath = "/data/local/tmp/" + PlayerSettings.bundleIdentifier;

		System.Diagnostics.ProcessStartInfo adbPsi = new System.Diagnostics.ProcessStartInfo();
		adbPsi.FileName = adbPath;
		adbPsi.Arguments = "push " + apkPath + " " + installPath;
		adbPsi.UseShellExecute = false;
		adbPsi.RedirectStandardOutput = true;

		var p = System.Diagnostics.Process.Start(adbPsi);
		p.WaitForExit();

		if (p.ExitCode == 0) {
			Debug.Log("adb push succeed");
			Debug.Log(p.StandardOutput.ReadToEnd());

		} else {
			Debug.LogError("adb push failed");
			Debug.LogError(p.StandardOutput.ReadToEnd());
			return false;
		}

		adbPsi.Arguments = "shell pm install -r \"" + installPath + "\"";

		p = System.Diagnostics.Process.Start(adbPsi);
		p.WaitForExit();

		if (p.ExitCode == 0) {
			Debug.Log("adb install succeed");
			Debug.Log(p.StandardOutput.ReadToEnd());

		} else {
			Debug.LogError("adb install failed");
			Debug.LogError(p.StandardOutput.ReadToEnd());
			return false;
		}

		adbPsi.Arguments = "shell am start -n \"" + PlayerSettings.bundleIdentifier + "/" + PlayerSettings.bundleIdentifier + ".UnityPlayerActivity\" -a android.intent.action.MAIN -c android.intent.category.LAUNCHER";

		p = System.Diagnostics.Process.Start(adbPsi);
		p.WaitForExit();

		if (p.ExitCode == 0) {
			Debug.Log("app start succeed");
			Debug.Log(p.StandardOutput.ReadToEnd());

		} else {
			Debug.LogError("app start failed");
			Debug.LogError(p.StandardOutput.ReadToEnd());
			return false;
		}

		return true;
	}
}
