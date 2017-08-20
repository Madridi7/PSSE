using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pokemon_Shuffle_Save_Editor
{
    public class Database
    {
        #region Properties

        public byte[] MegaStone { get; private set; }
        public byte[] MonAbility { get; private set; }
        public byte[] MonData { get; private set; }
        public byte[] MonLevel { get; private set; }
        public byte[] MissionCard { get; private set; }
        public byte[] MessageDex { get; private set; }
        public byte[] StagesEvent { get; private set; }
        public byte[] StagesExpert { get; private set; }
        public byte[] StagesMain { get; private set; }
        public byte[] PokeLoad { get; private set; }

        public bool[][] HasMega { get; private set; }   // [X][0] = X, [X][1] = Y
        public int[] Forms { get; private set; }
        public List<int>[] Pokathlon { get; private set; }
        //public int[][] PokathlonRand { get; private set; }  // [step][0] = min, [step][1] = max
        public bool[][] Missions { get; private set; }
        public string[] MonsList { get; private set; }
        //public string[] PokathlonList { get; private set; }
        public string[] SpeciesList { get; private set; }
        public string[] SkillsList { get; private set; }
        public string[] SkillsTextList { get; private set; }
        public Tuple<int, int>[] Megas { get; private set; }    //monsIndex, speedups
        public Tuple<int, int, bool, int, int, int[], int, Tuple<int, int>>[] Mons { get; private set; }   //specieIndex, formIndex, isMega, raiseMaxLevel, basePower, skills, type, Rest
        public Tuple<int, int>[] Rest { get; private set; }  //stageNum, skillsCount
        public List<int> MegaList { get; private set; } //derivate a List from Megas.Item1 to use with IndexOf() functions (in UpdateForms() & UpdateOwnedBox())

        public int MegaStartIndex { get; private set; } // Indexes of first mega & second "---", respectively,...
        public int MonStopIndex { get; private set; }   //...should allow PSSE to work longer without needing an update.

        #endregion Properties

        public Database(bool shwmsg = false)
        {
            string[] filenames = { "megaStone.bin", "pokemonData.bin", "stageData.bin", "stageDataEvent.bin", "stageDataExtra.bin", "pokemonLevel.bin", "pokemonAbility.bin", "missionCard.bin", "messagePokedex_US.bin", "pokeLoad.bin" };
            string resourcedir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "resources" + Path.DirectorySeparatorChar;
            if (shwmsg)
            {
                string blabla = null;
                List<string> found = new List<string>();
                if (!Directory.Exists(resourcedir))
                    blabla = "No resources folder found.\nCreate a new folder in the same directory as PSSE and name it exactly \"resources\".\n";
                else
                {
                    blabla = "A \"resources\" folder has been found";
                    foreach (string file in filenames)
                        if (File.Exists(resourcedir + file)) { found.Add("\n\t" + file); }
                    if (found != null)
                    {
                        blabla += ".\n\nFiles found :";
                        found.Sort();
                        foreach (string str in found)
                            blabla += str;
                        blabla += "\n";
                    }
                    else blabla += ", but it looks empty.\n";
                }
                blabla += ("\nClick OK to use " + ((found == null) ? "built-in files" : "those files") + ", or use Abort to, well, abort.");
                var result = MessageBox.Show(blabla + "\nPlease click the Help button below for more informations.", "Resources scan", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, 0, "https://github.com/supercarotte/PSSE/wiki/Extract-needed-resource-files-from-the-game.");
                if (result != DialogResult.OK)
                    return;
            }

            //bin init
            MegaStone = Properties.Resources.megaStone;
            MissionCard = Properties.Resources.missionCard;
            MonAbility = Properties.Resources.pokemonAbility;
            MonData = Properties.Resources.pokemonData;
            MonLevel = Properties.Resources.pokemonLevel;
            StagesMain = Properties.Resources.stageData;
            StagesEvent = Properties.Resources.stageDataEvent;
            StagesExpert = Properties.Resources.stageDataExtra;
            MessageDex = Properties.Resources.messagePokedex_US;
            PokeLoad = Properties.Resources.pokeLoad;

            //resources override            
            if (Directory.Exists(resourcedir))
            {                
                for (int i = 0; i < filenames.Length; i++)
                {
                    if (File.Exists(resourcedir + filenames[i]))
                        switch (i) //don't forget that part or resources files won't override Database files, add an entry if a file is added above
                        {
                            case 0:
                                MegaStone = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 1:
                                MonData = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 2:
                                StagesMain = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 3:
                                StagesEvent = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 4:
                                StagesExpert = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 5:
                                MonLevel = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;

                            case 6:
                                MonAbility = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;
                            case 7:
                                MissionCard = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;
                            case 8:
                                MessageDex = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;
                            case 9:
                                PokeLoad = File.ReadAllBytes(resourcedir + filenames[i]);
                                break;
                            default:
                                MessageBox.Show("Error loading resources :\nfilename = " + (filenames[i] != null ? filenames[i] : "null") +"\ni = " + i);
                                break;
                        }
                }
                
            }
                

            //txt init
            SpeciesList = Properties.Resources.species.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            MonsList = Properties.Resources.mons.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            //PokathlonList = Properties.Resources.pokathlon.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            MegaStartIndex = MonsList.ToList().IndexOf("Mega Venusaur");
            MonStopIndex = MonsList.ToList().IndexOf("---", 1);

            //megas
            int entrylen = BitConverter.ToInt32(MonData, 0x4);
            Megas = new Tuple<int, int>[BitConverter.ToUInt32(MegaStone, 0) - 1];
            for (int i = 0; i < Megas.Length; i++)
            {
                int monIndex = BitConverter.ToUInt16(MegaStone, MegaStone[0x10] + (i+1) * 4) & 0x3FF;
                string str = "Mega " + MonsList[monIndex];
                if (monIndex == 6 || monIndex == 150)
                    str += (monIndex != (BitConverter.ToUInt16(MegaStone, MegaStone[0x10] + i * 4) & 0x3FF)) ? " X" : " Y";
                byte[] data = MonData.Skip(0x50 + entrylen * MonsList.ToList().IndexOf(str)).Take(entrylen).ToArray();
                int maxSpeedup = (BitConverter.ToInt32(data, 0xA) >> 7) & 0x7F;
                Megas[i] = new Tuple<int, int>(monIndex, maxSpeedup);
            }
            MegaList = new List<int>();
            for (int i = 0; i < Megas.Length; i++)
                MegaList.Add(Megas[i].Item1);
            HasMega = new bool[MonsList.Length][];
            for (int i = 0; i < MonsList.Length; i++)
                HasMega[i] = new bool[2];
            for (int i = 0; i < Megas.Length; i++)
                HasMega[BitConverter.ToUInt16(MegaStone, 0x54 + i * 4) & 0x3FF][(MegaStone[0x54 + (i * 4) + 1] >> 3) & 1] = true;

            //pokemons
            Forms = new int[SpeciesList.Length];
            Mons = new Tuple<int, int, bool, int, int, int[], int, Tuple<int, int>>[BitConverter.ToUInt32(MonData, 0)];
            Rest = new Tuple<int, int>[Mons.Length];
            for (int i = 0; i < Mons.Length; i++)
            {
                byte[] data = MonData.Skip(0x50 + entrylen * i).Take(entrylen).ToArray();
                bool isMega = i >= MegaStartIndex && i <= MonsList.Count() - 1;
                int spec = (isMega && i <= MegaStartIndex + Megas.Length - 1)
                    ? SpeciesList.ToList().IndexOf(MonsList[Megas[i - MegaStartIndex].Item1].Substring(0, (MonsList[Megas[i - MegaStartIndex].Item1].LastIndexOf(' ') <= 0) ? MonsList[Megas[i - MegaStartIndex].Item1].Length : MonsList[Megas[i - MegaStartIndex].Item1].LastIndexOf(' ')))
                    : (BitConverter.ToInt32(data, 0xE) >> 6) & 0x7FF;
                int raiseMaxLevel = (BitConverter.ToInt16(data, 0x4)) & 0x3F;
                int basePower = (BitConverter.ToInt16(data, 0x3)) & 0x7; //ranges 1-7 for now (30-90 BP), may need an update later on
                int[] skillsadr = new int[] { 0x02, 0x20, 0x21, 0x22, 0x23 }, skill = new int[skillsadr.Length];
                int j = 0, skillCount = 0;
                foreach (int adr in skillsadr)
                {
                    skill[j] = data[adr] & 0x7F; //ranges 1-~130 for now, ordered list in MESSAGE_XX/message_PokedexXX.bin ("Opportunist" to "Transform" then a bunch more with a lot of placeholders)
                    if (skill[j] != 0) { skillCount++; }
                    j++;
                }
                skillCount = Math.Max(skillCount, 1);
                int type = (BitConverter.ToInt16(data, 0x01) >> 3) & 0x1F; //ranges 0-17 (normal - fairy) (https://gbatemp.net/threads/psse-pokemon-shuffle-save-editor.396499/page-33#post-6278446)
                int index = (BitConverter.ToInt16(data, 0)) & 0x3FF; //ranges 1-999, it's the number you can see on the team selection menu
                Rest[i] = new Tuple<int, int>(index, skillCount); //Mons has more than 7 arguments so 8th one and beyond have to be included in another Tuple
                Mons[i] = new Tuple<int, int, bool, int, int, int[], int, Tuple<int, int>>(spec, Forms[spec], isMega, raiseMaxLevel, basePower, skill, type, Rest[i]);
                Forms[spec]++;
            }

            //Survival mode
            int smEntry = BitConverter.ToInt32(PokeLoad, 0x4), smSkip = BitConverter.ToInt32(PokeLoad, 0x10), smTake = BitConverter.ToInt32(PokeLoad, 0x14);
            Pokathlon = new List<int>[BitConverter.ToInt16(PokeLoad.Skip(smSkip + smTake - smEntry).Take(smEntry).ToArray(), 0) & 0x3FF]; //# of entries doesn't match # of steps since some are collided so I take the last entry and read its 'lowStep' value (should compare to 'highStep' but I don't want to overcomplicate thigns for now)
            for (int i = 0; i < BitConverter.ToInt32(PokeLoad, 0); i++)
            {
                byte[] data = PokeLoad.Skip(smSkip + i * smEntry).Take(smEntry).ToArray();
                int lowStep = BitConverter.ToInt16(data, 0) & 0x3FF, highStep = (BitConverter.ToInt16(data, 0x01) >> 2) & 0x3FF; //if highStep !=0 then data[] applies to all steps in the lowStep - highStep range
                int min = (BitConverter.ToInt16(data, 0x02) >> 4) & 0xFFF, max = BitConverter.ToInt16(data, 0x04) & 0xFFF; //if max !=0 then all stages in min-max range are possibilities for corresponding step(s)
                List<int> stagesList = Enumerable.Range(min, max != 0 ? max-min+1 : 1).ToList();
                for (int j = 0x08; j < (data.Length - 3); j += 4) //weird pattern for excluded stages : each 32-bits block starting at 0x08 contains 3 10-bits long stages #
                {
                    int exception = 0;
                    for (int w = 0; w < 3; w++)
                    {
                        exception = (BitConverter.ToInt32(data, j) >> (w * 10)) & 0x3FF;
                        if (exception == 0)
                            break;
                        else if (stagesList.Contains(exception))
                            stagesList.Remove(exception);
                    }
                    if (exception == 0)
                        break;
                }
                foreach (int step in Enumerable.Range(lowStep, 1 + Math.Max(0, highStep - lowStep)))
                    Pokathlon[step - 1] = stagesList;
            }

            #region old Survival
            //pokathlon
            //PokathlonRand = new int[PokathlonList.Length / 2][];
            //for (int i = 0; i < PokathlonRand.Length; i++)
            //{
            //    PokathlonRand[i] = new int[2];
            //    Int32.TryParse(PokathlonList[2 * i], out PokathlonRand[i][0]);
            //    Int32.TryParse(PokathlonList[1 + 2 * i], out PokathlonRand[i][1]);
            //}
            #endregion

            //missions
            Missions = new bool[BitConverter.ToInt32(MissionCard, 0)][];
            for(int i = 0 ; i < Missions.Length; i++)
            {
                Missions[i] = new bool[10];
                int ientrylen = BitConverter.ToInt32(MissionCard, 0x4);
                byte[] data = MissionCard.Skip(BitConverter.ToInt32(MissionCard, 0x10) + i * ientrylen).Take(ientrylen).ToArray();
                for (int j = 0; j < Missions[i].Length; j++)
                    Missions[i][j] = BitConverter.ToInt16(data, 0x8 + 2 * j) != 0;
            }

            //dictionnary (new)
            string temp = Encoding.Unicode.GetString(MessageDex.Skip(BitConverter.ToInt32(MessageDex, 0x08)).Take(BitConverter.ToInt32(MessageDex, 0x0C) - 0x17).ToArray()); //Relevant chunk specified in .bin file, UTF16 Encoding, 17 bytes at the end are a useless stamp (data.messagePokedex)
            temp = temp.Replace(Encoding.Unicode.GetString(MessageDex.Skip(BitConverter.ToInt32(MessageDex, 0x08)).Take(0x10).ToArray()), "[name]"); //because this variable ends with 0x00 it messes with Split() later on, so I replace it here
            temp = temp.Replace(Encoding.Unicode.GetString(new byte[] { 0x01, 0x00, 0x03, 0x01, 0x01, 0x00, 0x03, 0x00, 0x05, 0x00, 0x6D, 0x65, 0x67, 0x61, 0x4E, 0x61, 0x6D, 0x65, 0x00, 0x00 }), "[megaName]"); //same but this variable isn't declared on a fixed position so I copied it directly
            string[] arr = temp.Split( (char)0x00); //split the single string in an array
            arr = arr.Skip(Array.IndexOf(arr, "Opportunist")).ToArray(); //we only care for skills so I get rid of anything before Opportunist
            for (int i = 0; i < arr.Length; i++)
            {
                if (String.IsNullOrEmpty(arr[i]))
                    arr[i] = "-Placeholder-"; //make sure there is no empty strings just in case
            }

            /* This code below separates Skills entries from Text entries while ignoring a few mega-skills entries
             * Right now (1.4.19) the list of strings looks like that : [Skills1][Text for Skills1][Text for mega skills][Skills2][Text for Skills2]
             * It shouldn't be a problem is more skills are added to [Skills2] (after all placeholders have been filled), but if another [Text for mega skills] is ever added this will need a 3rd string to concatenate
             * Also, note that there is no [Mega Skills], so if I ever want to implement them the same way I did normal skills another resource file will be needed.
             */

            int a = Array.IndexOf(arr, "Opportunist"), b = Array.IndexOf(arr, "Rarely, attacks can deal\ngreater damage than usual."), c = Array.IndexOf(arr, "Big Wave"), d = Array.IndexOf(arr, "Increases damage done by\nany Water types in a combo.");
            string[] s1 = arr.Skip(a).Take(b - a).ToArray(), s2 = arr.Skip(c).Take(d - c).ToArray(), Skills = new string[s1.Length + s2.Length];
            string[] st1 = arr.Skip(b).Take(b - a).ToArray(), st2 = arr.Skip(d).Take(d - c).ToArray(), SkillsT = new string[st1.Length + st2.Length];
            s1.CopyTo(Skills, 0);
            s2.CopyTo(Skills, s1.Length);
            SkillsList = Skills;
            st1.CopyTo(SkillsT, 0);
            st2.CopyTo(SkillsT, s1.Length);
            SkillsTextList = SkillsT;
        }
    }
}