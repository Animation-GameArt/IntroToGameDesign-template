using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CollinGame.Core.Editor
{
    /// <summary>
    /// Exports the course's .unitypackage files: CollinGame-Core (Core, 2D and
    /// 3D scripts), one package per kit, and one per Kenney art pack. Kit
    /// packages don't include Core; import Core first.
    ///
    /// Collin Game > Packages > Export Course Packages... asks for a folder and
    /// writes them all there. The course repository's
    /// tools/make_unitypackages.py makes the same packages without Unity.
    /// </summary>
    public static class CoursePackageExporter
    {
        const string Course = "Assets/_Course";
        const string ArtRoot = Course + "/Art/Kenney";

        static readonly string[] CoreRoots =
        {
            Course + "/Core", Course + "/TwoD", Course + "/ThreeD", Course + "/Credits",
            Course + "/README.md", Course + "/VERSION.txt", ArtRoot + "/README.md"
        };

        [MenuItem("Collin Game/Packages/Export Course Packages...")]
        static void ExportAll()
        {
            string folder = EditorUtility.SaveFolderPanel("Export course packages to", "", "CoursePackages");

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            string version = ReadVersion();
            int made = 0;

            made += Export(CoreRoots, Path.Combine(folder, $"CollinGame-Core-{version}.unitypackage"));

            foreach (string kit in AssetDatabase.GetSubFolders(Course + "/Kits"))
            {
                made += Export(new[] { kit }, Path.Combine(folder, $"CollinGame-Kit-{Path.GetFileName(kit)}-{version}.unitypackage"));
            }

            if (AssetDatabase.IsValidFolder(ArtRoot))
            {
                foreach (string pack in AssetDatabase.GetSubFolders(ArtRoot))
                {
                    string name = Path.GetFileName(pack).Replace("kenney_", "");
                    made += Export(new[] { pack }, Path.Combine(folder, $"Kenney-{name}.unitypackage"));
                }
            }

            Debug.Log($"Course packages: exported {made} packages to {folder}.");
            EditorUtility.RevealInFinder(folder);
        }

        static int Export(string[] roots, string file)
        {
            List<string> existing = new List<string>();

            foreach (string root in roots)
            {
                if (AssetDatabase.IsValidFolder(root) || File.Exists(root))
                {
                    existing.Add(root);
                }
            }

            if (existing.Count == 0)
            {
                return 0;
            }

            // Recurse takes everything inside the folders. Dependencies are left
            // out on purpose, so a kit package doesn't also carry Core.
            AssetDatabase.ExportPackage(existing.ToArray(), file, ExportPackageOptions.Recurse);
            return 1;
        }

        static string ReadVersion()
        {
            string path = Course + "/VERSION.txt";
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "0.0.0";
        }
    }
}
