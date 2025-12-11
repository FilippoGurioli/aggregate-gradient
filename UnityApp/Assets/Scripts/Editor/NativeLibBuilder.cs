#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Editor
{

    public static class NativeLibBuilder
    {
        private const string KotlinProjectPath = "../collektive-lib";
        private const string GradleExecutable = "../collektive-lib/gradlew";
        private const string GradleTask = "linkReleaseSharedLinuxX64";

        private const string SourceSoPath =
            "../collektive-lib/lib/build/bin/linuxX64/releaseShared/libsimple_gradient.so";

        private const string UnitySoPath = "Assets/Plugins/Linux/libsimple_gradient.so";

        [MenuItem("Tools/Native/Rebuild libsimple_gradient")]
        public static void RebuildNativeLibrary()
        {
            try
            {
                if (!RunGradleBuild())
                {
                    Debug.LogError("NativeLibBuilder: Gradle/Kotlin build failed. Aborting.");
                    return;
                }
                CopySoIntoUnity();
                ConfigurePluginImporter();
                Debug.Log("NativeLibBuilder: Native library rebuilt and configured successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NativeLibBuilder: Exception while rebuilding native library: {ex}");
            }
        }

        private static bool RunGradleBuild()
        {
            var workingDir = Path.GetFullPath(KotlinProjectPath);
            if (!Directory.Exists(workingDir))
            {
                Debug.LogError($"NativeLibBuilder: Kotlin project directory not found: {workingDir}");
                return false;
            }
            var gradlePath = GradleExecutable;
            var startInfo = new ProcessStartInfo
            {
                FileName = gradlePath,
                Arguments = GradleTask,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            Debug.Log($"NativeLibBuilder: Running Gradle in '{workingDir}' → {gradlePath} {GradleTask}");
            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log($"[gradle] {e.Data}");
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogError($"[gradle] {e.Data}");
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            var success = process.ExitCode == 0;
            Debug.Log($"NativeLibBuilder: Gradle build finished with exit code {process.ExitCode} (success = {success})");
            return success;
        }

        private static void CopySoIntoUnity()
        {
            var source = Path.GetFullPath(SourceSoPath);
            var destination = Path.GetFullPath(UnitySoPath);
            if (!File.Exists(source))
                throw new FileNotFoundException($"NativeLibBuilder: .so file not found at expected path: {source}");
            var destDir = Path.GetDirectoryName(destination)!;
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            File.Copy(source, destination, overwrite: true);
            Debug.Log($"NativeLibBuilder: Copied .so from '{source}' to '{destination}'");
            AssetDatabase.Refresh();
        }

        private static void ConfigurePluginImporter()
        {
            var importer = AssetImporter.GetAtPath(UnitySoPath) as PluginImporter;
            if (importer == null)
            {
                Debug.LogError($"NativeLibBuilder: Could not get PluginImporter for asset at '{UnitySoPath}'");
                return;
            }
            importer.ClearSettings();
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, true);
            importer.SetPlatformData(BuildTarget.StandaloneLinux64, "CPU", "x86_64");
            importer.SaveAndReimport();
            Debug.Log("NativeLibBuilder: PluginImporter configuration updated for libsimple_gradient.so");
        }
    }
}

#endif
