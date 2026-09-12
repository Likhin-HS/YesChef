using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesChef.Editor
{
    [InitializeOnLoad]
    public static class BurstDotNetFix
    {
        static BurstDotNetFix()
        {
            ApplyFix();
        }

        [MenuItem("Tools/Burst/Fix Burst .NET Environment")]
        public static void ApplyFix()
        {
            string chosenPath = null;

            // 1. Check Unity's embedded DotNetSdk
            string unityDotNet = Path.Combine(EditorApplication.applicationContentsPath, "DotNetSdk");
            if (Directory.Exists(unityDotNet) && Directory.Exists(Path.Combine(unityDotNet, "shared", "Microsoft.AspNetCore.App")))
            {
                chosenPath = unityDotNet;
            }
            // 2. Fallback to user .dotnet folder
            else
            {
                string userDotNet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
                if (Directory.Exists(userDotNet) && Directory.Exists(Path.Combine(userDotNet, "shared", "Microsoft.AspNetCore.App")))
                {
                    chosenPath = userDotNet;
                }
            }

            if (!string.IsNullOrEmpty(chosenPath))
            {
                string current = Environment.GetEnvironmentVariable("DOTNET_ROOT");
                if (string.IsNullOrEmpty(current) || !current.Equals(chosenPath, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable("DOTNET_ROOT", chosenPath, EnvironmentVariableTarget.Process);
                    Debug.Log($"[BurstDotNetFix] Successfully configured DOTNET_ROOT to '{chosenPath}' for Unity process.");
                }
            }
            else
            {
                Debug.LogWarning("[BurstDotNetFix] Could not locate a valid .NET 8 runtime with Microsoft.AspNetCore.App.");
            }
        }
    }
}
