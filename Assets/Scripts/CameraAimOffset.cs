using System;
using UnityEngine;
using Unity.Cinemachine;

public class CameraAimOffset : CinemachineExtension {

    [NonSerialized] public Vector2 offset;
    private Vector2 appliedOffset;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime) {
        if (stage != CinemachineCore.Stage.Finalize) return;
        
        float keepZ = vcam.State.RawPosition.z;
        Vector3 origin = vcam.State.RawPosition - new Vector3(appliedOffset.x, appliedOffset.y, keepZ);
        Vector3 overridePos = origin + new Vector3(offset.x, offset.y, keepZ);
        vcam.ForceCameraPosition(overridePos, Quaternion.identity);
        appliedOffset = offset;
    }

}
