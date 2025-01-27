using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class EditorCommands
{
    private const BuildTarget GAME_BUILD_TARGET = BuildTarget.WebGL;
    private const string SCRIPTING_BACKEND_ENV_VAR = "SCRIPTING_BACKEND";
    private const string VERSION_NUMBER_VAR = "VERSION_NUMBER_VAR";

    private static string defines = "";

    [MenuItem("TTT/Build")]
    static void PerformEditorBuild()
    {
        if (!int.TryParse(Application.version, out var version))
        {
            version = 1;
        }

        var buildPath = Application.dataPath + "/../../Builds/WebGL/";
        PerformBuildInternal(
            "WebGL",
            "TicTacToe",
            buildPath,
            version.ToString()
        );

        UploadClient(buildPath, version.ToString());
    }

    [MenuItem("TTT/Upload")]
    static void UploadBuild()
    {
        var buildPath = Application.dataPath + "/../../Builds/WebGL/";
        UploadClient(buildPath, Application.version);
    }

    static void PerformCIBuild()
    {
        PerformBuildInternal();
    }

    static void PerformBuildInternal(
        string customBuildTarget = null,
        string customBuildName = null,
        string customBuildPath = null,
        string customVersionNumber = null
    )
    {
        var buildTarget = GetBuildTarget(customBuildTarget);
        SetScriptingBackendFromEnv(buildTarget);

        var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

        SetScriptingSymbol("DEPLOYED", true);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        if (!string.IsNullOrEmpty(customVersionNumber))
        {
            PlayerSettings.bundleVersion = customVersionNumber;
        }
        else if (TryGetEnv(VERSION_NUMBER_VAR, out var bundleVersionNumber))
        {
            Console.WriteLine($":: Setting bundleVersionNumber to '{bundleVersionNumber}' (Length: {bundleVersionNumber.Length})");
            PlayerSettings.bundleVersion = bundleVersionNumber;
        }

        var buildPath = GetBuildPath(customBuildPath);
        var buildName = GetBuildName(customBuildName);
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName);

        Console.WriteLine($":: Performing build\n\tScenes:{GetEnabledScenes().Aggregate("", (c, s) => c + $"{s}, ")}\n\tPath: {fixedBuildPath}\n\tTarget: {buildTarget}");
        var buildReport = BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, BuildOptions.None);

        if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception($"Build ended with {buildReport.summary.result} status");

        Console.WriteLine(":: Done with build");
    }


    static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(name))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static string[] GetEnabledScenes()
    {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }

    static BuildTarget GetBuildTarget(string customBuildTarget = null)
    {
        string buildTargetName = customBuildTarget ?? GetArgument("customBuildTarget");
        Console.WriteLine(":: Received customBuildTarget " + buildTargetName);

        if (buildTargetName.ToLower() == "android")
        {
#if !UNITY_5_6_OR_NEWER
                // https://issuetracker.unity3d.com/issues/buildoptions-dot-acceptexternalmodificationstoplayer-causes-unityexception-unknown-project-type-0
                // Fixed in Unity 5.6.0
                // side effect to fix android build system:
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Internal;
#endif
        }

        if (buildTargetName.TryConvertToEnum(out BuildTarget target))
            return target;

        Console.WriteLine($":: {nameof(buildTargetName)} \"{buildTargetName}\" not defined on enum {nameof(BuildTarget)}, using {nameof(BuildTarget.NoTarget)} enum to build");

        return BuildTarget.NoTarget;
    }

    static string GetBuildPath(string customBuildPath = null)
    {
        string buildPath = customBuildPath ?? GetArgument("customBuildPath");
        Console.WriteLine(":: Received customBuildPath " + buildPath);
        if (buildPath == "")
        {
            throw new Exception("customBuildPath argument is missing");
        }
        return buildPath;
    }

    static string GetBuildName(string customBuildName = null)
    {
        string buildName = customBuildName ?? GetArgument("customBuildName");
        Console.WriteLine(":: Received customBuildName " + buildName);
        if (buildName == "")
        {
            throw new Exception("customBuildName argument is missing");
        }
        return buildName;
    }

    static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName)
    {
        if (buildTarget.ToString().ToLower().Contains("windows"))
        {
            buildName += ".exe";
        }
        else if (buildTarget == BuildTarget.Android)
        {
#if UNITY_2018_3_OR_NEWER
            buildName += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
#else
                buildName += ".apk";
#endif
        }
        return buildPath + buildName;
    }


    // https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
    static bool TryConvertToEnum<TEnum>(this string strEnumValue, out TEnum value)
    {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
        {
            value = default;
            return false;
        }

        value = (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        return true;
    }

    static bool TryGetEnv(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrEmpty(value);
    }


    static void SetScriptingBackendFromEnv(BuildTarget platform)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(platform);
        if (TryGetEnv(SCRIPTING_BACKEND_ENV_VAR, out string scriptingBackend))
        {
            if (scriptingBackend.TryConvertToEnum(out ScriptingImplementation backend))
            {
                Console.WriteLine($":: Setting ScriptingBackend to {backend} for {targetGroup}");
                PlayerSettings.SetScriptingBackend(targetGroup, backend);
            }
            else
            {
                string possibleValues = string.Join(", ", Enum.GetValues(typeof(ScriptingImplementation)).Cast<ScriptingImplementation>());
                throw new Exception($"Could not find '{scriptingBackend}' in ScriptingImplementation enum. Possible values are: {possibleValues}");
            }
        }
        else
        {
            var defaultBackend = PlayerSettings.GetDefaultScriptingBackend(targetGroup);
            Console.WriteLine($":: Using project's configured ScriptingBackend (should be {defaultBackend} for targetGroup {targetGroup}");
        }
    }

    private static void SetScriptingSymbol(string symbol, bool on)
    {
        if (on && Regex.IsMatch(defines, $"{symbol}(;|$)"))
            return;

        defines = on ? defines + $";{symbol}" : Regex.Replace(defines, $"{symbol}(;|$)", "");
    }

    private static void UploadClient(string buildPath, string version)
    {
        // Create the process
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:/Program Files/Git/git-bash.exe", // Bash executable
                Arguments = $"-c \"upload-remote.sh {buildPath} {version}\"",
                WorkingDirectory = Application.dataPath + "/../../", // Set working directory
                RedirectStandardOutput = true, // Redirect standard output
                RedirectStandardError = true, // Redirect error output
                UseShellExecute = false, // Required for redirection
                CreateNoWindow = true // Hide the shell window
            }
        };

        // Subscribe to output events
        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                UnityEngine.Debug.Log(args.Data); // Print to Unity console
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                UnityEngine.Debug.LogError(args.Data); // Print errors to Unity console
            }
        };

        try
        {
            UnityEngine.Debug.Log("t1");
            process.Start(); // Start the process
            UnityEngine.Debug.Log("t2");
            process.BeginOutputReadLine(); // Start reading output
            UnityEngine.Debug.Log("t3");
            process.BeginErrorReadLine(); // Start reading error output
            UnityEngine.Debug.Log("t4");
            process.WaitForExit(); // Wait for the process to exit
            UnityEngine.Debug.Log(process.ExitCode.ToString());
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error running script: {ex.Message}");
        }
        finally
        {
            process.Dispose(); // Clean up resources
        }
    }
}
