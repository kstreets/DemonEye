using System.IO;
using PrimeTween;
using UnityEngine;

public partial class Game {

    // Set from the Trailer window
    private TrailerSettings trailerSettings;

    private void InitTrailer() {
        trailerSettings = Resources.Load<TrailerSettings>(TrailerSettings.resourcesPath);
    }

    private bool TryGetTrailerExitPortal(Transform exitPortalParent, out Portal portal) {
        portal = null;
        if (trailerSettings == null || !trailerSettings.enabled || !trailerSettings.HasPortal) return false;
        if (trailerSettings.portalScene != Path.GetFileNameWithoutExtension(curRaid.map.sceneReference)) return false;
        if (trailerSettings.portalIndex >= exitPortalParent.childCount) return false;

        portal = exitPortalParent.GetChild(trailerSettings.portalIndex).GetComponent<Portal>();
        return portal != null;
    }

    private void ScheduleTrailerExitPortalSummon(Transform exitPortalParent) {
        if (!TryGetTrailerExitPortal(exitPortalParent, out Portal portal)) return;
        if (trailerSettings.autoSummonDelay < 0f) return;

        Tween.Delay(trailerSettings.autoSummonDelay, () => {
            // The raid could have ended, or the player summoned it themselves
            if (!curRaid.activeExitPortals.Contains(portal) || portal.state != Portal.State.Inactive) return;
            portal.StartOpenCloseSequence(config.gameplay.portalPostSummonDelay, config.gameplay.portalActiveDuration);
        });
    }

}
