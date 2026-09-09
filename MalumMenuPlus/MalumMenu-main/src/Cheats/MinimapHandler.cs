using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;
public static class MinimapHandler
{
    public static bool minimapActive;
    public static List<HerePoint> herePoints = new List<HerePoint>();
    public static List<HerePoint> herePointsToRemove = new List<HerePoint>();

    public static bool IsCheatEnabled()
    {
        return CheatToggles.mapCrew || CheatToggles.mapGhosts || CheatToggles.mapImps;
    }

    // --- Freeze last positions (map during meetings) ------------------------------------------------
    // While no meeting is up we record every player's world position each frame. When a meeting/exile is
    // active the map draws these frozen positions instead of live ones, so opening the map mid-meeting
    // shows where everyone was just BEFORE the meeting, not everyone bunched at the cafeteria table.
    public static readonly Dictionary<byte, Vector3> lastPositions = new Dictionary<byte, Vector3>();

    public static bool IsMeetingActive => MeetingHud.Instance != null || ExileController.Instance != null;

    // Called every frame from HudManager.Update.
    public static void TrackPositions()
    {
        if (ShipStatus.Instance == null || PlayerControl.AllPlayerControls == null)
        {
            lastPositions.Clear();
            return;
        }
        if (IsMeetingActive) return; // freeze: keep the pre-meeting snapshot

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null) continue;
            lastPositions[p.PlayerId] = p.transform.position;
        }
    }

    public static void HandleHerePoint(HerePoint herePoint)
    {
        Color herePointColor = new Color();

        try // try-catch to fix issues caused by player disconnection
        {
            herePoint.sprite.gameObject.SetActive(false); // Initally make player icon invisible

            // Crewmate, alive
            if (CheatToggles.mapCrew && !herePoint.player.Data.Role.IsImpostor)
            {
                if (!herePoint.player.Data.IsDead)
                {
                    herePoint.sprite.gameObject.SetActive(true);
                    if (CheatToggles.colorBasedMap)
                    {
                        herePointColor = herePoint.player.Data.Color; // Color-Based Icon
                    }
                    else
                    {
                        herePointColor = herePoint.player.Data.Role.TeamColor; // Role-Based Icon
                    }
                }
            }
            // Impostor, alive
            else if (CheatToggles.mapImps && herePoint.player.Data.Role.IsImpostor)
            {
                if (!herePoint.player.Data.IsDead)
                {
                    herePoint.sprite.gameObject.SetActive(true);
                    if (CheatToggles.colorBasedMap)
                    {
                        herePointColor = herePoint.player.Data.Color; // Color-Based Icon
                    }
                    else
                    {
                        herePointColor = herePoint.player.Data.Role.TeamColor; // Role-Based Icon
                    }
                }
            }
            // Any Role, dead
            if (CheatToggles.mapGhosts && herePoint.player.Data.IsDead)
            {
                herePoint.sprite.gameObject.SetActive(true);
                if (CheatToggles.colorBasedMap)
                {
                    herePointColor = herePoint.player.Data.Color; // Color-Based Icon
                }
                else
                {
                    herePointColor = Palette.White;
                }
            }

            if (herePoint.sprite.gameObject.active)
            {
                // Set the right colors for active herePoint icons
                herePoint.sprite.material.SetColor(PlayerMaterial.BackColor, herePointColor);
                herePoint.sprite.material.SetColor(PlayerMaterial.BodyColor, herePointColor);
                herePoint.sprite.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);

                // Sync the position of active herePoint icons with their players. During a meeting/exile,
                // use the frozen pre-meeting position instead of the live (cafeteria) one.
                Vector3 vector;
                if (IsMeetingActive && lastPositions.TryGetValue(herePoint.player.PlayerId, out var frozen))
                    vector = frozen;
                else
                    vector = herePoint.player.transform.position;
                float mapScale = (ShipStatus.Instance != null && ShipStatus.Instance.MapScale != 0f) ? ShipStatus.Instance.MapScale : 1f;
                vector /= mapScale;
                float localScaleX = ShipStatus.Instance != null ? ShipStatus.Instance.transform.localScale.x : 1f;
                vector.x *= Mathf.Sign(localScaleX);
                vector.z = -1f;
                herePoint.sprite.transform.localPosition = vector;
            }
        }
        catch
        {
            // Remove icons that are causing problems
            Object.Destroy(herePoint.sprite.gameObject);
            herePointsToRemove.Add(herePoint);
        }
    }
}
