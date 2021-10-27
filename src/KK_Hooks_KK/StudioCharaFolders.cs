﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.StudioChara)]
    public class StudioCharaFolders : BaseStudioFolders<CharaListEntry, CharaList, StudioCharaFoldersHelper>, IFolderBrowser
    {
        public StudioCharaFolders()
        {
            WindowLabel = "Select folder with cards to view";
            RefreshLabel = "Refresh characters";
            Harmony.CreateAndPatchAll(typeof(StudioCharaFolders));
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
                catch { }

                Debug.LogException(err);
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
                Debug.LogException(err);
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
                Debug.LogException(err);
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
                Debug.LogException(err);
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
                Debug.LogException(err);
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
                Debug.LogException(err);
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