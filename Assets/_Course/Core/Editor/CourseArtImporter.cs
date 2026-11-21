using UnityEditor;
using UnityEngine;

namespace CollinGame.EditorTools
{
    /// <summary>
    /// Gives the course's art packs the right import settings, so nobody has to
    /// set them by hand. It only looks at the pack folders listed below.
    ///
    /// The settings are applied the first time each image is imported. To apply
    /// them again to every image, use the menu:
    ///   Collin Game > Art > Reapply Art Import Settings
    /// </summary>
    public class CourseArtImporter : AssetPostprocessor
    {
        struct Pack
        {
            public string folder;
            public int pixelsPerUnit;
            public bool pixelArt;

            public Pack(string folder, int pixelsPerUnit, bool pixelArt)
            {
                this.folder = folder;
                this.pixelsPerUnit = pixelsPerUnit;
                this.pixelArt = pixelArt;
            }
        }

        // One entry per art pack. Pixels Per Unit is the size of one tile in pixels,
        // so that one tile is exactly one unit in the scene.
        static readonly Pack[] packs =
        {
            new Pack("Assets/_Course/Art/Kenney/kenney_pixel-platformer/", 18, true),

            // Packs for the other kits. Each folder is named as Kenney's download is named.
            // Check the pixels per unit when each pack is first added.
            new Pack("Assets/_Course/Art/Kenney/kenney_tiny-dungeon/", 16, true),
            new Pack("Assets/_Course/Art/Kenney/kenney_tiny-town/", 16, true),
            new Pack("Assets/_Course/Art/Kenney/kenney_physics-assets/", 70, false),
            new Pack("Assets/_Course/Art/Kenney/kenney_puzzle-pack-2/", 256, false),
            new Pack("Assets/_Course/Art/Kenney/kenney_space-shooter-remastered/", 100, false),
            new Pack("Assets/_Course/Art/Kenney/kenney_pixel-ui-pack/", 100, true),
            new Pack("Assets/_Course/Art/Kenney/kenney_pixel-shmup/", 16, true),
            new Pack("Assets/_Course/Art/Kenney/kenney_sokoban-pack/", 64, false),
            new Pack("Assets/_Course/Art/Kenney/kenney_tower-defense-top-down/", 64, false),
        };

        static bool reapplying;

        void OnPreprocessTexture()
        {
            for (int i = 0; i < packs.Length; i++)
            {
                if (!assetPath.StartsWith(packs[i].folder))
                {
                    continue;
                }

                TextureImporter importer = (TextureImporter)assetImporter;

                // Leave images alone once they have been imported, so changes
                // made by hand afterwards are kept.
                if (!reapplying && !importer.importSettingsMissing)
                {
                    return;
                }

                importer.textureType = TextureImporterType.Sprite;

                // Images in a "Tiles" folder are one sprite each. Tile sheets
                // keep whatever slicing they already have.
                if (assetPath.Contains("/Tiles/"))
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                }

                importer.spritePixelsPerUnit = packs[i].pixelsPerUnit;
                importer.mipmapEnabled = false;

                if (packs[i].pixelArt)
                {
                    // Keep pixel art sharp: no blurring and no compression.
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                }

                return;
            }
        }

        [MenuItem("Collin Game/Art/Reapply Art Import Settings")]
        static void ReapplyAll()
        {
            int count = 0;
            reapplying = true;

            try
            {
                for (int i = 0; i < packs.Length; i++)
                {
                    string folder = packs[i].folder.TrimEnd('/');

                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        continue;
                    }

                    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

                    for (int g = 0; g < guids.Length; g++)
                    {
                        AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guids[g]), ImportAssetOptions.ForceUpdate);
                        count++;
                    }
                }
            }
            finally
            {
                reapplying = false;
            }

            Debug.Log($"Reapplied the course's art import settings to {count} images.");
        }
    }
}
