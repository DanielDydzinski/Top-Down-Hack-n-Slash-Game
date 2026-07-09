using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// One-time, disposable migration tool - same pattern as the now-removed GlobalXMigratorShortcut
// classes. Converts every Ability's old single abilityVisualParticles/visualEffectAudio slot into
// an equivalent AbilityVisualCue, so existing visuals keep working under the new cue-based system
// with zero manual re-entry. Safe to delete once every ability asset has been run through this.
public static class AbilityVisualCueMigrator
{
    [MenuItem("Tools/Migrate Ability Visuals To Cues")]
    public static void RunMigration()
    {
        string[] guids = AssetDatabase.FindAssets("t:Ability");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Ability ability = AssetDatabase.LoadAssetAtPath<Ability>(path);
            if (ability == null) continue;

            bool hasLegacyVisual = ability.baseSettings.abilityVisualParticles != null || ability.baseSettings.visualEffectAudio != null;
            bool alreadyMigrated = ability.visualCues != null && ability.visualCues.Count > 0;
            if (!hasLegacyVisual || alreadyMigrated) continue;

            if (ability.visualCues == null) ability.visualCues = new List<AbilityVisualCue>();

            ability.visualCues.Add(new AbilityVisualCue
            {
                id = "Execute",
                trigger = CueTrigger.OnExecute,
                prefab = ability.baseSettings.abilityVisualParticles,
                audio = ability.baseSettings.visualEffectAudio,
                attachPoint = ability.baseSettings.attachPoint,
                positionOffset = ability.baseSettings.spawnLocationOffset,
                rotationOffset = ability.baseSettings.spawnRotationOffset,
                persistUntilNextCue = false,
                autoReturnDelay = 0f
            });

            EditorUtility.SetDirty(ability);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CUE MIGRATION] Converted {count} ability visual(s) into a default Execute cue.");
    }
}
