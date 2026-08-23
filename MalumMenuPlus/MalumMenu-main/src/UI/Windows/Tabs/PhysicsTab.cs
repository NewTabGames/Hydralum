using UnityEngine;

namespace MalumMenu;

public class PhysicsTab : ITab
{
    public string name => "Stuff";

    public void Draw()
    {
        GUILayout.Label("Stuff", GUIStylePreset.TabSubtitle);
        GUILayout.Label("Shoutout Cyberleek", GUIStylePreset.Hint);
        GUILayout.Space(6);

        CheatToggles.jigglePhysics = GUILayout.Toggle(CheatToggles.jigglePhysics, " Enable Jiggle Physics");

        CheatToggles.waterPouringPhysics = GUILayout.Toggle(CheatToggles.waterPouringPhysics, " Water Pouring Physics");

        CheatToggles.installGta6 = GUILayout.Toggle(CheatToggles.installGta6, " Install GTA 6");

        CheatToggles.nukeCyberleek = GUILayout.Toggle(CheatToggles.nukeCyberleek, " Nuke Cyberleek");

        CheatToggles.longWeeWeePhysics = GUILayout.Toggle(CheatToggles.longWeeWeePhysics, " Long wee wee physics");

        CheatToggles.sex = GUILayout.Toggle(CheatToggles.sex, " Sex");

        CheatToggles.launchFirework = GUILayout.Toggle(CheatToggles.launchFirework, " Launch the El culo del diablo de puro dolor, agonía y sufrimiento firework");

        CheatToggles.cokeCanPhysics = GUILayout.Toggle(CheatToggles.cokeCanPhysics, " Coke Can Physics");

        CheatToggles.sussy = GUILayout.Toggle(CheatToggles.sussy, " sussy");

        CheatToggles.duelingForHonour = GUILayout.Toggle(CheatToggles.duelingForHonour, " dueling for ones honour");

        CheatToggles.musketLineBattles = GUILayout.Toggle(CheatToggles.musketLineBattles, " Musket Line Battles and die");

        CheatToggles.sexWithCyberleek = GUILayout.Toggle(CheatToggles.sexWithCyberleek, " Sex with Cyberleek");

        CheatToggles.ricky = GUILayout.Toggle(CheatToggles.ricky, " Ricky");

        CheatToggles.CreamPie = GUILayout.Toggle(CheatToggles.CreamPie, " Creampie Imposters");

        CheatToggles.letTyGamer4Rest = GUILayout.Toggle(CheatToggles.letTyGamer4Rest, " Let TyGamer4 Rest");

        CheatToggles.sayGex = GUILayout.Toggle(CheatToggles.sayGex, " Say Gex");

        CheatToggles.makeTyGamer4WorkHarder = GUILayout.Toggle(CheatToggles.makeTyGamer4WorkHarder, " Make TyGamer4 Work Harder");

        GUILayout.Space(8);
        if (GUILayout.Button("When The Imposter Is Gay", GUIStylePreset.NormalButton, GUILayout.Height(26)))
        {
            Application.OpenURL("https://www.youtube.com/watch?v=4o-625plsMk");
        }
    }
}
