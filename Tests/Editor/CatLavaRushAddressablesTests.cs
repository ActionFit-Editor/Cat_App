#if UNITY_EDITOR
using System;
using System.Linq;
using ActionFit.Cat.App.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ActionFit.Cat.App.Tests
{
    public sealed class CatLavaRushAddressablesTests
    {
        private static readonly string[] Addresses =
        {
            "UI_LavaRush",
            "UI_LavaRush_Icon",
            "UI_LavaRush_Cell",
        };

        private static readonly string[] Guids =
        {
            "ffae8bfdd6acf4657b158ff432e5a23b",
            "f7a017bca31e14a2eae90bc3a60cd5e3",
            "800bfcd600b24494eb593e8f6ed492b1",
        };

        private AddressableAssetSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = AddressableAssetSettings.Create(
                "Assets",
                "MCC1632AddressablesTest",
                true,
                false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
            {
                UnityEngine.Object.DestroyImmediate(_settings);
            }
        }

        [Test]
        public void Preview_ReportsThreeMissingEntriesWithoutMutation()
        {
            int entryCount = EntryCount();
            string defaultGroupGuid = _settings.DefaultGroup.Guid;

            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(_settings);

            Assert.That(plan.IsBlocked, Is.False, plan.Blocker);
            Assert.That(plan.Missing.Select(spec => spec.Address), Is.EquivalentTo(Addresses));
            Assert.That(plan.Current, Is.Empty);
            Assert.That(EntryCount(), Is.EqualTo(entryCount));
            Assert.That(_settings.DefaultGroup.Guid, Is.EqualTo(defaultGroupGuid));
        }

        [Test]
        public void Apply_WhenCancelled_CreatesNothing()
        {
            CatLavaRushAddressables.ApplyResult result =
                CatLavaRushAddressables.Apply(_settings, false, false);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Cancelled, Is.True);
            Assert.That(EntryCount(), Is.Zero);
        }

        [Test]
        public void Apply_CreatesOnlyMissingEntriesAndRepeatIsUnchanged()
        {
            CatLavaRushAddressables.ApplyResult first =
                CatLavaRushAddressables.Apply(_settings, true, false);

            Assert.That(first.Success, Is.True, first.Message);
            AssertCanonicalEntries();

            CatLavaRushAddressables.ApplyResult repeat =
                CatLavaRushAddressables.Apply(_settings, true, false);

            Assert.That(repeat.Success, Is.True, repeat.Message);
            Assert.That(repeat.Message, Does.Contain("already current"));
            AssertCanonicalEntries();
        }

        [Test]
        public void Apply_PreservesMatchingEntryGroupAndLabels()
        {
            AddressableAssetGroup originalGroup = _settings.CreateGroup(
                "Lava Rush Existing",
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            AddressableAssetEntry existing = CreateEntry(0, originalGroup);
            existing.SetLabel("project-owned", true, false);

            CatLavaRushAddressables.ApplyResult result =
                CatLavaRushAddressables.Apply(_settings, true, false);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(_settings.FindAssetEntry(Guids[0]), Is.SameAs(existing));
            Assert.That(existing.parentGroup, Is.SameAs(originalGroup));
            Assert.That(existing.labels, Does.Contain("project-owned"));
            AssertCanonicalEntries();
        }

        [Test]
        public void Preview_BlocksAddressOwnedByAnotherGuidWithoutMutation()
        {
            string otherGuid = AssetDatabase.AssetPathToGUID("Packages/com.actionfit.cat.app/README.md");
            Assert.That(otherGuid, Is.Not.Empty);
            AddressableAssetEntry collision = _settings.CreateOrMoveEntry(
                otherGuid,
                _settings.DefaultGroup,
                false,
                false);
            collision.SetAddress(Addresses[0], false);
            int entryCount = EntryCount();

            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(_settings);
            CatLavaRushAddressables.ApplyResult apply =
                CatLavaRushAddressables.Apply(_settings, true, false);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocker, Does.Contain("Address collision"));
            Assert.That(apply.Success, Is.False);
            Assert.That(EntryCount(), Is.EqualTo(entryCount));
            Assert.That(Guids.All(guid => _settings.FindAssetEntry(guid) == null), Is.True);
        }

        [Test]
        public void Preview_BlocksCanonicalGuidRegisteredUnderAnotherAddress()
        {
            AddressableAssetEntry entry = _settings.CreateOrMoveEntry(
                Guids[0],
                _settings.DefaultGroup,
                false,
                false);
            entry.SetAddress("UI_Other", false);

            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(_settings);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocker, Does.Contain("Create-only conflict"));
            Assert.That(entry.address, Is.EqualTo("UI_Other"));
        }

        [Test]
        public void Preview_BlocksDefaultGroupWithoutBundledPolicy()
        {
            UnityEngine.Object.DestroyImmediate(_settings);
            _settings = AddressableAssetSettings.Create(
                "Assets",
                "MCC1632AddressablesInvalidGroupTest",
                false,
                false);
            _settings.CreateGroup(
                "Invalid Default",
                true,
                false,
                false,
                null,
                typeof(ContentUpdateGroupSchema));

            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(_settings);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocker, Does.Contain("BundledAssetGroupSchema"));
            Assert.That(EntryCount(), Is.Zero);
        }

        [Test]
        public void Preview_BlocksMissingSettings()
        {
            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(null);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocker, Does.Contain("unavailable"));
        }

        [Test]
        public void Preview_BlocksReadOnlyDefaultGroup()
        {
            AddressableAssetGroup readOnlyGroup = _settings.CreateGroup(
                "Read Only Default",
                false,
                true,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            var serializedSettings = new SerializedObject(_settings);
            serializedSettings.FindProperty("m_DefaultGroup").stringValue = readOnlyGroup.Guid;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            CatLavaRushAddressables.RegistrationPlan plan = CatLavaRushAddressables.BuildPlan(_settings);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocker, Does.Contain("read-only"));
            Assert.That(EntryCount(), Is.Zero);
        }

        [Test]
        public void Apply_RollsBackEveryCreatedEntryWhenRegistrationFails()
        {
            CatLavaRushAddressables.ApplyResult result = CatLavaRushAddressables.Apply(
                _settings,
                true,
                false,
                count =>
                {
                    if (count == 2)
                    {
                        throw new InvalidOperationException("Injected registration failure.");
                    }
                });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("rolled back"));
            Assert.That(Guids.All(guid => _settings.FindAssetEntry(guid) == null), Is.True);
        }

        private AddressableAssetEntry CreateEntry(int index, AddressableAssetGroup group)
        {
            AddressableAssetEntry entry = _settings.CreateOrMoveEntry(Guids[index], group, false, false);
            entry.SetAddress(Addresses[index], false);
            return entry;
        }

        private int EntryCount()
        {
            return _settings.groups
                .Where(group => group != null)
                .Sum(group => group.entries.Count(entry => entry != null));
        }

        private void AssertCanonicalEntries()
        {
            for (int index = 0; index < Guids.Length; index++)
            {
                AddressableAssetEntry entry = _settings.FindAssetEntry(Guids[index]);
                Assert.That(entry, Is.Not.Null, Addresses[index]);
                Assert.That(entry.address, Is.EqualTo(Addresses[index]));
            }

            Assert.That(EntryCount(), Is.EqualTo(Guids.Length));
        }
    }
}
#endif
