#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseTemplateRecorder))]
public class PoseTemplateRecorderEditor : Editor
{
    private const string DefaultFolder =
        "Assets/PoseCaptures";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        PoseTemplateRecorder recorder =
            (PoseTemplateRecorder)target;

        using (new EditorGUI.DisabledScope(
                   !Application.isPlaying))
        {
            if (GUILayout.Button(
                    "Capture Pose To New Asset"))
            {
                CaptureToNewAsset(
                    recorder
                );
            }

            if (GUILayout.Button(
                    "Overwrite Selected PoseTemplate"))
            {
                OverwriteSelectedAsset(
                    recorder
                );
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode, pose the character using the " +
                "debug slot controls, then use a capture button.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Capture reads the server-authoritative limb " +
                "positions and Spine/Chest/Head side-lean values.",
                MessageType.Info
            );
        }
    }

    private static void CaptureToNewAsset(
        PoseTemplateRecorder recorder)
    {
        if (!recorder.TryCapture(
                out PoseCaptureSnapshot snapshot))
        {
            return;
        }

        EnsureFolderExists(
            DefaultFolder
        );

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{DefaultFolder}/PoseTemplate.asset"
            );

        PoseTemplate template =
            ScriptableObject.CreateInstance<
                PoseTemplate
            >();

        template.InitializeDefaults(
            recorder.DefaultHandTolerance,
            recorder.DefaultFootTolerance,
            recorder.DefaultLeanTolerance,
            recorder.DefaultLimbWeight,
            recorder.DefaultLeanWeight,
            recorder.GetDefaultOverallThresholds(),
            recorder.GetDefaultIndividualThresholds()
        );

        ApplySnapshot(
            template,
            snapshot
        );

        AssetDatabase.CreateAsset(
            template,
            path
        );

        EditorUtility.SetDirty(
            template
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject =
            template;

        EditorGUIUtility.PingObject(
            template
        );

        Debug.Log(
            $"Captured pose template to {path}.",
            template
        );
    }

    private static void OverwriteSelectedAsset(
        PoseTemplateRecorder recorder)
    {
        PoseTemplate selected =
            Selection.activeObject as PoseTemplate;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "No PoseTemplate Selected",
                "Select a PoseTemplate asset in the Project " +
                "window, then press Overwrite Selected PoseTemplate.",
                "OK"
            );

            return;
        }

        if (!recorder.TryCapture(
                out PoseCaptureSnapshot snapshot))
        {
            return;
        }

        ApplySnapshot(
            selected,
            snapshot
        );

        EditorUtility.SetDirty(
            selected
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(
            selected
        );

        Debug.Log(
            $"Overwrote captured values on {selected.name}.",
            selected
        );
    }

    private static void ApplySnapshot(
        PoseTemplate template,
        PoseCaptureSnapshot snapshot)
    {
        template.SetCapturedPose(
            snapshot.LeftHand,
            snapshot.RightHand,
            snapshot.LeftFoot,
            snapshot.RightFoot,
            snapshot.SpineLeanDegrees,
            snapshot.ChestLeanDegrees,
            snapshot.HeadLeanDegrees
        );
    }

    private static void EnsureFolderExists(
        string folderPath)
    {
        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }

        string[] parts =
            folderPath.Split('/');

        string current =
            parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string next =
                $"{current}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(
                    next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current =
                next;
        }
    }
}

#endif
