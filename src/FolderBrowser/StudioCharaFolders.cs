/*namespace SceneBrowserFolders
{

    /// <summary>
    /// Unused, the lists are loading cards from all subfolders and I didn't manage to do this easily
    /// Maybe someone someday will do something with this
    /// </summary>
    internal static class StudioCharaFolders
    {
        //private readonly bool _male;

        public static void Init()
        {
            //_male = male;
            HarmonyInstance.Create($"{LoadFolders.Guid}.{nameof(StudioCharaFolders)}").PatchAll(typeof(SceneFolders)); //.{(male ? "male" : "female")}
        }

        private static CharaFileSort GetCharaFileSort(CharaList __instance)
        {
            return (CharaFileSort)typeof(CharaList).GetField("charaFileSort", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
        }

        private static CharaList _maleListInstance;
        private static CharaList _femaleListInstance;

        [HarmonyPatch(typeof(CharaList), "Awake")]
        [HarmonyPrefix]
        public static void CharaListAwake(CharaList __instance)
        {
            var sex = (int)typeof(CharaList).GetField("sex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (sex == 1)
            {
                _femaleListInstance = __instance;

                //todo make universal for male / female since there are 2 lists separately
            }
            else
            {
                _maleListInstance = __instance;
                CurrentMaleFolder = GetDefaultMalePath();
            }
        }

        [HarmonyPatch(typeof(CharaList), "OnSelectChara")]
        [HarmonyPrefix]
        public static bool OnSelectChara(CharaList __instance, int _idx)
        {
            if (__instance == _maleListInstance)
            {
                var entry = GetCharaFileSort(_maleListInstance).cfiList.First(x => x.index == _idx);
                if (entry.name.EndsWith("\\"))
                {
                    CurrentMaleFolder = entry.file;
                    _maleListInstance.InitCharaList(true);
                    return false;
                }
            }
            else
            {

            }

            return true;
        }

        #region Female

        private static string GetDefaultFemalePath() => Path.Combine(Utils.GetPath(), @"chara\female");

        #endregion

        #region Male

        private static string _currentMaleRelativeFolder;
        private static string _currentMaleFolder;

        public static string CurrentMaleFolder
        {
            get => _currentMaleFolder;
            set
            {
                var lowVal = value.ToLower();
                if (_currentMaleFolder == lowVal) return;

                _currentMaleFolder = lowVal;
                _currentMaleRelativeFolder = _currentMaleFolder.Length > Utils.GetPath().Length ? _currentMaleFolder.Substring(Utils.GetPath().Length).TrimEnd('\\') : "";
                Logger.Log(LogLevel.Info, "CurrentMaleFolder " + _currentMaleFolder);
            }
        }

        private static string GetDefaultMalePath() => Path.Combine(Utils.GetPath(), @"chara\male");

        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        [HarmonyPostfix]
        public static void InitMaleList(CharaList __instance)
        {
            var s = GetCharaFileSort(__instance);
            var currentDir = new DirectoryInfo(CurrentMaleFolder);
            foreach (var subdir in currentDir.GetDirectories())
            {
                s.cfiList.Add(new CharaFileInfo(subdir.FullName, subdir.Name + "\\"));
            }

            if (currentDir.FullName.Length > GetDefaultMalePath().Length)
                s.cfiList.Add(new CharaFileInfo(currentDir.Parent.FullName, "..\\"));
        }

        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StudioInitInfoPatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "chara/male", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = sd();

                }

                yield return instruction;
            }
        }

        private static FieldInfo sd()
        {
            return typeof(StudioCharaFolders).GetField(nameof(_currentMaleRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
        }

        #endregion
    }

}*/