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
    /// <summary>
    /// The names of the players that have hosting permissions.
    /// </summary>
    private List<string> hostPlayers = new List<string>
    {
        "41Sqn_Skipper",
    };

    /// <summary>
    /// The name of the map of the lobby mission. Only missions that stage on the same map can be loaded from the lobby mission.
    /// </summary>
    private string mapName = "Land$English_Channel_1940";
    
    /// <summary>
    /// The ids of the different OrderMissionMenus.
    /// </summary>
    private enum MainMenuID
    {
        HostMainMenu,
        ClientMainMenu,

        OpenMissionMenu,
        CloseMissionMenu,
        StartMissionMenu,
        
        SelectMissionMenu,
        SelectAircraftMenu,
        PlayerMenu,
    }

    private class CoopMission
    {
        public enum MissionState
        {
            None,
            Pending,
            Running,
            Finished,
        }

        public CoopMission(string missionFileName)
        {
            this.MissionFileName = missionFileName;
            this.State = MissionState.None;
        }

        public MissionState State
        {
            get;
            set;
        }

        public int MissionNumber
        {
            get;
            set;
        }

        public string MissionFileName
        {
            get;
            set;
        }

        public string DisplayName
        {
            get
            {
                if (State == MissionState.None)
                {
                    return createMissionFileDisplayName(this.MissionFileName);
                }
                else
                {
                    return createMissionFileDisplayName(this.MissionFileName) + " (" + State.ToString() + ")";
                }
            }
        }

        public List<string> ForcedIdleAirGroups
        {
            get
            {
                return this.forcedIdleAirGroups;
            }
        }
        private List<string> forcedIdleAirGroups = new List<string>();

        public Dictionary<string, Player> AircraftPlaceSelections
        {
            get
            {
                return this.aircraftPlaceSelections;
            }
        }
        Dictionary<string, Player> aircraftPlaceSelections = new Dictionary<string, Player>();

        public List<AiActor> AiActors
        {
            get
            {
                return this.aiActors;
            }
        }
        List<AiActor> aiActors = new List<AiActor>();
    }

    List<CoopMission> missions = new List<CoopMission>();
    
    Dictionary<Player, CoopMission> missionSelections = new Dictionary<Player, CoopMission>();
    
    private Dictionary<Player, int> menuOffsets = new Dictionary<Player, int>();

    private static string createMissionFileDisplayName(string missionFileName)
    {
        return missionFileName.Replace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions", "");
    }

    private List<string> getMissionFileNames()
    {
        string missionsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\missions";
        string[] tempMissionFileNames = Directory.GetFiles(missionsFolderPath, "*.mis", SearchOption.AllDirectories);

        List<string> missionFileNames = new List<string>();
        foreach (string tempMissionFileName in tempMissionFileNames)
        {
            if (tempMissionFileName.EndsWith(".mis"))
            {
                ISectionFile tempMissionFile = GamePlay.gpLoadSectionFile(tempMissionFileName);
                if (tempMissionFile.get("MAIN", "MAP") == mapName)
                {
                    missionFileNames.Add(tempMissionFileName);
                }
            }
        }

        return missionFileNames;
    }

    private List<string> getAircraftPlaceDisplayNames(Player player)
    {
        List<string> aircraftPlaceDisplayNames = new List<string>();

        if(missionSelections.ContainsKey(player))
        {
            CoopMission mission = missionSelections[player];
            
            if (GamePlay.gpAirGroups(player.Army()) != null && GamePlay.gpAirGroups(player.Army()).Length > 0)
            {
                foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(player.Army()))
                {
                    if (airGroup.Name().StartsWith(mission.MissionNumber + ":"))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    if (aircraft.Places() > 0)
                                    {
                                        for (int placeIndex = 0; placeIndex < aircraft.Places(); placeIndex++)
                                        {
                                            if (aircraft.ExistCabin(placeIndex))
                                            {
                                                string aircraftPlaceDisplayName = aircraft.Name() + " " + aircraft.TypedName() + " | " + aircraft.InternalTypeName() + " " + aircraft.CrewFunctionPlace(placeIndex).ToString();
                                                aircraftPlaceDisplayNames.Add(aircraftPlaceDisplayName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return aircraftPlaceDisplayNames;
    }

    private string getAircraftPlaceDisplayName(Player player, string aircraftPlace)
    {
        if (missionSelections.ContainsKey(player))
        {
            CoopMission mission = missionSelections[player];

            if (GamePlay.gpAirGroups(player.Army()) != null && GamePlay.gpAirGroups(player.Army()).Length > 0)
            {
                foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(player.Army()))
                {
                    if (airGroup.Name().StartsWith(mission.MissionNumber + ":"))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    if (aircraft.Places() > 0)
                                    {
                                        for (int placeIndex = 0; placeIndex < aircraft.Places(); placeIndex++)
                                        {
                                            if (aircraft.ExistCabin(placeIndex))
                                            {
                                                if (aircraftPlace == aircraft.Name().Replace(mission.MissionNumber + ":", "") + "@" + placeIndex)
                                                {
                                                    return aircraft.Name() + " " + aircraft.TypedName() + " | " + aircraft.InternalTypeName() + " " + aircraft.CrewFunctionPlace(placeIndex).ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return "";
    }

    private List<string> getAircraftPlaces(Player player)
    {
        List<string> aircraftPlaces = new List<string>();

        if(missionSelections.ContainsKey(player))
        {
            CoopMission mission = missionSelections[player];

            if (GamePlay.gpAirGroups(player.Army()) != null && GamePlay.gpAirGroups(player.Army()).Length > 0)
            {
                foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(player.Army()))
                {
                    if (airGroup.Name().StartsWith(mission.MissionNumber + ":"))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    if (aircraft.Places() > 0)
                                    {
                                        for (int placeIndex = 0; placeIndex < aircraft.Places(); placeIndex++)
                                        {
                                            if (aircraft.ExistCabin(placeIndex))
                                            {
                                                string aircraftPlace = aircraft.Name().Replace(mission.MissionNumber + ":", "") + "@" + placeIndex;
                                                aircraftPlaces.Add(aircraftPlace);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return aircraftPlaces;
    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        MissionNumberListener = -1;
    }

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);

        foreach (CoopMission mission in missions)
        {
            if (mission.MissionNumber == missionNumber)
            {
                foreach (string aircraftSelection in mission.AircraftPlaceSelections.Keys)
                {
                    string aircraftName = aircraftSelection.Remove(aircraftSelection.IndexOf("@"), aircraftSelection.Length - aircraftSelection.IndexOf("@"));
                    if (missionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + ":" + aircraftName == actor.Name())
                    {
                        Timeout(3.0, () =>
                        {
                            placePlayer(mission.AircraftPlaceSelections[aircraftSelection]);
                        });
                    }
                }

                mission.AiActors.Add(actor);
            }
        }
    }

    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        if (ID == (int)MainMenuID.HostMainMenu)
        {
            if (menuItemIndex == 1)
            {
                setOpenMissionMenu(player);                
            }
            if (menuItemIndex == 2)
            {
                setCloseMissionMenu(player);
            }
            else if (menuItemIndex == 3)
            {
                setStartMissionMenu(player);
            }
            if (menuItemIndex == 4)
            {
                setSelectMissionMenu(player);
            }
            if (menuItemIndex == 5)
            {
                setSelectAircraftMenu(player);
            }
            else if (menuItemIndex == 6)
            {
                setPlayerMenu(player);
            }
        }
        else if (ID == (int)MainMenuID.ClientMainMenu)
        {
            if (menuItemIndex == 1)
            {
                setSelectMissionMenu(player);
            }
            if (menuItemIndex == 2)
            {
                setSelectAircraftMenu(player);
            }
            else if (menuItemIndex == 3)
            {
                setPlayerMenu(player);
            }
        }        
        else if (ID == (int)MainMenuID.SelectMissionMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setSelectMissionMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setSelectMissionMenu(player);
                }
                else
                {
                    if (menuItemIndex - 1 + (menuOffsets[player] * 7) < missions.Count)
                    {
                        CoopMission mission = missions[menuItemIndex - 1 + (menuOffsets[player] * 7)];
                        missionSelections[player] = mission;
                        setMainMenu(player);
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
        else if (ID == (int)MainMenuID.SelectAircraftMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setSelectAircraftMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setSelectAircraftMenu(player);
                }
                else
                {
                    if (menuItemIndex - 1 + (menuOffsets[player] * 7) < getAircraftPlaces(player).Count)
                    {
                        if(missionSelections.ContainsKey(player))
                        {
                            CoopMission mission = missionSelections[player];
                        
                            List<string> aircraftPlaces = getAircraftPlaces(player);
                            List<string> aircraftPlaceDisplayNames = getAircraftPlaceDisplayNames(player);
                            if (!mission.AircraftPlaceSelections.ContainsKey(aircraftPlaces[menuItemIndex - 1 + (menuOffsets[player] * 7)]))
                            {
                                foreach(string aircraftPlace in mission.AircraftPlaceSelections.Keys)
                                {
                                    if(mission.AircraftPlaceSelections[aircraftPlace] == player)
                                    {
                                        mission.AircraftPlaceSelections.Remove(aircraftPlace);
                                        break;
                                    }
                                }

                                mission.AircraftPlaceSelections[aircraftPlaces[menuItemIndex - 1 + (menuOffsets[player] * 7)]] = player;
                                placePlayer(player);
                                
                                GamePlay.gpLogServer(new Player[] { player }, "Aircraft selected: " + aircraftPlaceDisplayNames[menuItemIndex - 1 + (menuOffsets[player] * 7)], null);

                                setMainMenu(player);
                            }
                            else
                            {
                                GamePlay.gpLogServer(new Player[] { player }, "Aircraft is already occupied.", null);

                                setSelectAircraftMenu(player);
                            }
                        }
                        else
                        {
                            // No handling needed as menu item is not displayed.
                        }
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
        else if (ID == (int)MainMenuID.PlayerMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setPlayerMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setPlayerMenu(player);
                }
                else
                {
                    setPlayerMenu(player);
                }
            }
        }        
        else if (ID == (int)MainMenuID.OpenMissionMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setOpenMissionMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setOpenMissionMenu(player);
                }
                else
                {
                    List<string> missionFileNames = getMissionFileNames();

                    if (menuItemIndex - 1 + (menuOffsets[player] * 7) < missionFileNames.Count)
                    {
                        string missionFileName = missionFileNames[menuItemIndex - 1 + (menuOffsets[player] * 7)];

                        CoopMission mission = new CoopMission(missionFileName);
                        missions.Add(mission);
                        openMission(mission);

                        GamePlay.gpLogServer(new Player[] { player }, "Mission pending: " + mission.DisplayName, null);

                        setMainMenu(player);
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
        else if (ID == (int)MainMenuID.CloseMissionMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setCloseMissionMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setCloseMissionMenu(player);
                }
                else
                {
                    if (menuItemIndex - 1 + (menuOffsets[player] * 7) < missions.Count)
                    {
                        CoopMission mission = missions[menuItemIndex - 1 + (menuOffsets[player] * 7)];
                        closeMission(mission);

                        GamePlay.gpLogServer(new Player[] { player }, "Mission closed: " + mission.DisplayName, null);

                        setMainMenu(player);
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
        else if (ID == (int)MainMenuID.StartMissionMenu)
        {
            if (menuItemIndex == 0)
            {
                setMainMenu(player);
            }
            else
            {
                if (menuItemIndex == 8)
                {
                    menuOffsets[player] = menuOffsets[player] - 1;
                    setStartMissionMenu(player);
                }
                else if (menuItemIndex == 9)
                {
                    menuOffsets[player] = menuOffsets[player] + 1;
                    setStartMissionMenu(player);
                }
                else
                {
                    if (menuItemIndex - 1 + (menuOffsets[player] * 7) < missions.Count)
                    {
                        CoopMission mission = missions[menuItemIndex - 1 + (menuOffsets[player] * 7)];
                        startMission(mission);

                        GamePlay.gpLogServer(new Player[] { player }, "Mission started: " + mission.DisplayName, null);

                        setMainMenu(player);
                    }
                    else
                    {
                        // No handling needed as menu item is not displayed.
                    }
                }
            }
        }
    }

    public override void OnPlayerDisconnected(Player player, string diagnostic)
    {
        base.OnPlayerDisconnected(player, diagnostic);

        if (missionSelections.ContainsKey(player))
        {
            foreach (string aircraftSelection in missionSelections[player].AircraftPlaceSelections.Keys)
            {
                if (missionSelections[player].AircraftPlaceSelections[aircraftSelection] == player)
                {
                    missionSelections[player].AircraftPlaceSelections.Remove(aircraftSelection);
                    break;
                }
            }
        }

        missionSelections.Remove(player);
    }

    public override void OnPlayerArmy(Player player, int army)
    {
        base.OnPlayerArmy(player, army);

        assignToLobbyAircraft(player);
    }

    public override void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorDestroyed(missionNumber, shortName, actor);

        assignPlayersOfActorToLobbyAircraft(actor);
    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        setMainMenu(player);
    }

    /// <summary>
    /// Assigns all players that occupy a place of the actor to one of the lobby aircrafts.
    /// </summary>
    /// <param name="actor">The actor that might be occupied by players.</param>
    private void assignPlayersOfActorToLobbyAircraft(AiActor actor)
    {
        if (actor is AiAircraft)
        {
            AiAircraft aircraft = actor as AiAircraft;

            for (int placeIndex = 0; placeIndex < aircraft.Places(); placeIndex++)
            {
                if (aircraft.Player(placeIndex) != null)
                {
                    Player player = aircraft.Player(placeIndex);
                    assignToLobbyAircraft(player);
                }
            }
        }
    }

    /// <summary>
    /// Assigns a player to an unoccupied place in one of the lobby aircrafts.
    /// </summary>
    /// <param name="player">The player that is assigned to a lobby aircraft.</param>
    private void assignToLobbyAircraft(Player player)
    {
        if (GamePlay.gpAirGroups(player.Army()) != null && GamePlay.gpAirGroups(player.Army()).Length > 0)
        {
            foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(player.Army()))
            {
                // Lobby aircrafts always have the mission index 0.
                if (airGroup.Name().StartsWith("0:"))
                {
                    if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                    {
                        foreach (AiActor actor in airGroup.GetItems())
                        {
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
        else
        {
            GamePlay.gpLogServer(new Player[] { player }, "No unoccupied place available in the lobby aircrafts.", null);
        }
    }

    /// <summary>
    /// Sets the main menu for a player.
    /// </summary>
    /// <param name="player">The player that gets the main menu set.</param>
    private void setMainMenu(Player player)
    {
        menuOffsets[player] = 0;

        string aircraftPlaceDisplyName = "None";
        
        if ((GamePlay.gpPlayer() != null && player == GamePlay.gpPlayer()) || (player.Name() != null && player.Name() != "" && hostPlayers.Contains(player.Name())))
        {
            // Set host menu.
            string[] entry = new string[] { "", "", "", "", "", "" };
            bool[] hasSubEntry = new bool[] { true, true, true, true, true, true };

            entry[0] = "Open Mission";
            entry[1] = "Close Mission";
            entry[2] = "Start Mission";
            
            if(!missionSelections.ContainsKey(player))
            {
                entry[3] = "Select Mission (Selected Mission: None)";
            }
            else
            {
                CoopMission mission = missionSelections[player];

                foreach (string aircraftPlace in missionSelections[player].AircraftPlaceSelections.Keys)
                {
                    if (missionSelections[player].AircraftPlaceSelections[aircraftPlace] == player)
                    {
                        aircraftPlaceDisplyName = getAircraftPlaceDisplayName(player, aircraftPlace);
                        break;
                    }
                }

                entry[3] = "Select Mission (Selected Mission: " + mission.DisplayName + ")";
                entry[4] = "Select Aircraft (Selected Aircraft: " + aircraftPlaceDisplyName + ")";
                entry[5] = "Players";
            }

            GamePlay.gpSetOrderMissionMenu(player, false, (int)MainMenuID.HostMainMenu, entry, hasSubEntry);
        }
        else
        {
            // Set client menu.            
            string[] entry = new string[] { "", "", "" };
            bool[] hasSubEntry = new bool[] { true, true, true };

            if (!missionSelections.ContainsKey(player))
            {
                entry[0] = "Select Mission (Selected Mission: None)";
            }
            else
            {
                CoopMission mission = missionSelections[player];

                foreach (string aircraftPlace in mission.AircraftPlaceSelections.Keys)
                {
                    if (missionSelections[player].AircraftPlaceSelections[aircraftPlace] == player)
                    {
                        aircraftPlaceDisplyName = getAircraftPlaceDisplayName(player, aircraftPlace);
                        break;
                    }
                }

                entry[0] = "Select Mission (Selected Mission: " + mission.DisplayName + ")";
                entry[1] = "Select Aircraft (Selected Aircraft: " + aircraftPlaceDisplyName + ")";
                entry[2] = "Players";

                GamePlay.gpSetOrderMissionMenu(player, false, (int)MainMenuID.ClientMainMenu, entry, hasSubEntry);
            }
        }
    }

    private void setSelectMissionMenu(Player player)
    {
        if (menuOffsets[player] < 0)
        {
            menuOffsets[player] = (int)missions.Count / 7;
        }
        else if ((menuOffsets[player] * 7) > missions.Count)
        {
            menuOffsets[player] = 0;
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
                if (entryIndex + (menuOffsets[player] * 7) < missions.Count)
                {
                    entry[entryIndex] = createMissionFileDisplayName(missions[entryIndex + (menuOffsets[player] * 7)].DisplayName);
                    hasSubEntry[entryIndex] = true;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.SelectMissionMenu, entry, hasSubEntry);
    }

    private void setSelectAircraftMenu(Player player)
    {
        int entryCount = 9;
        string[] entry = new string[entryCount];
        bool[] hasSubEntry = new bool[entryCount];

        if (missionSelections.ContainsKey(player))
        {
            CoopMission mission = missionSelections[player];

            List<string> aircraftPlaceDisplayNames = getAircraftPlaceDisplayNames(player);
            List<string> aircraftPlaces = getAircraftPlaces(player);

            if (menuOffsets[player] < 0)
            {
                menuOffsets[player] = (int)aircraftPlaceDisplayNames.Count / 7;
            }
            else if ((menuOffsets[player] * 7) > aircraftPlaceDisplayNames.Count)
            {
                menuOffsets[player] = 0;
            }

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
                    if (entryIndex + (menuOffsets[player] * 7) < aircraftPlaceDisplayNames.Count)
                    {
                        if (mission.AircraftPlaceSelections.ContainsKey(aircraftPlaces[entryIndex + (menuOffsets[player] * 7)]))
                        {
                            entry[entryIndex] = aircraftPlaceDisplayNames[entryIndex + (menuOffsets[player] * 7)] + ": " + mission.AircraftPlaceSelections[aircraftPlaces[entryIndex + (menuOffsets[player] * 7)]].Name();
                            hasSubEntry[entryIndex] = true;
                        }
                        else
                        {
                            entry[entryIndex] = aircraftPlaceDisplayNames[entryIndex + (menuOffsets[player] * 7)];
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
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.SelectAircraftMenu, entry, hasSubEntry);
    }

    private void setPlayerMenu(Player player)
    {
        int entryCount = 9;
        string[] entry = new string[entryCount];
        bool[] hasSubEntry = new bool[entryCount];

        if (missionSelections.ContainsKey(player))
        {
            CoopMission playerMission = missionSelections[player];

            List<Player> players = new List<Player>();
            foreach (Player otherPlayer in missionSelections.Keys)
            {
                if (missionSelections[otherPlayer] == missionSelections[player])
                {
                    players.Add(otherPlayer);
                }
            }

            if (menuOffsets[player] < 0)
            {
                menuOffsets[player] = (int)players.Count / 7;
            }
            else if ((menuOffsets[player] * 7) > players.Count)
            {
                menuOffsets[player] = 0;
            }

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
                    if (entryIndex + (menuOffsets[player] * 7) < players.Count)
                    {
                        string aircraftPlaceDisplyName = "None";
                        foreach (string aircraftPlace in missionSelections[player].AircraftPlaceSelections.Keys)
                        {
                            if (missionSelections[player].AircraftPlaceSelections[aircraftPlace] == players[entryIndex + (menuOffsets[player] * 7)])
                            {
                                aircraftPlaceDisplyName = getAircraftPlaceDisplayName(player, aircraftPlace);
                                break;
                            }
                        }

                        entry[entryIndex] = players[entryIndex + (menuOffsets[player] * 7)].Name() + " (" + aircraftPlaceDisplyName + ")";

                        hasSubEntry[entryIndex] = true;
                    }
                    else
                    {
                        entry[entryIndex] = "";
                        hasSubEntry[entryIndex] = true;
                    }
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.PlayerMenu, entry, hasSubEntry);
    }

    private void setOpenMissionMenu(Player player)
    {
        List<string> missionFileNames = getMissionFileNames();

        if (menuOffsets[player] < 0)
        {
            menuOffsets[player] = (int)missionFileNames.Count / 7;
        }
        else if ((menuOffsets[player] * 7) > missionFileNames.Count)
        {
            menuOffsets[player] = 0;
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
                if (entryIndex + (menuOffsets[player] * 7) < missionFileNames.Count)
                {
                    entry[entryIndex] = createMissionFileDisplayName(missionFileNames[entryIndex + (menuOffsets[player] * 7)]);
                    hasSubEntry[entryIndex] = true;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.OpenMissionMenu, entry, hasSubEntry);
    }

    private void setCloseMissionMenu(Player player)
    {
        if (menuOffsets[player] < 0)
        {
            menuOffsets[player] = (int)missions.Count / 7;
        }
        else if ((menuOffsets[player] * 7) > missions.Count)
        {
            menuOffsets[player] = 0;
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
                if (entryIndex + (menuOffsets[player] * 7) < missions.Count)
                {
                    entry[entryIndex] = createMissionFileDisplayName(missions[entryIndex + (menuOffsets[player] * 7)].DisplayName);
                    hasSubEntry[entryIndex] = true;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.CloseMissionMenu, entry, hasSubEntry);
    }

    private void setStartMissionMenu(Player player)
    {
        if (menuOffsets[player] < 0)
        {
            menuOffsets[player] = (int)missions.Count / 7;
        }
        else if ((menuOffsets[player] * 7) > missions.Count)
        {
            menuOffsets[player] = 0;
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
                if (entryIndex + (menuOffsets[player] * 7) < missions.Count)
                {
                    entry[entryIndex] = createMissionFileDisplayName(missions[entryIndex + (menuOffsets[player] * 7)].DisplayName);
                    hasSubEntry[entryIndex] = true;
                }
                else
                {
                    entry[entryIndex] = "";
                    hasSubEntry[entryIndex] = true;
                }
            }
        }

        GamePlay.gpSetOrderMissionMenu(player, true, (int)MainMenuID.StartMissionMenu, entry, hasSubEntry);
    }
    
    private void openMission(CoopMission mission)
    {
        removeActors(mission);

        ISectionFile preloadMissionFile = GamePlay.gpLoadSectionFile(mission.MissionFileName);

        for (int airGroupIndex = 0; airGroupIndex < preloadMissionFile.lines("AirGroups"); airGroupIndex++)
        {
            string airGroupKey;
            string value;
            preloadMissionFile.get("AirGroups", airGroupIndex, out airGroupKey, out value);

            preloadMissionFile.set(airGroupKey, "Idle", true);
            preloadMissionFile.set(airGroupKey, "SpawnFromScript", false);
            preloadMissionFile.set(airGroupKey, "Fuel", 0);

            if (preloadMissionFile.lines(airGroupKey + "_Way") > 0)
            {
                string waypointKey;
                string waypointValue;
                preloadMissionFile.get(airGroupKey + "_Way", 0, out waypointKey, out waypointValue);
                if (waypointKey != "TAKEOFF")
                {
                    // TODO: Handle airstart.                    
                }
            }

            preloadMissionFile.delete("PARTS");
            preloadMissionFile.delete("MAIN");
            preloadMissionFile.delete("Stationary");
            preloadMissionFile.delete("Buildings");
            preloadMissionFile.delete("Chiefs");
        }

        mission.MissionNumber = GamePlay.gpNextMissionNumber();
        GamePlay.gpPostMissionLoad(preloadMissionFile);

        mission.State = CoopMission.MissionState.Pending;
    }

    private void closeMission(CoopMission mission)
    {
        removeActors(mission);

        List<Player> players = new List<Player>();
        foreach (Player player in missionSelections.Keys)
        {
            if (missionSelections[player] == mission)
            {
                mission.AircraftPlaceSelections.Clear();
                mission.State = CoopMission.MissionState.None;
                players.Add(player);
            }
        }

        foreach (Player player in players)
        {
            missionSelections.Remove(player);
        }

        missions.Remove(mission);
    }

    private void startMission(CoopMission mission)
    {
        removeActors(mission);

        // Set air groups to idle by editing the mission file.
        ISectionFile missionFile = GamePlay.gpLoadSectionFile(mission.MissionFileName);
        
        for (int airGroupIndex = 0; airGroupIndex < missionFile.lines("AirGroups"); airGroupIndex++)
        {
            string airGroupKey;
            string value;
            missionFile.get("AirGroups", airGroupIndex, out airGroupKey, out value);

            if (!(missionFile.get(airGroupKey, "Idle") == "1"))
            {
                missionFile.set(airGroupKey, "Idle", true);
                mission.ForcedIdleAirGroups.Add(airGroupKey);
            }
        }
        
        // Load the mission.
        mission.MissionNumber = GamePlay.gpNextMissionNumber();
        GamePlay.gpPostMissionLoad(missionFile);

        Timeout(5.0, () =>
        {
            removeIdle(mission);
        });

        mission.State = CoopMission.MissionState.Running;
    }

    private void placePlayer(Player player)
    {
        if (missionSelections.ContainsKey(player))
        {
            CoopMission mission = missionSelections[player];

            foreach (string aircraftSelection in mission.AircraftPlaceSelections.Keys)
            {
                if (mission.AircraftPlaceSelections[aircraftSelection] == player)
                {
                    string aircraftName = aircraftSelection.Remove(aircraftSelection.IndexOf("@"), aircraftSelection.Length - aircraftSelection.IndexOf("@"));
                    string place = aircraftSelection.Replace(aircraftName + "@", "");
                    int placeIndex;
                    if (int.TryParse(place, out placeIndex))
                    {
                        AiActor actor = GamePlay.gpActorByName(mission.MissionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + ":" + aircraftName);
                        if (actor != null && actor is AiAircraft)
                        {
                            AiAircraft aircraft = actor as AiAircraft;
                            if (aircraft.ExistCabin(placeIndex))
                            {
                                mission.AircraftPlaceSelections[aircraftSelection].PlaceEnter(aircraft, placeIndex);
                            }
                        }
                    }
                }
            }
        }
    }

    private void removeIdle(CoopMission mission)
    {
        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                {
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.Name().StartsWith(mission.MissionNumber + ":"))
                        {
                            string airGroupName = airGroup.Name().Replace(mission.MissionNumber + ":", "");

                            if (mission.ForcedIdleAirGroups.Contains(airGroupName))
                            {
                                airGroup.Idle = false;
                            }
                        }
                    }
                }
            }
        }
    }

    private void removeActors(CoopMission mission)
    {
        foreach (AiActor actor in mission.AiActors)
        {
            if (actor is AiCart)
            {
                (actor as AiCart).Destroy();
            }
        }
        mission.AiActors.Clear();
    }
}
