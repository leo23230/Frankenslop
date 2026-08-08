POSE SCORING SYSTEM SETUP

1. Add the runtime scripts to a normal Scripts folder.
2. Put Editor/PoseTemplateRecorderEditor.cs inside an Editor folder.
3. Merge or replace PoseableLimbIKController.cs.
   This version adds scoring accessors plus synchronized Spine/Chest/Head side-lean values.
4. Add PoseTemplateRecorder to the poseable body and assign the controller.
5. Enter Play Mode and use your 1-4 / Tab testing controls to pose all four limbs.
6. In the recorder Inspector, press Capture Pose To New Asset.
7. The asset is created in Assets/PoseCaptures and survives exiting Play Mode.
8. Edit its tolerances, weights, required settings, and thresholds.
9. Assign it to PoseWallJudge.
10. On the server, call poseWallJudge.EvaluateNow() when the wall reaches the evaluation plane.

CAPTURED / SCORED VALUES
- Left hand X/Y
- Right hand X/Y
- Left foot X/Y
- Right foot X/Y
- Spine signed side lean
- Chest signed side lean
- Head signed side lean

OVERALL RESULT
The overall cooperative score uses all seven weighted components and maps to:
Fail, Pass, Good, Perfect.

INDIVIDUAL RESULTS
Each player's hidden score uses only the limb assigned to that PlayerSlot.
Spine, Chest, and Head influence the team result only and are not blamed on one player.
PoseWallJudge keeps individual scores in LatestResult on the server.
The provided ObserversRpc sends only the overall result.

PLAY-MODE AUTHORING
Capture Pose To New Asset creates a new ScriptableObject.
Overwrite Selected PoseTemplate updates only the captured target values and preserves
the selected asset's tolerances, weights, required flags, and score thresholds.
