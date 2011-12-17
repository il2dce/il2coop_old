// IL2COOP: A co-op lobby for IL-2 Sturmovik: Cliffs of Dover
// Copyright (C) 2011 Stefan Rothdach
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using maddox.game;
using maddox.game.world;

//$debug

public class Mission : AMission
{
    internal enum MenuID
    {
        HostMainMenu,
        ClientMainMenu,
        SelectMissionMenu,
        SelectAircraftMenu,
    }
    
    private ISectionFile selectedMissionFile = null;
    private string selectedMissionFileName = null;
    private string selectedMissionFileShortName = null;

    private List<string> aircrafts = new List<string>();
    private Dictionary<string, string> aircraftTypes = new Dictionary<string, string>();
    private Dictionary<string, string> aircraftNames = new Dictionary<string, string>();
    private Dictionary<string, Player> aircraftSelections = new Dictionary<string, Player>();
    private Dictionary<Player, int> offsets = new Dictionary<Player, int>();

    private string createMissionFileDisplayName(string missionFileName)
    {
        return missionFileName.Replace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions", "");
    }

    private string createAircraftDisplayName(string aircraft)
    {
        return parseAirgroupName(aircraft) + ": " + aircraftNames[aircraft] + " (" + aircraftTypes[aircraft].Replace("Aircraft.", "") + ")";
    }

    private string createAirGroupDisplayName(string airGroupName)
    {
        // Fallback
        string airGroupDisplayName = airGroupName;

        if(airGroupName.Contains("LONDON"))
        {
            airGroupDisplayName += "London Flying Training School";
        }
        else if(airGroupName.StartsWith("BoB_RAF"))
        {
            if (airGroupName.Contains("FatCat"))
            {
                airGroupDisplayName = "FatCat Squadron RAF";
            }
            else
            {
                Match match = Regex.Match(airGroupName, @"(\d+)");
                if (match.Success)
                {
                    airGroupDisplayName = "No. ";
                    airGroupDisplayName += match.Value;
                    airGroupDisplayName += " Squadron";

                    if (airGroupName.Contains("RCAF"))
                    {
                        airGroupDisplayName += " RCAF";
                    }
                    else
                    {
                        airGroupDisplayName += " RAF";
                    }
                }
            }
        }
        else if (airGroupName.StartsWith("BoB_LW"))
        {
            if(airGroupName.Contains("BoB_LW_AufklGr_ObdL"))
            {
                airGroupDisplayName = "Aufklärungsgruppe ObdL";
            }
            else if(airGroupName.Contains("BoB_LW_AufklGr10"))
            {
                airGroupDisplayName = "Aufklärungsgruppe 10";
            }
            else if(airGroupName.Contains("BoB_LW_Wekusta_51"))
            {
                airGroupDisplayName = "Wekusta 51";
            }
            else if (airGroupName.Contains("BoB_LW_Wekusta_ObdL"))
            {
                airGroupDisplayName = "Wekusta ObdL";
            }
            else
            {
                Match match = Regex.Match(airGroupName, @"(\d+)");
                if (match.Success)
                {
                    if(airGroupName.Contains("Stab"))
                    {
                        airGroupDisplayName = "Stab/";
                    }
                    else
                    {
                        airGroupDisplayName = "x./";
                    }
                                        
                    if (airGroupName.Contains("JG"))
                    {
                        airGroupDisplayName += "JG";
                    }
                    else if (airGroupName.Contains("ZG"))
                    {
                        airGroupDisplayName += "ZG";
                    }
                    else if (airGroupName.Contains("LG"))
                    {
                        airGroupDisplayName += "LG";
                    }
                    else if (airGroupName.Contains("StG"))
                    {
                        airGroupDisplayName += "StG";
                    }
                    else if (airGroupName.Contains("KG"))
                    {
                        airGroupDisplayName += "KG";
                    }
                    else if (airGroupName.Contains("KGzbV"))
                    {
                        airGroupDisplayName += "KGzbV";
                    }
                    else if (airGroupName.Contains("AufklGr"))
                    {
                        airGroupDisplayName += "AufklGr";
                    }

                    airGroupDisplayName += " " + match.Value;
                }
            }
        }

        return airGroupDisplayName;
    }

    internal string parseAirgroupName(string airGroupName)
    {
        string[] tempString = null;
        string parsedName = "";
        tempString = airGroupName.Split('.');

        if (tempString[0].StartsWith("BoB_LW_"))
        {
            StringBuilder b = new StringBuilder(tempString[0]);
            parsedName = b.Replace("BoB_LW_", "").ToString();

            if (parsedName.EndsWith("_I"))
                parsedName = "I./" + b.Replace("_I", "").ToString();
            else if (parsedName.EndsWith("_II"))
                parsedName = "II./" + b.Replace("_II", "").ToString();
            else if (parsedName.EndsWith("_III"))
                parsedName = "III./" + b.Replace("_III", "").ToString();
            else if (parsedName.EndsWith("_IV"))
                parsedName = "IV./" + b.Replace("_IV", "").ToString();
            else if (parsedName.EndsWith("_V"))
                parsedName = "V./" + b.Replace("_V", "").ToString();
            else if (parsedName.EndsWith("_Stab"))
                parsedName = "Stab./" + b.Replace("_Stab", "").ToString();

            if (tempString[1].StartsWith("0"))
            {
                if (!parsedName.StartsWith("Stab"))
                    parsedName += " (Stabstaffel)";
            }
            else if (tempString[1].StartsWith("1"))
                parsedName += " (1. Staffel)";
            else if (tempString[1].StartsWith("2"))
                parsedName += " (2. Staffel)";
            else if (tempString[1].StartsWith("3"))
                parsedName += " (3. Staffel)";
            else if (tempString[1].StartsWith("4"))
                parsedName += " (4. Staffel)";
            else parsedName += " (Unknown)";
        }
        else if (tempString[0].StartsWith("BoB_RAF_"))
        {
            StringBuilder b = new StringBuilder(tempString[0]);

            if (tempString[0].StartsWith("BoB_RAF_F_FatCat"))
                parsedName = b.Replace("BoB_RAF_F_", "(F)  ").ToString();
            else if (tempString[0].StartsWith("BoB_RAF_F_"))
                parsedName = b.Replace("BoB_RAF_F_", "(F)  No. ").ToString();
            else if (tempString[0].StartsWith("BoB_RAF_B_"))
                parsedName = b.Replace("BoB_RAF_B_", "(B)  No. ").ToString();
            if (parsedName.EndsWith("_Early"))
                parsedName = b.Replace("_Early", "").ToString();
            else if (parsedName.EndsWith("_Late"))
                parsedName = b.Replace("_Late", "").ToString();
            if (parsedName.EndsWith("Sqn"))
                parsedName = b.Replace("Sqn", ".Sqn").ToString();
            else if (parsedName.EndsWith("_PL"))
                parsedName = b.Replace("_PL", ".Sqn (PL)").ToString();
            else if (parsedName.EndsWith("_CZ"))
                parsedName = b.Replace("_CZ", ".Sqn (CZ)").ToString();
            else if (parsedName.EndsWith("_RCAF"))
                parsedName = b.Replace("_RCAF", ".Sqn RCAF").ToString();

            if (tempString[1].StartsWith("0"))
                parsedName += " (1)";
            else if (tempString[1].StartsWith("1"))
                parsedName += " (2)";
            else if (tempString[1].StartsWith("2"))
                parsedName += " (3)";
            else if (tempString[1].StartsWith("3"))
                parsedName += " (4)";
            else parsedName += " (Unknown)";
        }

        return parsedName;
    }

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);

        if (actor is AiAircraft)
        {
            AiAircraft aircraft = actor as AiAircraft;
            string aircraftName = actor.Name().Replace(missionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + ":", "");

            if(aircraftSelections.ContainsKey(aircraftName))
            {
                aircraftSelections[aircraftName].PlaceEnter(aircraft, 0);
            }
        }
    }


    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        if (ID == (int)MenuID.HostMainMenu)
        {
            if (menuItemIndex == 1)
            {
                setSelectMissionMenu(player);
            }
            else if (menuItemIndex == 2)
            {
                setSelectAircraftMenu(player);
            }
            else if (menuItemIndex == 3)
            {
                startMission(player);
            }
        }
        else if (ID == (int)MenuID.ClientMainMenu)
        {
            if (menuItemIndex == 1)
            {
                setSelectAircraftMenu(player);
            }
        }
        else if (ID == (int)MenuID.SelectMissionMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    offsets[player] = offsets[player] - 1;
                    setSelectMissionMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    offsets[player] = offsets[player] + 1;
                    setSelectMissionMenu(player);
                }
                else
                {
                    string missionsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions";
                    string[] missionFileNames = Directory.GetFiles(missionsFolderPath, "*.mis", SearchOption.AllDirectories);

                    if (menuItemIndex - 1 + (offsets[player] * 7) < missionFileNames.Length)
                    {
                        string missionFileName = missionFileNames[menuItemIndex - 1 + (offsets[player] * 7)];
                        selectedMissionFileName = missionFileName;
                        selectedMissionFileShortName = createMissionFileDisplayName(missionFileName);
                        selectedMissionFile = GamePlay.gpLoadSectionFile(missionFileName);
                        GamePlay.gpLogServer(new Player[] { player }, "Mission selected: " + selectedMissionFileShortName, null);

                        parseSelectedMissionFile();

                        setMainMenu(player);
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
        else if (ID == (int)MenuID.SelectAircraftMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    offsets[player] = offsets[player] - 1;
                    setSelectAircraftMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    offsets[player] = offsets[player] + 1;
                    setSelectAircraftMenu(player);
                }
                else
                {
                    if (menuItemIndex - 1 + (offsets[player] * 7) < aircrafts.Count)
                    {
                        if (!aircraftSelections.ContainsKey(aircrafts[menuItemIndex - 1 + (offsets[player] * 7)]))
                        {
                            aircraftSelections[aircrafts[menuItemIndex - 1 + (offsets[player] * 7)]] = player;
                            GamePlay.gpLogServer(new Player[] { player }, "Aircraft selected: " + createAircraftDisplayName(aircrafts[menuItemIndex - 1 + (offsets[player] * 7)]), null);

                            setMainMenu(player);
                        }
                        else
                        {
                            GamePlay.gpLogServer(new Player[] { player }, "Aircraft already occupied.", null);

                            setSelectAircraftMenu(player);
                        }
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }        
    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        setMainMenu(player);
    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
    
        MissionNumberListener = -1;        
    }

    public override void OnPlayerArmy(Player player, int army)
    {
        base.OnPlayerArmy(player, army);

        setDummyAircraft(player);
    }
    
    private void setDummyAircraft(Player player)
    {
        // Place player into a dummy aircraft.
        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            for (int armyIndex = 0; armyIndex < GamePlay.gpArmies().Length; armyIndex++)
            {
                if (GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]) != null && GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]).Length > 0)
                {
                    for (int airGroupIndex = 0; airGroupIndex < GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]).Length; airGroupIndex++)
                    {
                        AiAirGroup airGroup = GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex])[airGroupIndex];
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            for (int aircraftIndex = 0; aircraftIndex < airGroup.GetItems().Length; aircraftIndex++)
                            {
                                AiActor actor = airGroup.GetItems()[aircraftIndex];
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    for (int placeIndex = 0; placeIndex < aircraft.Places(); placeIndex++)
                                    {
                                        if (aircraft.Player(placeIndex) == null)
                                        {
                                            player.PlaceEnter(aircraft, placeIndex);
                                            // Place found.
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            GamePlay.gpLogServer(new Player[] { player }, "No place available.", null);
        }
    }

    private void setMainMenu(Player player)
    {
        offsets[player] = 0;

        if (GamePlay.gpPlayer() != null && player == GamePlay.gpPlayer())
        {
            // Set host menu.
            if (selectedMissionFileShortName == null)
            {
                GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Select Mission" }, new bool[] { true });                
            }
            else
            {
                string selectedAircraft = null;
                foreach (string aircraft in aircraftSelections.Keys)
                {
                    if (aircraftSelections[aircraft] == player)
                    {
                        selectedAircraft = aircraft;
                        break;
                    }
                }

                if (selectedAircraft == null)
                {
                    GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Select Aircraft", "Start Mission" }, new bool[] { true, true, false });
                }
                else
                {
                    GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Selected Aircraft: " + selectedAircraft, "Start Mission" }, new bool[] { true, true, false });
                }
            }
        }
        else
        {
            // Set client menu.
            if (selectedMissionFileShortName != null)
            {
                string selectedAircraft = null;
                foreach (string aircraft in aircraftSelections.Keys)
                {
                    if (aircraftSelections[aircraft] == player)
                    {
                        selectedAircraft = aircraft;
                        break;
                    }
                }

                if (selectedAircraft == null)
                {
                    GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Select Aircraft" }, new bool[] { true });
                }
                else
                {
                    GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Selected Aircraft: " + selectedAircraft }, new bool[] { true });
                }
            }
        }
    }

    private void setSelectMissionMenu(Player player)
    {
        string missionsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions";
        string[] missionFileNames = Directory.GetFiles(missionsFolderPath, "*.mis", SearchOption.AllDirectories);

        if (offsets[player] < 0)
        {
            offsets[player] = (int)missionFileNames.Length / 7;            
        }
        else if ((offsets[player] * 7) > missionFileNames.Length)
        {
            offsets[player] = 0;
        }
        
        int entryCount= 9;
        string[] entry = new string[entryCount];
        bool[] hasSubEntry = new bool[entryCount];

        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (entryIndex == entryCount - 2)
            {
                entry[entryIndex] = "Page up";
                hasSubEntry[entryIndex] = true;
            }
            else if (entryIndex == entryCount - 1)
            {
                entry[entryIndex] = "Page down";
                hasSubEntry[entryIndex] = true;
            }
            else
            {
                if (entryIndex + (offsets[player] * 7) < missionFileNames.Length)
                {
                    entry[entryIndex] = createMissionFileDisplayName(missionFileNames[entryIndex + (offsets[player] * 7)]);
                    hasSubEntry[entryIndex] = false;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }                
            }
        }
        
        GamePlay.gpSetOrderMissionMenu(player, true, (int)MenuID.SelectMissionMenu, entry, hasSubEntry);
    }
    
    private void setSelectAircraftMenu(Player player)
    {
        if (offsets[player] < 0)
        {
            offsets[player] = (int)aircrafts.Count / 7;
        }
        else if ((offsets[player] * 7) > aircrafts.Count)
        {
            offsets[player] = 0;
        }

        int entryCount = 9;
        string[] entry = new string[entryCount];
        bool[] hasSubEntry = new bool[entryCount];

        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (entryIndex == entryCount - 2)
            {
                entry[entryIndex] = "Page up";
                hasSubEntry[entryIndex] = true;
            }
            else if (entryIndex == entryCount - 1)
            {
                entry[entryIndex] = "Page down";
                hasSubEntry[entryIndex] = true;
            }
            else
            {
                if (entryIndex + (offsets[player] * 7) < aircrafts.Count)
                {
                    if (aircraftSelections.ContainsKey(aircrafts[entryIndex + (offsets[player] * 7)]) && aircraftSelections[aircrafts[entryIndex + (offsets[player] * 7)]] != null)
                    {
                        entry[entryIndex] = createAircraftDisplayName(aircrafts[entryIndex + (offsets[player] * 7)]) + ": " + aircraftSelections[aircrafts[entryIndex + (offsets[player] * 7)]].Name();
                        hasSubEntry[entryIndex] = true;
                    }
                    else
                    {
                        entry[entryIndex] = createAircraftDisplayName(aircrafts[entryIndex + (offsets[player] * 7)]);
                        hasSubEntry[entryIndex] = false;
                    }                    
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MenuID.SelectAircraftMenu, entry, hasSubEntry);
    }

    private void parseSelectedMissionFile()
    {
        if (selectedMissionFile != null)
        {
            aircrafts.Clear();
            aircraftSelections.Clear();

            for (int airGroupIndex = 0; airGroupIndex < selectedMissionFile.lines("AirGroups"); airGroupIndex++)
            {
                string key;
                string value;
                selectedMissionFile.get("AirGroups", airGroupIndex, out key, out value);

                string aircraftType = selectedMissionFile.get(key, "Class");

                // Remove the flight mask
                string airGroupName = key.Substring(0, key.Length - 1);

                for (int flightIndex = 0; flightIndex < 4; flightIndex++)
                {
                    if (selectedMissionFile.exist(key, "Flight" + flightIndex.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat)))
                    {
                        string acNumberLine = selectedMissionFile.get(key, "Flight" + flightIndex.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
                        string[] acNumberList = acNumberLine.Split(new char[] { ' ' });
                        if (acNumberList != null && acNumberList.Length > 0)
                        {
                            for (int aircraftIndex = 0; aircraftIndex < acNumberList.Length; aircraftIndex++)
                            {
                                string aircraft = airGroupName + flightIndex.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + aircraftIndex.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                                aircrafts.Add(aircraft);
                                aircraftTypes[aircraft] = aircraftType;
                                aircraftNames[aircraft] = acNumberList[aircraftIndex];
                            }
                        }
                    }
                }
            }
        }
    }

    private void startMission(Player player)
    {
        if (selectedMissionFile != null)
        {
            // Destroy all dummy aircraft.
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                for (int armyIndex = 0; armyIndex < GamePlay.gpArmies().Length; armyIndex++)
                {
                    if (GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]) != null && GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]).Length > 0)
                    {
                        for (int airGroupIndex = 0; airGroupIndex < GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex]).Length; airGroupIndex++)
                        {
                            AiAirGroup airGroup = GamePlay.gpAirGroups(GamePlay.gpArmies()[armyIndex])[airGroupIndex];
                            if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                            {
                                for (int aircraftIndex = 0; aircraftIndex < airGroup.GetItems().Length; aircraftIndex++)
                                {
                                    AiActor actor = airGroup.GetItems()[aircraftIndex];
                                    if (actor is AiAircraft)
                                    {
                                        AiAircraft aircraft = actor as AiAircraft;
                                        aircraft.Destroy();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Load the selected mission file.
            GamePlay.gpPostMissionLoad(selectedMissionFile);
        }
        else
        {
            GamePlay.gpLogServer(new Player[] { player }, "No mission selected.", null);
        }
    }
}
