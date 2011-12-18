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
        PlayerMenu,
    }

    private ISectionFile selectedMissionFile = null;
    private string selectedMissionFileName = null;
    private string selectedMissionFileShortName = null;

    private List<string> aircrafts = new List<string>();
    private Dictionary<string, string> aircraftTypes = new Dictionary<string, string>();
    private Dictionary<string, string> aircraftNames = new Dictionary<string, string>();
    private Dictionary<string, string> places = new Dictionary<string, string>();
    private Dictionary<string, Player> aircraftSelections = new Dictionary<string, Player>();    
    private List<Player> ready = new List<Player>();

    private Dictionary<Player, int> offsets = new Dictionary<Player, int>();

    private bool isRunning = false;

    private string createMissionFileDisplayName(string missionFileName)
    {
        return missionFileName.Replace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions", "");
    }

    private string createAircraftDisplayName(string aircraft)
    {
        return aircraft.Replace("." + places[aircraft], "") + ": " + aircraftNames[aircraft] + " (" + aircraftTypes[aircraft].Replace("Aircraft.", "") + " " + places[aircraft] + ")";
    }

    private string createAirGroupDisplayName(string airGroupName)
    {
        // Fallback
        string airGroupDisplayName = airGroupName;

        if(airGroupName.Contains("LONDON"))
        {
            airGroupDisplayName += "London Flying Training School";
        }
        else if(airGroupName.StartsWith("BoB_RAF_"))
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
        else if (airGroupName.StartsWith("BoB_LW_"))
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
        else if(airGroupName.StartsWith("BoB_RA_"))
        {

        }

        return airGroupDisplayName;
    }
    
    private string[] getPlaces(string aircraftType)
    {
        if(aircraftType == "Aircraft.Bf-109E-1")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.Bf-109E-3")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.Bf-109E-3B")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.Bf-109E-4")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.Bf-109E-4B")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.G50")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.SpitfireMkI_Heartbreaker")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.HurricaneMkI_dH5-20")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.HurricaneMkI")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.SpitfireMkI")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.SpitfireMkIa")
        {
            return new string[] { "Pilot" };
        }
        else if (aircraftType == "Aircraft.SpitfireMkIIa")
        {
            return new string[] { "Pilot" };
        }
        else if(aircraftType == "Aircraft.Ju-87B-2")
        {
            return new string[] { "Pilot", "Gunner" };
        }
        else if(aircraftType == "Aircraft.Bf-110C-4")
        {
            return new string[] { "Pilot", "Gunner" };
        }
        else if(aircraftType == "Aircraft.Bf-110C-7")
        {
            return new string[] { "Pilot", "Gunner" };
        }
        else if(aircraftType == "Aircraft.Bf-110C-7")
        {
            return new string[] { "Pilot", "Gunner" };
        }
        else if(aircraftType == "Aircraft.DH82A")
        {
            return new string[] { "Pilot", "Co-Pilot" };
        }
        else if(aircraftType == "Aircraft.BlenheimMkIV")
        {
            return new string[] { "Pilot", "Bombardier", "Gunner" };
        }
        else if(aircraftType == "Aircraft.BR-20M")
        {
            return new string[] { "Pilot", "Co-Pilot", "Bombardier", "Nose Gunner", "Top Gunner", "Observer" };
        }
        else if (aircraftType == "Aircraft.Ju-88A-1")
        {
            return new string[] { "Pilot", "Bombardier", "Nose Gunner", "Top Gunner", "Ventral Gunner" };
        }
        else if (aircraftType == "Aircraft.He-111H-2")
        {
            return new string[] { "Pilot", "Bombardier", "Nose Gunner Down", "Nose Gunner Up", "Top Gunner", "Ventral Gunner", "Waist Gunner Left", "Waist Gunner Right" };
        }
        else if (aircraftType == "Aircraft.He-111P-2")
        {
            return new string[] { "Pilot", "Bombardier", "Nose Gunner", "Top Gunner", "Ventral Gunner", "Waist Gunner Left", "Waist Gunner Right" };
        }
        else
        {
            return null;
        }
    }

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);

        if (actor is AiAircraft)
        {
            AiAircraft aircraft = actor as AiAircraft;
            string aircraftName = actor.Name().Replace(missionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + ":", "");

            foreach (string aircraftKey in aircraftSelections.Keys)
            {
                if (aircraftKey.StartsWith(aircraftName + "."))
                {
                    string place = aircraftKey.Replace(aircraftName + ".", "");
                    if (getPlaces(aircraftTypes[aircraftKey]) != null && getPlaces(aircraftTypes[aircraftKey]).Length > 0)
                    {
                        for (int placeIndex = 0; placeIndex < getPlaces(aircraftTypes[aircraftKey]).Length; placeIndex++)
                        {
                            if (getPlaces(aircraftTypes[aircraftKey])[placeIndex] == place)
                            {
                                aircraftSelections[aircraftKey].PlaceEnter(aircraft, placeIndex);
                            }
                        }
                    }
                }
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
            if (menuItemIndex == 3)
            {
                if (!ready.Contains(player))
                {
                    ready.Add(player);
                }
                else
                {
                    ready.Remove(player);
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 4)
            {
                setPlayerMenu(player);
            }
            else if (menuItemIndex == 5)
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
            if (menuItemIndex == 2)
            {
                if (!ready.Contains(player))
                {
                    ready.Add(player);
                }
                else
                {
                    ready.Remove(player);
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 3)
            {
                setPlayerMenu(player);
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
        else if (ID == (int)MenuID.PlayerMenu)
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
                    setPlayerMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    offsets[player] = offsets[player] + 1;
                    setPlayerMenu(player);
                }
                else
                {
                    setPlayerMenu(player);
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

        if (!isRunning)
        {
            setDummyAircraft(player);
        }
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
                    if (!ready.Contains(player))
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Select Aircraft", "Ready", "Players", "Start Mission" }, new bool[] { true, true, true, true, false });
                    }
                    else
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Select Aircraft", "Not Ready", "Players", "Start Mission" }, new bool[] { true, true, true, true, false });
                    }
                }
                else
                {
                    if (!ready.Contains(player))
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Selected Aircraft: " + createAircraftDisplayName(selectedAircraft), "Ready", "Players", "Start Mission" }, new bool[] { true, true, true, true, false });
                    }
                    else
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.HostMainMenu, new string[] { "Selected Mission: " + selectedMissionFileShortName, "Selected Aircraft: " + createAircraftDisplayName(selectedAircraft), "Not Ready", "Players", "Start Mission" }, new bool[] { true, true, true, true, false });
                    }
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
                    if (!ready.Contains(player))
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Select Aircraft", "Ready", "Players" }, new bool[] { true, true, true });
                    }
                    else
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Select Aircraft", "Not Ready", "Players" }, new bool[] { true, true, true });
                    }
                }
                else
                {
                    if (!ready.Contains(player))
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Selected Aircraft: " + createAircraftDisplayName(selectedAircraft), "Ready", "Players" }, new bool[] { true, true, true });
                    }
                    else
                    {
                        GamePlay.gpSetOrderMissionMenu(player, false, (int)MenuID.ClientMainMenu, new string[] { "Selected Aircraft: " + createAircraftDisplayName(selectedAircraft), "Not Ready", "Players" }, new bool[] { true, true, true });
                    }
                }
            }
        }
    }

    private void setPlayerMenu(Player player)
    {
        List<Player> players = new List<Player>();
        if (GamePlay.gpPlayer() != null)
        {
            if(!players.Contains(GamePlay.gpPlayer()))
            {
                players.Add(GamePlay.gpPlayer());
            }
        }
        if (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
        {
            players.AddRange(GamePlay.gpRemotePlayers());
        }

        if (offsets[player] < 0)
        {
            offsets[player] = (int)players.Count / 7;
        }
        else if ((offsets[player] * 7) > players.Count)
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
                if (entryIndex + (offsets[player] * 7) < players.Count)
                {
                    string aircraftName = null;
                    foreach (string aircraftKey in aircraftSelections.Keys)
                    {
                        if (aircraftSelections[aircraftKey] == players[entryIndex + (offsets[player] * 7)])
                        {
                            aircraftName = aircraftKey;
                            break;
                        }
                    }

                    if (ready.Contains(players[entryIndex + (offsets[player] * 7)]))
                    {
                        if (aircraftName == null)
                        {
                            entry[entryIndex] = players[entryIndex + (offsets[player] * 7)].Name() + ": Ready";
                        }
                        else
                        {
                            entry[entryIndex] = players[entryIndex + (offsets[player] * 7)].Name() + ": Ready (" + createAircraftDisplayName(aircraftName) + ")";
                        }
                    }
                    else
                    {
                        if (aircraftName == null)
                        {
                            entry[entryIndex] = players[entryIndex + (offsets[player] * 7)].Name() + ": Not Ready";
                        }
                        else
                        {
                            entry[entryIndex] = players[entryIndex + (offsets[player] * 7)].Name() + ": Not Ready (" + createAircraftDisplayName(aircraftName) + ")";
                        }
                    }
                    hasSubEntry[entryIndex] = true;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MenuID.PlayerMenu, entry, hasSubEntry);
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
                    hasSubEntry[entryIndex] = true;
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
                        hasSubEntry[entryIndex] = true;
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
            aircraftTypes.Clear();
            aircraftNames.Clear();
            places.Clear();
            aircraftSelections.Clear();
            ready.Clear();

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

                                if (getPlaces(aircraftType) != null)
                                {
                                    foreach (string place in getPlaces(aircraftType))
                                    {
                                        aircrafts.Add(aircraft + "." + place);
                                        aircraftTypes[aircraft + "." + place] = aircraftType;
                                        aircraftNames[aircraft + "." + place] = acNumberList[aircraftIndex];
                                        places[aircraft + "." + place] = place;
                                    }
                                }
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
            isRunning = true;

            // Destroy all dummy aircraft.
            List<AiAircraft> aircaftsToDestroy = new List<AiAircraft>();
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
                                    
                                    if (actor is AiCart )
                                    {
                                        AiAircraft aircraft = actor as AiAircraft;
                                        aircaftsToDestroy.Add(aircraft);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (AiAircraft aircraft in aircaftsToDestroy)
            {
                aircraft.Destroy();
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
