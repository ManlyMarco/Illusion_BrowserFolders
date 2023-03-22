using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;
using static Studio.MPCharCtrl;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.StudioOutfit)]
    public class StudioOutfitFolders : BaseStudioFolders<CostumeInfoEntry, CostumeInfo, StudioOutfitFoldersHelper>,
        IFolderBrowser
    {
        public StudioOutfitFolders()
        {
            WindowLabel = "Folder with outfits to view";
            RefreshLabel = "Refresh outfits";
            Harmony.CreateAndPatchAll(typeof(StudioOutfitFolders));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CostumeInfo), nameof(CostumeInfo.InitList))]
        internal static void InitCostumeListPrefix(CostumeInfo __instance, int _sex, ref int __state)
        {
            var origSex = __instance.sex;
            __state = _sex;
            try
            {
                SetRefilterOnly(__instance, __instance.sex == _sex);
                // if _sex == __instance.sex InitCostumeList does nothing, set to invalid here, restore after
                __instance.sex = -1;
            }
            catch (Exception err)
            {
                __instance.sex = origSex;
                try
                {
                    // try to disable refilter only if something went wrong
                    SetRefilterOnly(__instance, false);
                }
                catch { }
                Debug.LogException(err);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CostumeInfo), nameof(CostumeInfo.InitList))]
        internal static void InitCostumeListPostfix(CostumeInfo __instance, int __state)
        {
            try
            {
                __instance.sex = __state;
                SetRefilterOnly(__instance, false);
                if (TryGetListEntry(__instance, out var entry)) entry.RefilterInProgress = false;
            }
            catch (Exception err)
            {
                Debug.LogException(err);
            }
        }

     
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CostumeInfo), nameof(CostumeInfo.InitFileList))]
        internal static bool InitFileListPrefix(CostumeInfo __instance)
        {
            try
            {
                return InitListPrefix(__instance);
            }
            catch (Exception err)
            {
                Debug.LogException(err);
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CostumeInfo), nameof(CostumeInfo.InitFileList))]
        internal static void InitFileListPostfix(CostumeInfo __instance)
        {
            try
            {
                InitListPostfix(__instance);
            }
            catch (Exception err)
            {
                Debug.LogException(err);
            }
        }
    }

    public class CostumeInfoEntry : BaseListEntry<CostumeInfo>
    {
        public CostumeInfoEntry(CostumeInfo list) : base(list) { }

        public override bool isActiveAndEnabled => _list != null && _list.objRoot != null && _list.objRoot.activeSelf;

        protected internal override string GetRoot()
        {
            return string.Concat(UserData.Path, "coordinate");
        }

        public override List<CharaFileInfo> GetCharaFileInfos()
        {
            return _list?.fileSort?.cfiList;
        }

        protected override int GetSex()
        {
            return _list.sex;
        }

        public override void InitListFolderChanged()
        {
            _list.InitList(GetSex());
        }

        public override void InitListRefresh()
        {
            _list.InitList(GetSex());
        }
    }

    public class StudioOutfitFoldersHelper : BaseStudioFoldersHelper<CostumeInfoEntry, CostumeInfo>
    {
        protected override CostumeInfoEntry CreateNewListEntry(CostumeInfo gameList)
        {
            return new CostumeInfoEntry(gameList);
        }

        // KK really only has one of these, so always return the same one
        internal override int GetListEntryIndex(CostumeInfo gameList)
        {
            return -1;
        }
    }
}
