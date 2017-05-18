using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Club_Management
{
    class Club_Management : Script
    {
        private Ped Player = Game.Player.Character;

        private const int maxDrunk = 1;
        private List<Ped> groupMembers = new List<Ped>();

        private bool firstRun = true;
        private bool isManaging;
        private bool noGuns;
        private bool policeIgnore;
        private bool kickingOut;
        private bool interiorLoaded;

        private Stopwatch watchMoney = new Stopwatch();
        private Stopwatch watchPed = new Stopwatch();

        public Club_Management()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            Interval = 10;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (firstRun)
            {
                Blip blipLocation = World.CreateBlip(new Vector3(-566.7609f, 280.0294f, 82.9757f));
                blipLocation.Sprite = BlipSprite.Bar;
                blipLocation.Name = "Club Management";
                blipLocation.IsShortRange = true;
                firstRun = !firstRun;
            }

            int Hours = Function.Call<int>(Hash.GET_CLOCK_HOURS);

            if (Hours > 20 || Hours < 6)
            {
                if(!interiorLoaded)
                {
                    Function.Call(Hash.DISABLE_INTERIOR, Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, -556.5089111328125, 286.318115234375, 81.1763), false);
                    Function.Call(Hash.CAP_INTERIOR, Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, -556.5089111328125, 286.318115234375, 81.1763), false);
                    Function.Call(Hash.REQUEST_IPL, "v_rockclub");
                    Function.Call(Hash._DOOR_CONTROL, 993120320, -565.1712f, 276.6259f, 83.28626f, false, 0.0f, 0.0f, 0.0f);// front door
                    Function.Call(Hash._DOOR_CONTROL, 993120320, -561.2866f, 293.5044f, 87.77851f, false, 0.0f, 0.0f, 0.0f);// back door
                    interiorLoaded = !interiorLoaded;
                }

                var coordsColliding = Function.Call<bool>(Hash._ARE_COORDS_COLLIDING_WITH_EXTERIOR, Player.Position.X, Player.Position.Y, Player.Position.Z);
                var interiorReady = Function.Call<bool>(Hash.IS_INTERIOR_READY, Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, -556.5089111328125, 286.318115234375, 81.1763));

                if (!coordsColliding && interiorReady)
                {
                    Function.Call(Hash.DRAW_MARKER, 2, -566.7609f, 280.0294f, 84.1757f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 0.0f, 0.75f, 0.75f, 0.75f, 204, 204, 0, 50, true, false, 2, true, false, false, false);

                    Ped[] AllPeds = World.GetAllPeds();
                    foreach (Ped P in AllPeds)
                    {
                        var pedsOutside = Function.Call<bool>(Hash._ARE_COORDS_COLLIDING_WITH_EXTERIOR, P.Position.X, P.Position.Y, P.Position.Z);

                        if (P != Player && !pedsOutside)
                        {
                            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, P, 1);
                            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, P, 0, 0);
                            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, P, 17, 1);
                        }
                    }

                    Game.DisableControlThisFrame(21, GTA.Control.Sprint);
                }

                if (isManaging)
                {
                    if(!watchMoney.IsRunning)
                    {
                        watchMoney.Start();
                    }

                    if (groupMembers.Count < maxDrunk)
                    {
                        if (!watchPed.IsRunning)
                        {
                            watchPed.Start();
                        }

                        if (watchPed.Elapsed.Seconds == 30)
                        {
                            spawnDrunk();
                        }
                    }

                    if (Player.IsInRangeOf(new Vector3(-566.7609f, 280.0294f, 82.9757f), 1f))
                    {
                        helpText("Press ~INPUT_CONTEXT~ to stop managing the club.");
                    }

                    if (!noGuns)
                    {
                        Function.Call(Hash.SET_CURRENT_PED_WEAPON, Player, 2725352035, true);
                        Player.CanSwitchWeapons = false;
                        noGuns = !noGuns;
                    }

                    if(!policeIgnore)
                    {
                        Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
                        policeIgnore = !policeIgnore;
                    }

                    if (Game.Player.IsDead)
                    {
                        isManaging = !isManaging;
                    }

                    if(groupMembers.Count > 0)
                    {
                        if(Player.IsInRangeOf(groupMembers[0].Position, 1f))
                        {
                            if(!kickingOut)
                            {
                                helpText("Press ~INPUT_CONTEXT~ to kick out guest.");
                            }
                        }

                        if(groupMembers[0].IsInCombat)
                        {
                            if(groupMembers[0].Health < 25)
                            {
                                groupMembers[0].Task.ClearAllImmediately();
                                groupMembers[0].Task.FleeFrom(Player);
                            }
                        }

                        for (int i = 0; i < groupMembers.Count; i++)
                        {
                            if (groupMembers[i].IsDead || groupMembers[i].IsFleeing)
                            {
                                if (groupMembers[i].CurrentBlip.Exists())
                                {
                                    groupMembers[i].CurrentBlip.Remove();
                                }
                                groupMembers[i].MarkAsNoLongerNeeded();
                                groupMembers.RemoveAt(i);

                                if(kickingOut)
                                {
                                    kickingOut = !kickingOut;
                                }
                            }
                        }
                    }

                    if (coordsColliding)
                    {
                        isManaging = !isManaging;
                    }
                }

                if (!isManaging)
                {
                    if(watchMoney.IsRunning)
                    {
                        watchMoney.Stop();
                        var timeElapsed = watchMoney.Elapsed.Minutes;
                        var moneyMade = timeElapsed * 1000;
                        Game.Player.Money += moneyMade;
                        watchMoney.Reset();
                    }

                    if (Player.IsInRangeOf(new Vector3(-566.7609f, 280.0294f, 82.9757f), 1f))
                    {
                        if(Game.Player.WantedLevel == 0)
                        {
                            helpText("Press ~INPUT_CONTEXT~ to manage the club.");
                        }

                        if(Game.Player.WantedLevel > 0)
                        {
                            helpText("You cannot manage the club with a wanted level.");
                        }
                    }

                    if (groupMembers.Count > 0)
                    {
                        for (int i = 0; i < groupMembers.Count; i++)
                        {
                            if (groupMembers[i].IsAlive)
                            {
                                if (groupMembers[i].CurrentBlip.Exists())
                                {
                                    groupMembers[i].CurrentBlip.Remove();
                                }
                                groupMembers[i].MarkAsNoLongerNeeded();
                                groupMembers.RemoveAt(i);
                            }
                        }
                    }

                    if (kickingOut)
                    {
                        kickingOut = !kickingOut;
                    }

                    if (noGuns)
                    {
                        Player.CanSwitchWeapons = true;
                        noGuns = !noGuns;
                    }

                    if (policeIgnore)
                    {
                        Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                        policeIgnore = !policeIgnore;
                    }
                }
            }

            else
            {
                var coordsColliding = Function.Call<bool>(Hash._ARE_COORDS_COLLIDING_WITH_EXTERIOR, Player.Position.X, Player.Position.Y, Player.Position.Z);
                var nearFrontDoor = Player.IsInRangeOf(new Vector3(-565.1712f, 276.6259f, 83.28626f), 1f);
                var nearRearDoor = Player.IsInRangeOf(new Vector3(-561.2866f, 293.5044f, 87.77851f), 1f);

                if (coordsColliding && !nearFrontDoor && !nearRearDoor && interiorLoaded)
                {
                    Function.Call(Hash.REMOVE_IPL, "v_rockclub");
                    Function.Call(Hash._DOOR_CONTROL, 993120320, -565.1712f, 276.6259f, 83.28626f, true, 0.0f, 0.0f, 0.0f);// front door
                    Function.Call(Hash._DOOR_CONTROL, 993120320, -561.2866f, 293.5044f, 87.77851f, true, 0.0f, 0.0f, 0.0f);// back door
                    interiorLoaded = !interiorLoaded;
                }


                if (Player.IsInRangeOf(new Vector3(-565.1712f, 276.6259f, 83.28626f), 5f))
                {
                    helpText("Tequi-La-La is closed come back between 21:00 and 6:00.");
                }

                if (groupMembers.Count > 0)
                {
                    for (int i = 0; i < groupMembers.Count; i++)
                    {
                        if (groupMembers[i].IsAlive)
                        {
                            if (groupMembers[i].CurrentBlip.Exists())
                            {
                                groupMembers[i].CurrentBlip.Remove();
                            }
                            groupMembers[i].MarkAsNoLongerNeeded();
                            groupMembers.RemoveAt(i);
                        }
                    }
                }

                if (noGuns)
                {
                    Player.CanSwitchWeapons = true;
                    noGuns = !noGuns;
                }

                if (policeIgnore)
                {
                    Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                    policeIgnore = !policeIgnore;
                }

                if (isManaging)
                {
                    isManaging = !isManaging;

                    if (watchMoney.IsRunning)
                    {
                        watchMoney.Stop();
                        var timeElapsed = watchMoney.Elapsed.Minutes;
                        var moneyMade = timeElapsed * 1000;
                        Game.Player.Money += moneyMade;
                        watchMoney.Reset();
                    }
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.E)
            {
                if (Player.IsInRangeOf(new Vector3(-566.7609f, 280.0294f, 82.9757f), 1f) && Game.Player.WantedLevel == 0)
                {
                    isManaging = !isManaging;
                }

                if (isManaging)
                {
                    if (groupMembers.Count > 0)
                    {
                        if (Player.IsInRangeOf(groupMembers[0].Position, 1f))
                        {
                            if(!kickingOut)
                            {
                                kickingOut = !kickingOut;
                                Random reactRandom = new Random();
                                int pedReaction = reactRandom.Next(0, 100);

                                if (pedReaction <= 49)
                                {
                                    pedPassive();
                                }

                                if (pedReaction >= 50)
                                {
                                    pedAggressive();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool A_0)
        {
            if (A_0)
            {
                if (groupMembers.Count > 0)
                {
                    for (int i = 0; i < groupMembers.Count; i++)
                    {
                        if (groupMembers[i].CurrentBlip.Exists())
                        {
                            groupMembers[i].CurrentBlip.Remove();
                        }
                        groupMembers[i].Delete();
                    }
                }
            }
        }

        private void helpText(string text)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, text);
            Function.Call(Hash._0x238FFE5C7B0498A6, 0, 0, 1, -1);
        }

        private void spawnDrunk()
        {
            List<Ped> allowedPeds = new List<Ped>();

            foreach (Ped P in World.GetAllPeds())
            {
                if(P != Player)
                {
                    var pedExists = Function.Call<bool>(Hash.DOES_ENTITY_EXIST, P);

                    if (pedExists)
                    {
                        var pedOutside = Function.Call<bool>(Hash._ARE_COORDS_COLLIDING_WITH_EXTERIOR, P.Position.X, P.Position.Y, P.Position.Z);

                        if (!pedOutside)
                        {
                            allowedPeds.Add(P);
                        }

                    }
                }
            }

            Random getPed = new Random();

            Ped Drunk = allowedPeds[getPed.Next(0, allowedPeds.Count)];

            Function.Call(Hash.REQUEST_CLIP_SET, "move_m@drunk@verydrunk");
            Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, Drunk, "move_m@drunk@verydrunk", 1.0f);
            Function.Call(Hash.SET_PED_CAN_RAGDOLL, Drunk, false);

            groupMembers.Add(Drunk);

            Drunk.AddBlip();
            Drunk.CurrentBlip.Sprite = BlipSprite.ChatBubble;
            helpText("The ~h~chat bubble~h~ indicates an unruly guest. Go and deal with them.");

            Drunk.AlwaysKeepTask = true;
            Drunk.Task.WanderAround(Drunk.Position, 2f);

            allowedPeds.Clear();

            watchPed.Reset();
        }

        private void pedPassive()
        {
            groupMembers[0].Task.ClearAllImmediately();
            Function.Call(Hash.TASK_LOOK_AT_ENTITY, groupMembers[0], Player, -1, 2048, 3);
            groupMembers[0].Task.TurnTo(Player.Position, -1);
            Player.Task.ClearAllImmediately();
            Player.Task.TurnTo(groupMembers[0].Position, -1);
            Player.Task.PlayAnimation("mini@strip_club@idles@bouncer@go_away", "go_away", 1.0f, 5000, true, 1.0f);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Player, "GENERIC_FUCK_YOU", "SPEECH_PARAMS_STANDARD");
            Wait(5000);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, groupMembers[0], "APOLOGY_NO_TROUBLE", "SPEECH_PARAMS_STANDARD");
            groupMembers[0].Task.ClearAllImmediately();
            groupMembers[0].Task.FleeFrom(Player);
        }

        private void pedAggressive()
        {
            groupMembers[0].Task.ClearAllImmediately();
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, groupMembers[0], 46, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, groupMembers[0], 5, true);
            Function.Call(Hash.TASK_LOOK_AT_ENTITY, groupMembers[0], Player, -1, 2048, 3);
            groupMembers[0].Task.TurnTo(Player.Position, -1);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, groupMembers[0], "GENERIC_FUCK_YOU", "SPEECH_PARAMS_STANDARD");
            Player.Task.ClearAllImmediately();
            Player.Task.TurnTo(groupMembers[0].Position, -1);
            Player.Task.PlayAnimation("mini@strip_club@idles@bouncer@go_away", "go_away", 1.0f, 5000, true, 1.0f);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Player, "GENERIC_FUCK_YOU", "SPEECH_PARAMS_STANDARD");
            Wait(5000);
            groupMembers[0].Task.ClearAllImmediately();
            groupMembers[0].CurrentBlip.Sprite = BlipSprite.Standard;
            groupMembers[0].CurrentBlip.Color = BlipColor.Red;
            Function.Call(Hash.TASK_COMBAT_PED, groupMembers[0], Player, 0, 16);
        }
    }
}