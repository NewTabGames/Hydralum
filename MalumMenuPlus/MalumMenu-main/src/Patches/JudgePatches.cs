using System.Reflection;
using HarmonyLib;

namespace MalumMenu;

// When "Enable Judge Overrule (No Tasks)" is on, the Judge can overrule regardless of task completion by
// forcing JudgeRole.IsBlockedByTasks to return false.
//
// Patched via reflection: the strongly-typed JudgeRole isn't present in the game libs we build against
// (Malum already accesses Judge internals reflectively elsewhere), so we resolve the target at runtime and
// simply skip the patch if this game build doesn't have it.
[HarmonyPatch]
public static class JudgeTasksPatch
{
    private static MethodBase ResolveTarget()
    {
        var judgeType = AccessTools.TypeByName("JudgeRole");
        if (judgeType == null) return null;
        return AccessTools.Method(judgeType, "IsBlockedByTasks")
            ?? AccessTools.PropertyGetter(judgeType, "IsBlockedByTasks");
    }

    public static bool Prepare() => ResolveTarget() != null;

    public static MethodBase TargetMethod() => ResolveTarget();

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.judgeNoTasks) __result = false;
    }
}
