using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using System.IO;
using Unity.EditorCoroutines.Editor;
using System.Collections;

[CustomEditor(typeof(PTTI_Multiplayer.VideoList))]
public class VideoListEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PTTI_Multiplayer.VideoList list = (PTTI_Multiplayer.VideoList)target;

        if (GUILayout.Button("Generate Thumbnails & Names"))
        {
            if (list.videos == null || list.videos.Length == 0)
            {
                Debug.LogWarning("No videos in list!");
                return;
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateThumbnails(list));
        }
    }

    private IEnumerator GenerateThumbnails(PTTI_Multiplayer.VideoList list)
    {
        string savePath = "Assets/HVAC_Multiplayer/Videos/VideoThumbnails";
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        for (int i = 0; i < list.videos.Length; i++)
        {
            var entry = list.videos[i];

            // 🔹 Skip if already has name and thumbnail
            if (!string.IsNullOrEmpty(entry.videoName) && entry.thumbnail != null)
            {
                Debug.Log($"⏩ Skipping {entry.videoName} (already has name + thumbnail)");
                continue;
            }

            // Setup VideoPlayer
            GameObject go = new("ThumbnailGen");
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.renderMode = VideoRenderMode.RenderTexture;

            if (entry.useURL && !string.IsNullOrEmpty(entry.videoURL))
            {
                vp.source = VideoSource.Url;
                vp.url = entry.videoURL;

                if (string.IsNullOrEmpty(entry.videoName))
                    entry.videoName = Path.GetFileNameWithoutExtension(entry.videoURL);
            }
            else if (entry.videoClip != null)
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = entry.videoClip;

                if (string.IsNullOrEmpty(entry.videoName))
                    entry.videoName = entry.videoClip.name;
            }
            else
            {
                Debug.LogWarning($"⚠️ Skipping entry {i}, no VideoClip or URL set.");
                continue;
            }

            // 🔹 Show progress
            float progress = (float)i / list.videos.Length;
            EditorUtility.DisplayProgressBar("Generating Thumbnails", $"Processing {entry.videoName}", progress);

            var rt = new RenderTexture(512, 288, 0);
            vp.targetTexture = rt;

            vp.Prepare();
            while (!vp.isPrepared)
            {
                EditorApplication.QueuePlayerLoopUpdate(); // force editor update
                yield return null;
            }

            vp.time = 3f; // capture at 3 seconds
            vp.Play();

            // Wait for a valid frame
            double start = EditorApplication.timeSinceStartup;
            while (vp.frame <= 0 && EditorApplication.timeSinceStartup - start < 3.0)
                yield return null;

            // Capture
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // Save PNG (optional, also store in ScriptableObject)
            string safeName = entry.useURL
                            ? Path.GetFileNameWithoutExtension(entry.videoURL)
                            : entry.videoClip.name;

            string filePath = Path.Combine(savePath, $"{safeName}_thumb.png");
            File.WriteAllBytes(filePath, tex.EncodeToPNG());

            AssetDatabase.Refresh();
            entry.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

            vp.Stop();
            DestroyImmediate(go);

            // Save ScriptableObject changes
            EditorUtility.SetDirty(list);

            yield return null;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        Debug.Log("✅ All video entries updated with thumbnails & names.");
    }
}