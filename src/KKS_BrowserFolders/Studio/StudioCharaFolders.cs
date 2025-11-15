using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;

namespace BrowserFolders.Studio
{
    public class StudioCharaFolders : BaseStudioFolders<CharaListEntry, CharaList, StudioCharaFoldersHelper>
    {
        public override bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Chara Studio", "Enable folder browser in character browser", true, "Changes take effect on game restart");
            
            if (!isStudio || !enable.Value) return false;

            Title = "Character folder";

            harmony.PatchAll(typeof(StudioCharaFolders));

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPrefix(CharaList __instance, bool _force)
        {
            try
            {
                SetRefilterOnly(__instance, _force);
            }
            catch (Exception err)
            {
                try
                {
                    // try to disable refilter only if something went wrong
                    SetRefilterOnly(__instance, false);
                }
                catch
                {
                    // At this point just give up, this error can be ignored only log the outer one
                }

                UnityEngine.Debug.LogException(err);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPostfix(CharaList __instance)
        {
            try
            {
                SetRefilterOnly(__instance, false);
                if (TryGetListEntry(__instance, out var entry)) entry.RefilterInProgress = false;
            }
            catch (Exception err)
            {
                UnityEngine.Debug.LogException(err);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static bool InitFemaleListPrefix(CharaList __instance)
        {
            try
            {
                return InitListPrefix(__instance);
            }
            catch (Exception err)
            {
                UnityEngine.Debug.LogException(err);
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static void InitFemaleLisPostfix(CharaList __instance)
        {
            try
            {
                InitListPostfix(__instance);
            }
            catch (Exception err)
            {
                UnityEngine.Debug.LogException(err);
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static bool InitMaleListPrefix(CharaList __instance)
        {
            try
            {
                return InitListPrefix(__instance);
            }
            catch (Exception err)
            {
                UnityEngine.Debug.LogException(err);
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static void InitMaleListPostfix(CharaList __instance)
        {
            try
            {
                InitListPostfix(__instance);
            }
            catch (Exception err)
            {
                UnityEngine.Debug.LogException(err);
            }
        }
    }

    public class CharaListEntry : BaseListEntry<CharaList>
    {
        public CharaListEntry(CharaList list) : base(list) { }
        public override bool isActiveAndEnabled => _list.isActiveAndEnabled;

        protected internal override string GetRoot()
        {
            return string.Concat(UserData.Path, GetSex() != 0 ? "chara/female" : "chara/male");
        }

        public override List<CharaFileInfo> GetCharaFileInfos()
        {
            return _list?.charaFileSort?.cfiList;
        }

        protected override int GetSex()
        {
            return _list.sex;
        }

        public override void InitListFolderChanged()
        {
            InitCharaList(true);
        }

        public override void InitListRefresh()
        {
            InitCharaList(true);
        }

        private void InitCharaList(bool force)
        {
            _list.InitCharaList(force);
        }
    }

    public class StudioCharaFoldersHelper : BaseStudioFoldersHelper<CharaListEntry, CharaList>
    {
        protected override CharaListEntry CreateNewListEntry(CharaList gameList)
        {
            return new CharaListEntry(gameList);
        }

        internal override int GetListEntryIndex(CharaList gameList)
        {
            return gameList.sex;
        }
    }
}