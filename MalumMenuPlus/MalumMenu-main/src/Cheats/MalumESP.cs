using UnityEngine;
using Sentry.Internal.Extensions;

namespace MalumMenu;
public static class MalumESP
{
    private static bool _freecamActive;
    private static bool _resolutionChangeNeeded;
    public static void SporeCloudVision(Mushroom mushroom)
    {
        if (CheatToggles.noShadows)
        {
            // Change the Z axis position of spore clouds as to make players appear above them
            mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, -1);
            return;
        }

        // Normal Z axis position: 5f
        mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, 5f);
    }

    public static bool IsFullbrightActive()
    {
        // Fullbright is automatically activated when being a ghost, zooming out, spectating other players, or "freecamming"
        // This is done to avoid issues with shadows
        if (CheatToggles.noShadows) return true;
        if (PlayerControl.LocalPlayer?.Data != null && PlayerControl.LocalPlayer.Data.IsDead) return true;
        if (Camera.main == null) return false;
        if (Camera.main.orthographicSize > 3f) return true;
        var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
        if (cam != null && cam.Target != PlayerControl.LocalPlayer) return true;
        return false;
    }

    public static bool IsMouseOverActiveMenuGUI()
    {
        var mousePos = Input.mousePosition;
        var guiMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);

        if (MenuUI.isGUIActive && MenuUI._windowRect.Contains(guiMousePos))
        {
            return true;
        }

        try
        {
            var hydraType = MenuUI.GetHydraUIType();
            if (hydraType != null)
            {
                var visibleField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visibleField != null && (bool)visibleField.GetValue(null))
                {
                    var posField = hydraType.GetField("windowPosition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var sizeProp = hydraType.GetProperty("WindowSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (posField != null && sizeProp != null)
                    {
                        var pos = (Vector2)posField.GetValue(null);
                        var size = (Vector2)sizeProp.GetValue(null, null);
                        var hydraRect = new Rect(pos.x, pos.y, size.x, size.y);
                        if (hydraRect.Contains(guiMousePos))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch { }

        return false;
    }

    private static bool IsMatchInfoGuideActive()
    {
        try
        {
            var guideType = System.Type.GetType("MatchInfoGuide, Assembly-CSharp") ?? System.Type.GetType("MatchInfoGuide");
            if (guideType != null)
            {
                var instanceProp = guideType.GetProperty("Instance");
                if (instanceProp != null)
                {
                    var instance = instanceProp.GetValue(null, null);
                    if (instance != null)
                    {
                        var activeProp = guideType.GetProperty("IsActive") ?? guideType.GetProperty("isActive");
                        if (activeProp != null)
                        {
                            var val = activeProp.GetValue(instance, null);
                            if (val is bool b) return b;
                        }
                    }
                }
            }
        }
        catch { }
        return false;
    }

    public static void ZoomOut(HudManager hudManager)
    {
        if (CheatToggles.zoomOut)
        {
            bool lobbyBusy = Utils.isLobby && (
                (FriendsListUI.Instance != null && FriendsListUI.Instance.IsOpen) ||
                (GameStartManager.Instance != null && GameStartManager.Instance.LobbyInfoPane != null && GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane != null && GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane.gameObject.active) ||
                (GameStartManager.Instance != null && GameStartManager.Instance.RulesEditPanel)
            );
            if (IsMouseOverActiveMenuGUI() || (hudManager != null && hudManager.Chat != null && hudManager.Chat.IsOpenOrOpening) || IsMatchInfoGuideActive() || PlayerCustomizationMenu.Instance || lobbyBusy) return;

            _resolutionChangeNeeded = true;

            if (Input.GetAxis("Mouse ScrollWheel") < 0f) // Zoom out
            {
                // Both the main camera and the UI camera need to be adjusted
                Camera.main.orthographicSize++;
                hudManager.UICamera.orthographicSize++;

                // Utils.AdjustResolution() seems to be needed to properly sync the game's UI after a change in orthographicSize
                Utils.AdjustResolution();
            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                // Zoom in
                if (!(Camera.main.orthographicSize > 3f)) return; // Never go below the default orthographicSize: 3f

                Camera.main.orthographicSize--;
                hudManager.UICamera.orthographicSize--;

                Utils.AdjustResolution();
            }
        }
        else
        {
            // orthographicSize is reset to default value: 3f
            Camera.main.orthographicSize = 3f;
            hudManager.UICamera.orthographicSize = 3f;

            // Utils.AdjustResolution() is invoked one last time to prevent issues with UI
            if (_resolutionChangeNeeded)
            {
                Utils.AdjustResolution();
                _resolutionChangeNeeded = false;
            }
        }
    }

    public static void MeetingNametags(MeetingHud meetingHud)
    {
        if (meetingHud == null || meetingHud.playerStates == null) return;

        foreach (var playerState in meetingHud.playerStates)
        {
            if (playerState == null || playerState.NameText == null) continue;

            try
            {
                // Fetch the NetworkedPlayerInfo of each playerState
                byte targetId = PlayerVoteAreaHelper.GetPlayerId(playerState);
                var data = GameData.Instance != null ? GameData.Instance.GetPlayerById(targetId) : null;
                if (data == null || data.Disconnected) continue;

                string playerName = data.PlayerName ?? (data.DefaultOutfit != null ? data.DefaultOutfit.PlayerName : "");
                if (string.IsNullOrEmpty(playerName)) continue;

                // Update the player's nametag appropriately
                playerState.NameText.text = Utils.GetNameTag(data, playerName);

                // Move and resize the nametag to prevent it overlapping with colorblind text
                if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.33f, 0.08f, 0f);
                    playerState.NameText.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                }
                else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
                else
                {
                    // Reset the position and scale of the nametag to default values
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
            }
            catch { }
        }
    }

    public static void PlayerNametags(PlayerPhysics playerPhysics)
    {
        try
        {
            if (playerPhysics == null || playerPhysics.myPlayer == null || playerPhysics.myPlayer.cosmetics == null || playerPhysics.myPlayer.Data == null) return;

            string playerName = playerPhysics.myPlayer.CurrentOutfit != null ? playerPhysics.myPlayer.CurrentOutfit.PlayerName : playerPhysics.myPlayer.Data.PlayerName;
            if (string.IsNullOrEmpty(playerName)) playerName = playerPhysics.myPlayer.Data.PlayerName ?? "";

            playerPhysics.myPlayer.cosmetics.SetName(Utils.GetNameTag(playerPhysics.myPlayer.Data, playerName));

            // Move the nameText up to prevent it overlapping with colorblind text or character sprite
            if (playerPhysics.myPlayer.cosmetics.nameText != null)
            {
                bool isHydralum = PresenceTracker.IsHydralumUser(playerPhysics.myPlayer.Data);
                bool isLocal = PlayerControl.LocalPlayer != null && playerPhysics.myPlayer.Data == PlayerControl.LocalPlayer.Data;
                bool showingGem = isHydralum && !CheatToggles.hideAllGems && !(isLocal && CheatToggles.hideMyGem);

                if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
                {
                    playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
                }
                else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo || showingGem)
                {
                    playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
                }
                else
                {
                    playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
                }
            }
        }
        catch { }
    }

    public static void ChatNametags(ChatBubble chatBubble)
    {
        try
        {
            if (chatBubble == null || chatBubble.NameText == null) return;

            // Ensure name does not wrap onto a second line and collide with message text
            chatBubble.NameText.enableWordWrapping = false;

            // Update the player's nametag appropriately
            chatBubble.NameText.text = Utils.GetNameTag(chatBubble.playerInfo, chatBubble.NameText.text, true);

            // Adjust the chatBubble's size to the new nametag to prevent issues
            chatBubble.NameText.ForceMeshUpdate(true, true);
            chatBubble.Background.size = new Vector2(5.52f, 0.2f + chatBubble.NameText.GetNotDumbRenderedHeight() + chatBubble.TextArea.GetNotDumbRenderedHeight());
            chatBubble.MaskArea.size = chatBubble.Background.size - new Vector2(0f, 0.03f);
        }
        catch { }
    }

    public static void SeeGhostsCheat(PlayerPhysics playerPhysics)
    {
        try
        {
            if (playerPhysics.myPlayer.Data.IsDead && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                playerPhysics.myPlayer.Visible = CheatToggles.seeGhosts;
            }
        }
        catch { }
    }

    public static void FreecamCheat()
    {
        try
        {
            if (CheatToggles.freecam)
            {
                if (Camera.main == null || !PlayerControl.LocalPlayer) return;

                // Disable FollowerCamera once freecam is enabled
                if (!_freecamActive)
                {
                    var folCam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                    if (folCam != null)
                    {
                        folCam.enabled = false;
                        folCam.Target = null;
                    }

                    _freecamActive = true;
                }

                // Prevent the player from moving while in freecam
                PlayerControl.LocalPlayer.moveable = false;

                // Get keyboard input
                var movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);

                // Change the camera's position depending on the keyboard input
                Camera.main.transform.position = Camera.main.transform.position + movement * 10f * Time.deltaTime;
            }
            else
            {
                // Re-enable FollowerCamera & movement once freecam is disabled
                if (!_freecamActive) return;
                if (PlayerControl.LocalPlayer) PlayerControl.LocalPlayer.moveable = true;
                if (Camera.main != null)
                {
                    var folCam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                    if (folCam != null)
                    {
                        folCam.enabled = true;
                        folCam.SetTarget(PlayerControl.LocalPlayer);
                    }
                }
                _freecamActive = false;
            }
        }
        catch { }
    }
}
