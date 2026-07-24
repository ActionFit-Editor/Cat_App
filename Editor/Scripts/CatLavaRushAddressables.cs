#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ActionFit.Cat.App.Editor
{
    /// <summary>
    /// Previews and explicitly creates the three canonical Cat Lava Rush Addressable entries.
    /// </summary>
    public static class CatLavaRushAddressables
    {
        private const string MenuRoot = "Tools/Package/Cat App/Lava Rush Addressables/";
        private const string LogPrefix = "[CatLavaRushAddressables]";

        private static readonly EntrySpec[] Specs =
        {
            new(
                "UI_LavaRush",
                "Packages/com.actionfit.lava-rush.ui/Runtime/Prefabs/Main/UI_LavaRush.prefab",
                "ffae8bfdd6acf4657b158ff432e5a23b"),
            new(
                "UI_LavaRush_Icon",
                "Packages/com.actionfit.lava-rush.ui/Runtime/Prefabs/Icon/UI_LavaRush_Icon.prefab",
                "f7a017bca31e14a2eae90bc3a60cd5e3"),
            new(
                "UI_LavaRush_Cell",
                "Packages/com.actionfit.lava-rush.ui/Runtime/Prefabs/Icon/UI_LavaRush_Cell.prefab",
                "800bfcd600b24494eb593e8f6ed492b1"),
        };

        [MenuItem(MenuRoot + "Preview Registration", false, 20)]
        private static void PreviewMenu()
        {
            RegistrationPlan plan = BuildDefaultPlan();
            string report = plan.Report();
            if (plan.IsBlocked)
            {
                Debug.LogError(report);
            }
            else
            {
                Debug.Log(report);
            }

            EditorUtility.DisplayDialog("Cat Lava Rush Addressables Preview", report, "OK");
        }

        [MenuItem(MenuRoot + "Apply Missing Entries", false, 21)]
        private static void ApplyMenu()
        {
            if (Application.isBatchMode)
            {
                Debug.LogError($"{LogPrefix} Apply is disabled in batchmode.");
                return;
            }

            RegistrationPlan plan = BuildDefaultPlan();
            string report = plan.Report();
            if (plan.IsBlocked)
            {
                Debug.LogError(report);
                EditorUtility.DisplayDialog(
                    "Cat Lava Rush Addressables Blocked",
                    report + "\n\nNo Addressables settings were changed.",
                    "OK");
                return;
            }

            if (!plan.HasChanges)
            {
                Debug.Log(report);
                EditorUtility.DisplayDialog(
                    "Cat Lava Rush Addressables",
                    report + "\n\nAll three entries are already current.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Create Cat Lava Rush Addressables",
                report
                + "\n\nOnly missing entries will be created in the current writable default group."
                + "\nExisting entries, groups, labels, and addresses will not be changed.",
                "Create Missing",
                "Cancel");
            if (!confirmed)
            {
                Debug.Log($"{LogPrefix} Apply cancelled; no Addressables settings were changed.");
                return;
            }

            ApplyResult result = Apply(plan.Settings, true, true);
            if (result.Success)
            {
                Debug.Log(result.Message);
            }
            else
            {
                Debug.LogError(result.Message);
            }

            EditorUtility.DisplayDialog(
                result.Success ? "Cat Lava Rush Addressables" : "Cat Lava Rush Addressables Failed",
                result.Message,
                "OK");
        }

        internal static RegistrationPlan BuildDefaultPlan()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
            {
                return RegistrationPlan.Blocked("AddressableAssetSettings is unavailable.");
            }

            return BuildPlan(AddressableAssetSettingsDefaultObject.Settings);
        }

        internal static RegistrationPlan BuildPlan(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                return RegistrationPlan.Blocked("AddressableAssetSettings is unavailable.");
            }

            foreach (EntrySpec spec in Specs)
            {
                string actualGuid = AssetDatabase.AssetPathToGUID(spec.AssetPath);
                if (!string.Equals(actualGuid, spec.Guid, StringComparison.Ordinal))
                {
                    return RegistrationPlan.Blocked(
                        $"Canonical prefab GUID mismatch: {spec.AssetPath} expected {spec.Guid}, found {Display(actualGuid)}.");
                }
            }

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    EntrySpec spec = Specs.FirstOrDefault(candidate =>
                        string.Equals(candidate.Address, entry.address, StringComparison.Ordinal));
                    if (spec != null && !string.Equals(entry.guid, spec.Guid, StringComparison.Ordinal))
                    {
                        return RegistrationPlan.Blocked(
                            $"Address collision: {spec.Address} is already owned by {Display(entry.AssetPath)} ({entry.guid}).");
                    }
                }
            }

            var current = new List<EntrySpec>();
            var missing = new List<EntrySpec>();
            foreach (EntrySpec spec in Specs)
            {
                AddressableAssetEntry entry = settings.FindAssetEntry(spec.Guid);
                if (entry == null)
                {
                    missing.Add(spec);
                    continue;
                }

                if (entry.parentGroup == null)
                {
                    return RegistrationPlan.Blocked(
                        $"Incompatible group policy: {spec.Address} has no owning Addressables group.");
                }

                if (!string.Equals(entry.address, spec.Address, StringComparison.Ordinal))
                {
                    return RegistrationPlan.Blocked(
                        $"Create-only conflict: canonical GUID {spec.Guid} is registered as {Display(entry.address)}, not {spec.Address}.");
                }

                current.Add(spec);
            }

            AddressableAssetGroup targetGroup = null;
            if (missing.Count > 0)
            {
                targetGroup = FindCurrentDefaultGroup(settings);
                if (targetGroup == null)
                {
                    return RegistrationPlan.Blocked(
                        "Incompatible group policy: a serialized default Addressables group is unavailable.");
                }

                if (targetGroup.ReadOnly)
                {
                    return RegistrationPlan.Blocked(
                        $"Incompatible group policy: default group {targetGroup.Name} is read-only.");
                }

                if (targetGroup.GetSchema<BundledAssetGroupSchema>() == null)
                {
                    return RegistrationPlan.Blocked(
                        $"Incompatible group policy: default group {targetGroup.Name} has no BundledAssetGroupSchema.");
                }
            }

            return new RegistrationPlan(settings, targetGroup, current.ToArray(), missing.ToArray(), null);
        }

        internal static ApplyResult Apply(
            AddressableAssetSettings settings,
            bool confirmed,
            bool saveAssets,
            Action<int> afterCreate = null)
        {
            if (!confirmed)
            {
                return ApplyResult.CancelledResult($"{LogPrefix} Apply cancelled; no Addressables settings were changed.");
            }

            RegistrationPlan plan = BuildPlan(settings);
            if (plan.IsBlocked)
            {
                return ApplyResult.Failed($"{LogPrefix} Apply blocked: {plan.Blocker}");
            }

            if (!plan.HasChanges)
            {
                return ApplyResult.Completed($"{LogPrefix} All three entries are already current.");
            }

            var createdGuids = new List<string>();
            try
            {
                foreach (EntrySpec spec in plan.Missing)
                {
                    if (settings.FindAssetEntry(spec.Guid) != null)
                    {
                        throw new InvalidOperationException(
                            $"Addressables changed after preview: {spec.Address} is no longer missing.");
                    }

                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(
                        spec.Guid,
                        plan.TargetGroup,
                        false,
                        false);
                    if (entry == null)
                    {
                        throw new InvalidOperationException($"Failed to create {spec.Address}.");
                    }

                    createdGuids.Add(spec.Guid);
                    entry.SetAddress(spec.Address, false);
                    afterCreate?.Invoke(createdGuids.Count);
                }

                RegistrationPlan verified = BuildPlan(settings);
                if (verified.IsBlocked || verified.HasChanges)
                {
                    throw new InvalidOperationException(
                        verified.IsBlocked ? verified.Blocker : "Registration verification found missing entries.");
                }

                MarkChanged(settings, saveAssets);
                return ApplyResult.Completed(
                    $"{LogPrefix} Created {createdGuids.Count} missing entries; existing entries, groups, labels, and addresses were preserved.");
            }
            catch (Exception exception)
            {
                string rollbackError = RollBackCreatedEntries(settings, createdGuids, saveAssets);
                string suffix = string.IsNullOrEmpty(rollbackError)
                    ? "All entries created by this attempt were rolled back."
                    : $"Rollback requires review: {rollbackError}";
                return ApplyResult.Failed($"{LogPrefix} Apply failed: {exception.Message} {suffix}");
            }
        }

        private static AddressableAssetGroup FindCurrentDefaultGroup(AddressableAssetSettings settings)
        {
            return settings.groups.FirstOrDefault(group => group != null && group.Default);
        }

        private static string RollBackCreatedEntries(
            AddressableAssetSettings settings,
            IEnumerable<string> createdGuids,
            bool saveAssets)
        {
            try
            {
                foreach (string guid in createdGuids.Reverse())
                {
                    settings.RemoveAssetEntry(guid, false);
                }

                MarkChanged(settings, saveAssets);
                return "";
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private static void MarkChanged(AddressableAssetSettings settings, bool saveAssets)
        {
            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.BatchModification,
                null,
                false,
                true);
            EditorUtility.SetDirty(settings);
            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static string Display(string value)
        {
            return string.IsNullOrEmpty(value) ? "<missing>" : value;
        }

        internal sealed class RegistrationPlan
        {
            internal RegistrationPlan(
                AddressableAssetSettings settings,
                AddressableAssetGroup targetGroup,
                EntrySpec[] current,
                EntrySpec[] missing,
                string blocker)
            {
                Settings = settings;
                TargetGroup = targetGroup;
                Current = current ?? Array.Empty<EntrySpec>();
                Missing = missing ?? Array.Empty<EntrySpec>();
                Blocker = blocker;
            }

            internal AddressableAssetSettings Settings { get; }
            internal AddressableAssetGroup TargetGroup { get; }
            internal EntrySpec[] Current { get; }
            internal EntrySpec[] Missing { get; }
            internal string Blocker { get; }
            internal bool IsBlocked => !string.IsNullOrEmpty(Blocker);
            internal bool HasChanges => !IsBlocked && Missing.Length > 0;

            internal static RegistrationPlan Blocked(string blocker)
            {
                return new RegistrationPlan(null, null, null, null, blocker);
            }

            internal string Report()
            {
                var builder = new StringBuilder();
                builder.AppendLine($"{LogPrefix} Preview");
                foreach (EntrySpec spec in Specs)
                {
                    string state = Current.Contains(spec) ? "unchanged" : Missing.Contains(spec) ? "create" : "blocked";
                    builder.AppendLine($"{spec.Address}: {state}");
                    builder.AppendLine($"  Prefab: {spec.AssetPath}");
                    builder.AppendLine($"  GUID: {spec.Guid}");
                }

                builder.AppendLine($"Create group: {TargetGroup?.Name ?? "<none>"}");
                if (IsBlocked)
                {
                    builder.Append("Blocked: ").Append(Blocker);
                }
                else
                {
                    builder.Append(HasChanges
                        ? $"Result: create {Missing.Length} missing entries"
                        : "Result: unchanged");
                }

                return builder.ToString();
            }
        }

        internal sealed class ApplyResult
        {
            private ApplyResult(bool success, bool cancelled, string message)
            {
                Success = success;
                Cancelled = cancelled;
                Message = message;
            }

            internal bool Success { get; }
            internal bool Cancelled { get; }
            internal string Message { get; }

            internal static ApplyResult Completed(string message) => new(true, false, message);
            internal static ApplyResult CancelledResult(string message) => new(false, true, message);
            internal static ApplyResult Failed(string message) => new(false, false, message);
        }

        internal sealed class EntrySpec
        {
            internal EntrySpec(string address, string assetPath, string guid)
            {
                Address = address;
                AssetPath = assetPath;
                Guid = guid;
            }

            internal string Address { get; }
            internal string AssetPath { get; }
            internal string Guid { get; }
        }
    }
}
#endif
