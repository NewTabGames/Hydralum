using System.Collections.Generic;
using System.Linq;
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

        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (CheatToggles.showConsole && (MenuUI.isGUIActive || keepOpen) && ConsoleUI.windowRect.Contains(guiMousePos))
        {
            return true;
        }

        var hydraRect = MenuUI.GetHydraRect();
        if (hydraRect.width > 0 && hydraRect.Contains(guiMousePos))
        {
            return true;
        }

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
        if (Camera.main == null || hudManager == null) return;

        if (CheatToggles.zoomOut)
        {
            bool lobbyBusy = Utils.isLobby && (
                (FriendsListUI.Instance != null && FriendsListUI.Instance.IsOpen) ||
                (GameStartManager.Instance != null && GameStartManager.Instance.LobbyInfoPane != null && GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane != null && GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane.gameObject.activeSelf) ||
                (GameStartManager.Instance != null && GameStartManager.Instance.RulesEditPanel != null && GameStartManager.Instance.RulesEditPanel.gameObject.activeSelf)
            );
            bool isWardrobeOpen = PlayerCustomizationMenu.Instance != null && PlayerCustomizationMenu.Instance.gameObject != null && PlayerCustomizationMenu.Instance.gameObject.activeInHierarchy;
            if (IsMouseOverActiveMenuGUI() || (hudManager.Chat != null && hudManager.Chat.IsOpenOrOpening) || IsMatchInfoGuideActive() || isWardrobeOpen || lobbyBusy) return;

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
                bool isDev = PresenceTracker.IsDevUser(playerPhysics.myPlayer.Data);
                bool isHydralum = isDev || PresenceTracker.IsHydralumUser(playerPhysics.myPlayer.Data);
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

    public static void UpdateChatBubbleColorTag(ChatBubble chatBubble)
    {
        if (chatBubble == null || chatBubble.ColorBlindName == null || chatBubble.playerInfo == null) return;

        try
        {
            if (CheatToggles.chatColorTags)
            {
                int colorId = chatBubble.playerInfo.DefaultOutfit != null 
                    ? chatBubble.playerInfo.DefaultOutfit.ColorId 
                    : (chatBubble.playerInfo.Object != null && chatBubble.playerInfo.Object.CurrentOutfit != null 
                        ? chatBubble.playerInfo.Object.CurrentOutfit.ColorId 
                        : 0);

                string colorName = "";
                if (Palette.ColorNames != null && colorId >= 0 && colorId < Palette.ColorNames.Length)
                {
                    colorName = TranslationController.Instance != null
                        ? TranslationController.Instance.GetString(Palette.ColorNames[colorId])
                        : Palette.ColorNames[colorId].ToString();
                }

                if (!string.IsNullOrEmpty(colorName))
                {
                    if (chatBubble.ColorBlindName.gameObject != null)
                    {
                        chatBubble.ColorBlindName.gameObject.SetActive(true);
                    }
                    chatBubble.ColorBlindName.enabled = true;
                    chatBubble.ColorBlindName.text = colorName;
                }
            }
            else if (AmongUs.Data.DataManager.Settings?.Accessibility != null && !AmongUs.Data.DataManager.Settings.Accessibility.ColorBlindMode)
            {
                if (chatBubble.ColorBlindName.gameObject != null)
                {
                    chatBubble.ColorBlindName.gameObject.SetActive(false);
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

            // Update colorblind text under avatar in chat bubble
            UpdateChatBubbleColorTag(chatBubble);

            // Ensure name does not wrap onto a second line and collide with message text
            chatBubble.NameText.enableWordWrapping = false;

            // Update the player's nametag appropriately
            if (chatBubble.playerInfo != null)
            {
                chatBubble.NameText.text = Utils.GetNameTag(chatBubble.playerInfo, chatBubble.NameText.text, true);
            }

            // Adjust the chatBubble's size to the new nametag to prevent issues
            if (chatBubble.Background != null && chatBubble.TextArea != null)
            {
                chatBubble.NameText.ForceMeshUpdate(true, true);
                chatBubble.Background.size = new Vector2(5.52f, 0.2f + chatBubble.NameText.GetNotDumbRenderedHeight() + chatBubble.TextArea.GetNotDumbRenderedHeight());
                if (chatBubble.MaskArea != null)
                {
                    chatBubble.MaskArea.size = chatBubble.Background.size - new Vector2(0f, 0.03f);
                }
            }
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

    public static void VentESPCheat()
    {
        try
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null || PlayerControl.AllPlayerControls == null) return;

            var allVents = ShipStatus.Instance.AllVents;
            var allPlayers = PlayerControl.AllPlayerControls;

            var ventingPlayers = new List<PlayerControl>();
            for (int i = 0; i < allPlayers.Count; i++)
            {
                var p = allPlayers[i];
                if (p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected && p.inVent)
                {
                    ventingPlayers.Add(p);
                }
            }

            bool localInVent = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.inVent;
            Vent currentLocalVent = Vent.currentVent;

            for (int vIdx = 0; vIdx < allVents.Length; vIdx++)
            {
                var vent = allVents[vIdx];
                if (vent == null || vent.gameObject == null || vent.transform == null) continue;

                // Hide stray navigation arrow buttons if local player is not actively inside this vent
                if ((!localInVent || currentLocalVent != vent) && vent.Buttons != null)
                {
                    for (int b = 0; b < vent.Buttons.Length; b++)
                    {
                        var btn = vent.Buttons[b];
                        if (btn != null && btn.gameObject != null && btn.gameObject.activeSelf)
                        {
                            btn.gameObject.SetActive(false);
                        }
                    }
                }

                var textTransform = vent.transform.Find("VentEspText");
                GameObject textObj = textTransform != null ? textTransform.gameObject : null;
                TMPro.TextMeshPro tmp = textObj != null ? textObj.GetComponent<TMPro.TextMeshPro>() : null;

                if (!CheatToggles.ventEsp || ventingPlayers.Count == 0)
                {
                    if (textObj != null && textObj.activeSelf) textObj.SetActive(false);
                    if (vent.myRend != null && vent.myRend.color != Color.white) vent.myRend.color = Color.white;
                    continue;
                }

                var occupants = new List<PlayerControl>();
                for (int pIdx = 0; pIdx < ventingPlayers.Count; pIdx++)
                {
                    var p = ventingPlayers[pIdx];
                    if (p != null && Vector2.Distance(p.transform.position, vent.transform.position) < 1.5f)
                    {
                        occupants.Add(p);
                    }
                }

                if (occupants.Count == 0)
                {
                    if (textObj != null && textObj.activeSelf) textObj.SetActive(false);
                    if (vent.myRend != null && vent.myRend.color != Color.white) vent.myRend.color = Color.white;
                    continue;
                }

                if (textObj == null)
                {
                    textObj = new GameObject("VentEspText");
                    textObj.transform.SetParent(vent.transform, false);
                    textObj.transform.localPosition = new Vector3(0f, 0.55f, -10f);
                    textObj.transform.localScale = Vector3.one;

                    tmp = textObj.AddComponent<TMPro.TextMeshPro>();
                    tmp.fontSize = 2.2f;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                }
                else if (tmp == null)
                {
                    tmp = textObj.GetComponent<TMPro.TextMeshPro>() ?? textObj.AddComponent<TMPro.TextMeshPro>();
                    tmp.fontSize = 2.2f;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                }

                // Ensure it always renders above all walls, shadows, and room objects
                if (PlayerControl.LocalPlayer?.cosmetics?.nameText != null)
                {
                    tmp.font = PlayerControl.LocalPlayer.cosmetics.nameText.font;
                    tmp.fontSharedMaterial = PlayerControl.LocalPlayer.cosmetics.nameText.fontSharedMaterial;
                    tmp.sortingLayerID = PlayerControl.LocalPlayer.cosmetics.nameText.sortingLayerID;
                }
                tmp.sortingOrder = 32767;
                textObj.transform.localPosition = new Vector3(0f, 0.55f, -10f);

                var lines = new List<string>();
                for (int oIdx = 0; oIdx < occupants.Count; oIdx++)
                {
                    var occ = occupants[oIdx];
                    if (occ == null || occ.Data == null) continue;

                    int colorId = occ.Data.DefaultOutfit != null 
                        ? occ.Data.DefaultOutfit.ColorId 
                        : (occ.CurrentOutfit != null ? occ.CurrentOutfit.ColorId : 0);

                    string colorName = "";
                    if (Palette.ColorNames != null && colorId >= 0 && colorId < Palette.ColorNames.Length)
                    {
                        colorName = TranslationController.Instance != null 
                            ? TranslationController.Instance.GetString(Palette.ColorNames[colorId]) 
                            : Palette.ColorNames[colorId].ToString();
                    }

                    string colorHex = (Palette.PlayerColors != null && colorId >= 0 && colorId < Palette.PlayerColors.Length)
                        ? ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[colorId])
                        : "FFFFFF";

                    string playerName = occ.Data.PlayerName ?? (occ.Data.DefaultOutfit != null ? occ.Data.DefaultOutfit.PlayerName : "Player");
                    string roleName = occ.Data.RoleType.ToString();
                    string roleHex = occ.Data.Role != null ? ColorUtility.ToHtmlStringRGB(occ.Data.Role.TeamColor) : "00FFFF";

                    lines.Add($"<color=#{colorHex}>{playerName}</color> <color=#FFAA00>[{colorName}]</color>\n<size=80%><color=#{roleHex}>{roleName}</color></size>");
                }

                tmp.text = string.Join("\n", lines);
                if (!textObj.activeSelf) textObj.SetActive(true);
                if (vent.myRend != null) vent.myRend.color = new Color(1f, 0.45f, 0.45f, 1f);
            }
        }
        catch { }
    }
}
