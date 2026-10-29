using System;
using UnityEngine;
using Unity.Cinemachine;

public class CameraAimOffset : CinemachineExtension {

    [NonSerialized] public Vector2 offset;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime) {
        if (stage != CinemachineCore.Stage.Finalize) return;
        // Without a follow target the composer doesn't move the camera, so RawPosition is last frame's position and the offset would build up
        if (vcam.Follow == null) return;
        state.RawPosition += new Vector3(offset.x, offset.y, 0f);
    }

}
