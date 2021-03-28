using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        bool check = false;
        bool goPlanet = false;
        bool goStation = false;
        bool stationReady = false;
        bool planetReady = false;

        IMyMotorAdvancedStator hingeRamp;
        IMyTextSurface panel0;
        IMyRemoteControl rc_AstroBus;
        IMyShipConnector co_AstroBus;
        IMySensorBlock sensorExt_AstroBus;
        IMyTimerBlock tb_SlowDown;
        IMyTimerBlock tb_Start;
        IMyTimerBlock tb_goplanet;
        IMyDoor door;

        List<IMyBatteryBlock> battery_AstroBus = new List<IMyBatteryBlock>();
        List<IMyGasTank> hydrogenTank_AstroBus = new List<IMyGasTank>();
        List<IMyParachute> parachute_AstroBus = new List<IMyParachute>();
        List<IMyTextPanel> lcd_Top = new List<IMyTextPanel>();
        List<IMyExtendedPistonBase> pistonsRamp = new List<IMyExtendedPistonBase>();
        List<IMyLandingGear> landingGear_Rocket = new List<IMyLandingGear>();
        List<MyDetectedEntityInfo> listEntitiesDetected = new List<MyDetectedEntityInfo>();
        List<MyWaypointInfo> waypoints;
        List<IMyGyro> gyro_Rocket = new List<IMyGyro>();
        List<IMyThrust> thrusters_Up = new List<IMyThrust>();
        List<IMyThrust> thrusters_Hydro = new List<IMyThrust>();
        List<IMyThrust> thrusters_HydroPrecision = new List<IMyThrust>();
        List<IMyThrust> thrusters_Atmos = new List<IMyThrust>();
        List<IMyThrust> thrusters_AstroBus = new List<IMyThrust>();

        string arguments = "";
        float speedMax = 450f;
        double speed = 0;
        double Altitude = 0d;
        double step = 0;
        string err_TXT = "";
        string status = "";

        Vector3D waypoint0 = new Vector3D(12892.47, -12392.46, 58590.05);   // Approche Co Planet
        Vector3D waypoint1 = new Vector3D(12871.31, -12367.32, 58470.29);   // Co Planet
        Vector3D waypoint2 = new Vector3D(76418.03, 8110.16, 87685.90);   // Passage Retour
        Vector3D waypoint3 = new Vector3D(76365.45, 8330.74, 87689.96);   // Approche Co Station
        Vector3D waypoint4 = new Vector3D(76345.56, 8349.10, 87693.58);   // Co Station

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {

            if (check == true)
            {
                double gravity = rc_AstroBus.GetNaturalGravity().Length();
                rc_AstroBus.TryGetPlanetElevation(MyPlanetElevation.Surface, out Altitude);
                speed = rc_AstroBus.GetShipSpeed();



                Echo("checkList : " + check +
                     "\nstationReady : " + stationReady +
                     "\nStart Planet : " + goPlanet +
                     "\nplanetReady : " + planetReady +
                     "\nStart Station : " + goStation +
                     "\nAltitude : " + Altitude.ToString("0.0") +
                     "\nSpeed :" + speed.ToString("0.0") + " / " + rc_AstroBus.SpeedLimit.ToString("0.0") +
                     "\nStep : " + step +
                     "\n" + status +
                     rc_AstroBus.CurrentWaypoint);

                panel0.WriteText("checkList : " + check +
                                 "\nAltitude : " + Altitude.ToString("0.0") +
                                 "\nSpeed :" + speed.ToString("0.0") + " / " + rc_AstroBus.SpeedLimit.ToString("0.0") +
                                 "\nStep : " + step +
                                 "\n" + status);

                // Liste des Arguments--------------------------------------------------------
                // ---------------------------------------------------------------------------
                // ---------------------------------------------------------------------------
                if (argument == "door")
                { arguments = "door"; }
                if (arguments != "")
                { executeArgmuments(arguments); }
                // ---------------------------------------------------------------------------
                // ---------------------------------------------------------------------------
                // ---------------------------------------------------------------------------


                // Secquence de descente sur planete
                if (stationReady == true)
                {
                    if (goPlanet == true)
                    {
                        // Fermeture des portes
                        if (step == 0)
                        {
                            status = "Fermeture de la rampe";
                            foreach (var piston in pistonsRamp)
                            {
                                if (piston.CurrentPosition == 0)
                                {
                                    if (hingeRamp.Angle <= -1.5358f)
                                    {
                                        if (door.Status == DoorStatus.Closed)
                                        { step = 1.1; }
                                        else
                                        { door.ApplyAction("Open_Off"); }
                                    }
                                    else
                                    {
                                        hingeRamp.TargetVelocityRPM = -4;
                                        hingeRamp.LowerLimitDeg = -88;
                                    }
                                }
                                else
                                {
                                    piston.Velocity = -1;
                                    piston.MinLimit = 0;
                                }
                            }
                        }

                        // Déconnection
                        if (step == 1.1)
                        {
                            status = "Départ";
                            foreach (var lcd in lcd_Top)
                            { lcd.WriteText("Veuillez rester assis" + "\nEt garder votre ceinture attaché"); }
                            if (coDisconnect(co_AstroBus) == true)
                            { step = 1; }
                        }

                        // Sortie du hangar
                        if (step == 1)
                        {
                            // Verrouillage des gyroscopes afin d'éviter les accidents
                            foreach (var gyro in gyro_Rocket)
                            {
                                gyro.GyroOverride = true;
                                gyro.GyroPower = 1;
                                gyro.Pitch = 0f;
                                gyro.Yaw = 0f;
                                gyro.Roll = 0f;
                            }
                            if (speed > 20)
                            {
                                foreach (var thrust in thrusters_Hydro)
                                { thrust.ThrustOverridePercentage = 0; }
                                step = 2;
                            }
                            else
                            {
                                foreach (var thrust in thrusters_Hydro)
                                {
                                    thrust.ApplyAction("OnOff_On");
                                    if (thrust.Orientation.Forward == (Base6Directions.Direction.Backward))
                                    { thrust.ThrustOverridePercentage = 0.1f; }
                                }
                            }
                        }

                        // Initialisation du pilote Auto
                        if (step == 2)
                        {
                            // Désactive le verrouillage des gyros
                            foreach (var gyro in gyro_Rocket)
                            {
                                gyro.GyroOverride = false;
                                gyro.GyroPower = 1;
                                gyro.Pitch = 0f;
                                gyro.Yaw = 0f;
                                gyro.Roll = 0f;
                            }
                            List<MyWaypointInfo> list = new List<MyWaypointInfo>();
                            list.Clear();
                            rc_AstroBus.GetWaypointInfo(list);
                            if (list.Count == 0)
                            {
                                rc_AstroBus.AddWaypoint(waypoint0, "Approche Planete");
                                rc_AstroBus.AddWaypoint(waypoint1, "Atterissage planete");
                                rc_AstroBus.AddWaypoint(waypoint2, "Passage");
                                step = 2.1;
                            }
                            else
                            {
                                rc_AstroBus.ClearWaypoints();
                            }
                        }
                        if (step == 2.1)
                        {
                            List<MyWaypointInfo> list = new List<MyWaypointInfo>();
                            list.Clear();
                            rc_AstroBus.GetWaypointInfo(list);
                            if (list.Count == 3)
                            {
                                if (rc_AstroBus.IsAutoPilotEnabled == true)
                                { step = 3; }
                                else
                                {
                                    rc_AstroBus.SetAutoPilotEnabled(true);
                                    rc_AstroBus.SetCollisionAvoidance(true);
                                    rc_AstroBus.SpeedLimit = speedMax;

                                }
                            }
                            else
                            { step = 2; }
                        }

                        // Voyage vers la planete
                        if (step == 3)
                        {
                            parachuteControl(); // Vérifie l'altitude et la vitesse afin d'ouvrir les parachutes si besoins
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[0].Name)
                            {
                                status = "Voyage vers la planète";  // Entre dans le champs de gravité
                                if (rc_AstroBus.GetNaturalGravity().Length() > 0)
                                {
                                    if (rc_AstroBus.GetNaturalGravity().Length() < 1)
                                    {
                                        rc_AstroBus.SetCollisionAvoidance(false);
                                        status += "\nEntré dans la gravité";
                                        Echo(gravity.ToString());
                                    }
                                }

                                if (Altitude < 3000)
                                {
                                    if (Altitude > 2950)    // Réduit la vitesse par 2
                                    {
                                        foreach (var thrust in thrusters_Atmos) // Allumz les atmosphériques
                                        { thrust.ApplyAction("OnOff_On"); }
                                        foreach (var thrust in thrusters_Hydro) // Eteint les hydro
                                        { thrust.ApplyAction("OnOff_Off"); }
                                    }
                                }

                                if (Altitude < 2000)
                                {
                                    if (Altitude > 1950)    // Réduit la vitesse par 2
                                    {
                                        status += "\nCSI Tower en approche";
                                        rc_AstroBus.SpeedLimit = (speedMax / 2);
                                    }
                                }

                                if (Altitude < 1000)
                                {
                                    status += "\nSatbilisation";
                                    if (Altitude > 990)
                                    {
                                        rc_AstroBus.SpeedLimit = (speedMax / 10);
                                    }
                                }

                                if (Altitude < 300)         // Voir pour optimiser
                                {
                                    status = "Satbilisation";
                                    if (Altitude > 290)
                                    {
                                        rc_AstroBus.SpeedLimit = (speedMax / 20);
                                        step = 3.1;
                                    }
                                }
                            }
                        }

                        // Attérrissage
                        if (step == 3.1)
                        {
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[1].Name)
                            {
                                rc_AstroBus.SpeedLimit = 2;
                                status = "Attérissage";
                                step = 3.2;
                            }
                        }

                        // Vérrouillage de train d'attérrissage
                        if (step == 3.2)
                        {
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[2].Name)
                            {
                                if (rc_AstroBus.IsAutoPilotEnabled == false)
                                {
                                    if (coLanding() == true)
                                    {
                                        foreach (var thrust in thrusters_AstroBus)
                                        { thrust.ApplyAction("OnOff_Off"); }
                                        status = "Attérissage Réussi :-)";
                                        if (hingeRamp.Angle > 0)
                                        {
                                            foreach (var piston in pistonsRamp)
                                            {
                                                if (piston.CurrentPosition == 1)
                                                {
                                                    if (door.Status == DoorStatus.Open)
                                                    {
                                                        foreach (var lcd in lcd_Top)
                                                        { lcd.WriteText("Vous êtes arrivé" + "\nVous pouvez sortir"); }
                                                        stationReady = false;
                                                        goPlanet = false;
                                                        tb_goplanet.StartCountdown();
                                                        step = 0;
                                                    }
                                                    else
                                                    { door.ApplyAction("Open_On"); }
                                                }
                                                else
                                                {
                                                    piston.Velocity = 1;
                                                    piston.MaxLimit = 1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            hingeRamp.TargetVelocityRPM = 4;
                                            hingeRamp.UpperLimitDeg = 45;
                                        }
                                    }
                                }
                                else
                                { rc_AstroBus.SetAutoPilotEnabled(false); }
                            }
                        }
                    }
                    else
                    {
                        if (argument == "goPlanet")
                        { goPlanet = true; }
                    }
                }
                else
                {
                    if (planetReady == false)
                    {
                        if (coConnect(co_AstroBus) == true)
                        {
                            foreach (var thrust in thrusters_AstroBus)
                            {
                                thrust.ApplyAction("OnOff_Off");
                                { thrust.ThrustOverridePercentage = 0; }
                            }
                            if (checkHydrogen() == true)
                            {
                                if (checkBattery() == true)
                                {
                                    stationReady = true;
                                }
                            }

                        }
                    }
                }


                // Retour vers la station
                if (planetReady == true)
                {
                    if (goStation == true)
                    {
                        // Déverrouillage
                        if (step == 0)
                        {
                            status = "Déverrouillage";
                            foreach (var lcd in lcd_Top)
                            { lcd.WriteText("Maintenance" + "\nRetour à la station"); }
                            foreach (var piston in pistonsRamp)
                            {
                                if (piston.CurrentPosition == 0)
                                {
                                    if (hingeRamp.Angle <= -1.5358f)
                                    {
                                        if (door.Status == DoorStatus.Closed)
                                        { step = 0.1; }
                                        else
                                        { door.ApplyAction("Open_Off"); }
                                    }
                                    else
                                    {
                                        hingeRamp.TargetVelocityRPM = -4;
                                        hingeRamp.LowerLimitDeg = -88;
                                    }
                                }
                                else
                                {
                                    piston.Velocity = -1;
                                    piston.MinLimit = 0;
                                }
                            }
                        }

                        if (step == 0.1)
                        {
                            status = "Décollage";
                            foreach (var gyro in gyro_Rocket)
                            {
                                gyro.GyroOverride = true;
                                gyro.GyroPower = 1;
                                gyro.Pitch = 0f;
                                gyro.Yaw = 0f;
                                gyro.Roll = 0f;
                            }
                            foreach (var gear in landingGear_Rocket)
                            {
                                if (gear.IsLocked == false)
                                {
                                    foreach (var thrust in thrusters_Atmos)
                                    {
                                        rc_AstroBus.SpeedLimit = speedMax;
                                        thrust.ApplyAction("OnOff_On");
                                        if (thrust.Orientation.Forward == (Base6Directions.Direction.Down))
                                        { thrust.ThrustOverridePercentage = 0.5f; }
                                        if (thrust.Orientation.Forward == (Base6Directions.Direction.Up))
                                        { thrust.ApplyAction("OnOff_Off"); }
                                        step = 0.2;
                                    }
                                }
                                else
                                { gear.ApplyAction("SwitchLock"); }
                            }
                        }

                        if (step == 0.2)
                        {
                            status = "Décollage";
                            if (Altitude > 10000)
                            {
                                foreach (var thrust in thrusters_Atmos)
                                { thrust.ApplyAction("OnOff_Off"); }
                                foreach (var thrust in thrusters_Hydro)
                                {
                                    thrust.ApplyAction("OnOff_On");
                                    if (thrust.Orientation.Forward == (Base6Directions.Direction.Down))
                                    { thrust.ThrustOverridePercentage = 0.1f; }
                                    if (thrust.Orientation.Forward == (Base6Directions.Direction.Up))
                                    { thrust.ApplyAction("OnOff_Off"); }
                                    step = 1;
                                }
                            }
                        }
                        // Décollage
                        if (step == 1)
                        {
                            status = "Allumage des propulseurs";
                            if (rc_AstroBus.GetNaturalGravity().Length() == 0)
                            {
                                status = "Sortie de la Zone de Gravité";
                                foreach (var gyro in gyro_Rocket)
                                {
                                    gyro.GyroOverride = false;
                                    gyro.GyroPower = 1;
                                    gyro.Pitch = 0f;
                                    gyro.Yaw = 0f;
                                    gyro.Roll = 0f;
                                }
                                foreach (var thrust in thrusters_AstroBus)
                                {
                                    thrust.ThrustOverridePercentage = 0;
                                    thrust.ApplyAction("OnOff_On");
                                }
                                foreach (var thrust in thrusters_Atmos)
                                { thrust.ApplyAction("OnOff_Off"); }
                                step = 2;
                            }
                        }

                        // Initialisation du pilote Auto
                        if (step == 2)
                        {
                            List<MyWaypointInfo> list = new List<MyWaypointInfo>();
                            list.Clear();
                            rc_AstroBus.GetWaypointInfo(list);
                            if (list.Count == 0)
                            {
                                rc_AstroBus.AddWaypoint(waypoint2, "Passage");
                                rc_AstroBus.AddWaypoint(waypoint3, "co Station_Appr");
                                rc_AstroBus.AddWaypoint(waypoint4, "co Station");
                                step = 2.1;
                            }
                            else
                            {
                                rc_AstroBus.ClearWaypoints();
                            }
                        }
                        if (step == 2.1)
                        {
                            List<MyWaypointInfo> list = new List<MyWaypointInfo>();
                            list.Clear();
                            rc_AstroBus.GetWaypointInfo(list);
                            if (list.Count == 3)
                            {
                                if (rc_AstroBus.IsAutoPilotEnabled == true)
                                { step = 3; }
                                else
                                {
                                    rc_AstroBus.SetAutoPilotEnabled(true);
                                    rc_AstroBus.SetCollisionAvoidance(true);
                                    rc_AstroBus.SpeedLimit = speedMax;
                                    tb_SlowDown.StartCountdown();
                                }
                            }
                            else
                            { step = 2; }
                        }

                        // Voyage vers la station
                        if (step == 3)
                        {
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[2].Name)
                            {
                                status = "Voyage vers la station";
                                if (argument == "slowDown")
                                {
                                    rc_AstroBus.SpeedLimit = (speedMax / 20);
                                    step = 3.1;
                                }
                            }

                        }

                        // Arrivé sur point de passage
                        if (step == 3.1)
                        {
                            status = "Station en Approche";
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[3].Name)
                            {
                                rc_AstroBus.SetCollisionAvoidance(false);
                                status = "Entrer dans le hangar";
                                rc_AstroBus.SpeedLimit = 10;
                                step = 3.2;
                            }
                        }

                        if (step == 3.2)
                        {
                            if (rc_AstroBus.CurrentWaypoint.Name == waypoints[4].Name)
                            {
                                status = "Connection en cours";
                                rc_AstroBus.SpeedLimit = 2;
                                foreach (var thrust in thrusters_Hydro)
                                { thrust.ApplyAction("OnOff_Off"); }
                                foreach (var thrust in thrusters_HydroPrecision)
                                { thrust.ApplyAction("OnOff_On"); }
                                step = 3.3;
                            }
                        }
                        if (step == 3.3)
                        {
                            if (co_AstroBus.Status == MyShipConnectorStatus.Connectable)
                            {
                                if (rc_AstroBus.IsAutoPilotEnabled == false)
                                {
                                    Echo("alignement" + align().ToString());
                                    if (align() == true)
                                    {
                                        if (coConnect(co_AstroBus) == true)
                                        {
                                            status = "Connection réussi";
                                            foreach (var thrust in thrusters_AstroBus)
                                            { thrust.ApplyAction("OnOff_Off"); }
                                            planetReady = false;
                                            goStation = false;
                                            step = 0;
                                        }
                                    }
                                }
                                else
                                { rc_AstroBus.SetAutoPilotEnabled(false); }
                            }
                            else
                            { co_AstroBus.ApplyAction("OnOff_On"); }
                        }
                    }
                    else
                    {
                        if (argument == "goStation")
                        { goStation = true; }
                    }
                }
                else
                {
                    if (stationReady == false)
                    {
                        foreach (var gear in landingGear_Rocket)
                        {
                            if (gear.IsLocked == true)
                            {
                                if (rc_AstroBus.IsAutoPilotEnabled == false)
                                {
                                    foreach (var thrust in thrusters_AstroBus)
                                    {
                                        thrust.ApplyAction("OnOff_Off");
                                        thrust.SetValueFloat("Override", 0.1f);
                                    }
                                    planetReady = true;
                                }
                                else
                                { rc_AstroBus.SetAutoPilotEnabled(false); }
                            }
                            else
                            { gear.ApplyAction("SwitchLock"); }
                        }
                    }
                }

            }
            else
            {
                if (err_TXT == "")
                {
                    if (checkList() == true)
                    { check = true; }
                    else
                    {
                        if (err_TXT.Length < 500)
                        { Echo(err_TXT); }
                    }
                }
                return;
            }
        }




        public bool checkBattery()
        {
            bool isCharged = false;
            int batteryCount = 0;
            for (int i = 0; i < battery_AstroBus.Count; i++)
            {
                double max = battery_AstroBus[i].MaxStoredPower;
                if (max - battery_AstroBus[i].CurrentStoredPower <= 0.2)
                {
                    batteryCount++;
                    if (battery_AstroBus[i].ChargeMode != ChargeMode.Discharge)
                    { battery_AstroBus[i].ApplyAction("Discharge"); }
                }
                else
                {
                    panel0.WriteText("Batterie \nfaible");
                    if (battery_AstroBus[i].ChargeMode != ChargeMode.Recharge)
                    { battery_AstroBus[i].ApplyAction("Recharge"); }
                }
            }
            if (batteryCount == battery_AstroBus.Count)
            { isCharged = true; }

            return isCharged;
        }





        public bool checkHydrogen()
        {
            bool isCharged = false;
            int tankCount = 0;
            for (int i = 0; i < hydrogenTank_AstroBus.Count; i++)
            {
                if (hydrogenTank_AstroBus[i].FilledRatio == 1)
                {
                    tankCount++;
                    if (hydrogenTank_AstroBus[i].Stockpile == true)
                    { hydrogenTank_AstroBus[i].Stockpile = false; }

                }
                else
                {
                    panel0.WriteText("Réservoir Hydrogène\nest faible");
                    if (hydrogenTank_AstroBus[i].Stockpile == false)
                    { hydrogenTank_AstroBus[i].Stockpile = true; }
                }
            }
            if (tankCount == hydrogenTank_AstroBus.Count)
            { isCharged = true; }
            return isCharged;
        }


        public void executeArgmuments(String _arguments)
        {
            if (_arguments == "door")
            { openCloseDoor(); }
        }

        public bool openCloseDoor()
        {
            bool doorStatus = false;
            if (door.Status == DoorStatus.Open)
            {
                foreach (var piston in pistonsRamp)
                {
                    if (piston.CurrentPosition == 0)
                    {
                        if (hingeRamp.Angle <= -1.5358f)
                        {
                            door.ApplyAction("Open_Off");
                            Echo("Porte fermée");
                            arguments = "";
                            doorStatus = false;
                        }
                        else
                        {
                            hingeRamp.TargetVelocityRPM = -4;
                            hingeRamp.LowerLimitDeg = -88;
                        }
                    }
                    else
                    {
                        piston.Velocity = -1;
                        piston.MinLimit = 0;
                    }
                }
            }
            else
            {
                if (door.Status == DoorStatus.Closed)
                {
                    if (hingeRamp.Angle > -0.02)
                    {
                        foreach (var piston in pistonsRamp)
                        {
                            if (piston.CurrentPosition == 1)
                            {
                                door.ApplyAction("Open_On");
                                Echo("Porte Ouverte");
                                arguments = "";
                                doorStatus = true;
                            }
                            else
                            {
                                piston.Velocity = 1;
                                piston.MaxLimit = 1;
                            }
                        }
                    }
                    else
                    {
                        hingeRamp.TargetVelocityRPM = 4;
                        if (co_AstroBus.Status == MyShipConnectorStatus.Connected)
                        { hingeRamp.UpperLimitDeg = 0; }
                        else
                        { hingeRamp.UpperLimitDeg = 45; }
                    }
                }
            }

            return doorStatus;
        }

        public void parachuteControl()
        {
            if (Altitude < 500)
            {
                if (speed > 250)
                {
                    foreach (var parachute in parachute_AstroBus)
                    {
                        if (parachute.Status == DoorStatus.Open)
                        {
                            panel0.WriteText("ALERTE, CRASH !!!!!!!");
                            foreach (var lcd in lcd_Top)
                            { panel0.WriteText("ALERTE, CRASH !!!!!!!"); }
                        }
                        else
                        { parachute.OpenDoor(); }
                    }
                }
            }
        }
        public bool align()
        {
            bool isAligned = false;
            status = "Alignement";

            listEntitiesDetected.Clear();
            sensorExt_AstroBus.DetectedEntities(listEntitiesDetected);
            Echo(listEntitiesDetected.Count + " Blocks detected");
            if (listEntitiesDetected.Count == 0)
            {
                foreach (var gyro in gyro_Rocket)
                {
                    gyro.GyroOverride = true;
                    gyro.GyroPower = 1;
                    gyro.Pitch = 0f;
                    gyro.Yaw = -0.3f;
                    gyro.Roll = 0f;
                }
            }
            if (listEntitiesDetected.Count > 0)
            {
                foreach (var gyro in gyro_Rocket)
                {
                    gyro.GyroOverride = false;
                    gyro.GyroPower = 1;
                    gyro.Pitch = 0f;
                    gyro.Yaw = 0f;
                    gyro.Roll = 0f;
                }
                if (coConnect(co_AstroBus) == true)
                { isAligned = true; }
            }
            return isAligned;
        }
        public bool coDisconnect(IMyShipConnector _co)
        {
            bool isConnected = false;
            if (_co.Status != MyShipConnectorStatus.Connected)
            { isConnected = true; }
            else
            { _co.ApplyAction("OnOff_Off"); }
            return isConnected;
        }

        public bool coConnect(IMyShipConnector _co)
        {
            bool isConnected = false;
            if (_co.Status == MyShipConnectorStatus.Connected)
            { isConnected = true; }
            else
            {
                _co.ApplyAction("SwitchLock");
                _co.ApplyAction("OnOff_On");
            }
            return isConnected;
        }
        public bool coLanding()
        {
            bool isConnected = false;
            foreach (var gear in landingGear_Rocket)
            {
                if (gear.IsLocked == true)
                { isConnected = true; }
                else
                { gear.ApplyAction("SwitchLock"); }
            }
            return isConnected;
        }
        public bool checkList()
        {
            bool isOK = true;
            var nameGrid = Me.CubeGrid.CustomName;

            panel0 = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - [LCD]") as IMyTextSurface;
            if (panel0 == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - [LCD] !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - [LCD] !!"; }
                isOK = false;
            }
            else
            {
                panel0.ContentType = ContentType.TEXT_AND_IMAGE;
                panel0.FontSize = 1.5f;
                panel0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            }

            List<IMyGasTank> tankGas = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tankGas);
            if (tankGas.Count != 0)
            {
                foreach (var tank in tankGas)
                {
                    if (tank.CubeGrid.CustomName == nameGrid)
                    { hydrogenTank_AstroBus.Add(tank); }
                }
            }
            if (hydrogenTank_AstroBus.Count == 0)
            {
                if (err_TXT.Contains("No hydrogen Tank !!") == false)
                { err_TXT += "\nNo hydrogen Tank !!"; }
                isOK = false;
            }

            List<IMyParachute> parachutes = new List<IMyParachute>();
            GridTerminalSystem.GetBlocksOfType<IMyParachute>(parachutes);
            if (parachutes.Count != 0)
            {
                foreach (var parachute in parachutes)
                {
                    if (parachute.CubeGrid.CustomName == nameGrid)
                    { parachute_AstroBus.Add(parachute); }
                }
            }
            if (parachute_AstroBus.Count == 0)
            {
                if (err_TXT.Contains("No parachutes !!") == false)
                { err_TXT += "\nNo parachutes !!"; }
                isOK = false;
            }
            else
            {
                foreach (var parachute in parachute_AstroBus)
                {
                    if (parachute.InventoryCount == 0)
                    {
                        parachute.ApplyAction("OnOff_On");
                        if (err_TXT.Contains(parachute.CustomName + " est vide") == false)
                        { err_TXT += parachute.CustomName + " est vide"; }
                        isOK = false;
                    }
                }
            }

            List<IMyBatteryBlock> batterys = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batterys);
            if (batterys.Count != 0)
            {
                foreach (var battery in batterys)
                {
                    if (battery.CubeGrid.CustomName == nameGrid)
                    { battery_AstroBus.Add(battery); }
                }
            }
            if (battery_AstroBus.Count == 0)
            {
                if (err_TXT.Contains("No battery !!") == false)
                { err_TXT += "\nNo battery !!"; }
                isOK = false;
            }

            List<IMyTextPanel> lcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcds);
            if (lcds.Count != 0)
            {
                foreach (var lcd in lcds)
                {
                    if (lcd.CustomName.Contains("Astro Bus#2 - LCD Coin Passager"))
                    { lcd_Top.Add(lcd); }
                }
            }
            if (lcd_Top.Count == 0)
            {
                if (err_TXT.Contains("No Astro Bus#2 - LCD Coin Passager !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - LCD Coin Passager !!"; }
                isOK = false;
            }
            else
            {
                for (int i = 0; i < lcd_Top.Count; i++)
                {
                    lcd_Top[i].ContentType = ContentType.TEXT_AND_IMAGE;
                    lcd_Top[i].FontSize = 2f;
                    lcd_Top[i].Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                    lcd_Top[i].WriteText("Passager " + i);
                }
            }


            List<IMyLandingGear> gears = new List<IMyLandingGear>();
            GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(gears);
            if (gears.Count != 0)
            {
                foreach (var gear in gears)
                {
                    if (gear.CubeGrid.CustomName == nameGrid)
                    { landingGear_Rocket.Add(gear); }
                }
            }
            if (landingGear_Rocket.Count == 0)
            {
                if (err_TXT.Contains("No Langing Gear !!") == false)
                { err_TXT += "\nNo Langing Gear !!"; }
                isOK = false;
            }
            else
            {
                foreach (var gear in landingGear_Rocket)
                {
                    gear.ApplyAction("OnOff_On");
                    gear.AutoLock = false;
                }
            }

            List<IMyGyro> gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            if (gyros.Count != 0)
            {
                foreach (var gyro in gyros)
                {
                    if (gyro.CubeGrid.CustomName == nameGrid)
                    { gyro_Rocket.Add(gyro); }
                }
            }
            if (gyro_Rocket.Count == 0)
            {
                if (err_TXT.Contains("No gyro !!") == false)
                { err_TXT += "\nNo gyro !!"; }
                isOK = false;
            }
            else
            {
                foreach (var gyro in gyro_Rocket)
                {
                    gyro.GyroOverride = false;
                    gyro.GyroPower = 1;
                    gyro.Pitch = 0f;
                    gyro.Yaw = 0f;
                    gyro.Roll = 0f;
                }
            }

            hingeRamp = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - Hinge Ramp") as IMyMotorAdvancedStator;
            if (hingeRamp == null)
            {
                if (err_TXT.Contains("No Astro Bus - Hinge Ramp !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - Hinge Ramp !!"; }
                isOK = false;
            }
            else
            {
                List<IMyExtendedPistonBase> pistons = new List<IMyExtendedPistonBase>();
                GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(pistons);
                if (pistons.Count != 0)
                {
                    foreach (var piston in pistons)
                    {
                        if (piston.CubeGrid.CustomName == hingeRamp.TopGrid.CustomName)
                        { pistonsRamp.Add(piston); }
                    }
                }

                if (pistonsRamp.Count == 0)
                {
                    if (err_TXT.Contains("No Pistons Door !!") == false)
                    { err_TXT += "\nNo Pistons Door !!"; }
                    isOK = false;
                }
            }

            rc_AstroBus = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - Remote Control") as IMyRemoteControl;
            if (rc_AstroBus == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - Remote Control !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - Remote Control !!"; }
                isOK = false;
            }
            else
            {
                rc_AstroBus.ClearWaypoints();
                rc_AstroBus.AddWaypoint(waypoint0, "Approche Planete");
                rc_AstroBus.AddWaypoint(waypoint1, "Atterissage planete");
                rc_AstroBus.AddWaypoint(waypoint2, "Passage");
                rc_AstroBus.AddWaypoint(waypoint3, "co Station_Appr");
                rc_AstroBus.AddWaypoint(waypoint4, "co Station");

                waypoints = new List<MyWaypointInfo>();
                waypoints.Clear();
                rc_AstroBus.SetAutoPilotEnabled(false);
                rc_AstroBus.GetWaypointInfo(waypoints);
                rc_AstroBus.FlightMode = FlightMode.OneWay;
                rc_AstroBus.ClearWaypoints();

                if (waypoints.Count == 0)
                {
                    if (err_TXT.Contains("No road !!") == false)
                    { err_TXT += "\nNo road !!"; }
                    isOK = false;
                }
            }

            co_AstroBus = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - Connector") as IMyShipConnector;
            if (co_AstroBus == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - Connector !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - Connector !!"; }
                isOK = false;
            }

            tb_SlowDown = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - BM - SlowDown") as IMyTimerBlock;
            if (tb_SlowDown == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - BM - SlowDown !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - BM - SlowDown !!"; }
                isOK = false;
            }
            else
            { tb_SlowDown.ApplyAction("OnOff_On"); }

            tb_Start = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - BM - Start") as IMyTimerBlock;
            if (tb_Start == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - BM - Start !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - BM - Start !!"; }
                isOK = false;
            }
            else
            { tb_Start.ApplyAction("OnOff_On"); }

            tb_goplanet = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - BM - Retour Station") as IMyTimerBlock;
            if (tb_goplanet == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - BM - Retour Station !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - BM - Retour Station !!"; }
                isOK = false;
            }
            else
            { tb_goplanet.ApplyAction("OnOff_On"); }

            door = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - Door") as IMyDoor;
            if (door == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - Door !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - Door !!"; }
                isOK = false;
            }

            sensorExt_AstroBus = GridTerminalSystem.GetBlockWithName("Astro Bus#2 - Sensor Exterior") as IMySensorBlock;
            if (sensorExt_AstroBus == null)
            {
                if (err_TXT.Contains("No Astro Bus#2 - Sensor Exterior !!") == false)
                { err_TXT += "\nNo Astro Bus#2 - Sensor Exterior !!"; }
                isOK = false;
            }
            else
            {
                sensorExt_AstroBus.DetectAsteroids = false;
                sensorExt_AstroBus.DetectEnemy = false;
                sensorExt_AstroBus.DetectFloatingObjects = false;
                sensorExt_AstroBus.DetectLargeShips = false;
                sensorExt_AstroBus.DetectPlayers = false;
                sensorExt_AstroBus.DetectSmallShips = false;
                sensorExt_AstroBus.DetectStations = true;
                sensorExt_AstroBus.DetectSubgrids = false;
                sensorExt_AstroBus.LeftExtend = 0.1f;
                sensorExt_AstroBus.RightExtend = 0.1f;
                sensorExt_AstroBus.BottomExtend = 0.1f;
                sensorExt_AstroBus.TopExtend = 0.1f;
                sensorExt_AstroBus.BackExtend = 0.1f;
                sensorExt_AstroBus.FrontExtend = 4.54f;
                sensorExt_AstroBus.ApplyAction("OnOff_On");
            }

            // Déclaration des Thrusters
            List<IMyThrust> thrusters = new List<IMyThrust>();

            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            if (thrusters.Count > 0)
            {
                foreach (var thruster in thrusters)
                {
                    if (thruster.CubeGrid.CustomName == nameGrid)
                    { thrusters_AstroBus.Add(thruster); }
                }
            }

            if (thrusters_AstroBus.Count == 0)
            {
                if (err_TXT.Contains("No thrusters !!") == false)
                { err_TXT += "\nNo thrusters !!"; }
                isOK = false;
            }
            else
            {
                foreach (var thrust in thrusters_AstroBus)
                {
                    { thrust.ThrustOverridePercentage = 0; }
                    thrust.ApplyAction("OnOff_On");

                    if (thrust.BlockDefinition.ToString().Contains("Atmospheric"))
                    { thrusters_Atmos.Add(thrust); }

                    if (thrust.BlockDefinition.ToString().Contains("Hydrogen"))
                    { thrusters_Hydro.Add(thrust); }

                    if (thrust.CustomName.Contains("Propulseur à hydrogène Precision"))
                    { thrusters_HydroPrecision.Add(thrust); }
                }
            }
            if (thrusters_HydroPrecision.Count == 0)
            {
                if (err_TXT.Contains("No Propulseur à hydrogène Precision !!") == false)
                { err_TXT += "\nNo Propulseur à hydrogène Precision !!"; }
                isOK = false;
            }

            if (thrusters_Atmos.Count == 0)
            {
                if (err_TXT.Contains("No thrusters Atmosphérique !!") == false)
                { err_TXT += "\nNo thrusters Atmosphérique !!"; }
                isOK = false;
            }

            if (thrusters_Hydro.Count == 0)
            {
                if (err_TXT.Contains("No thrusters Hydro !!") == false)
                { err_TXT += "\nNo thrusters Hydro !!"; }
                isOK = false;
            }
            else
            {
                foreach (var thrust in thrusters_Hydro)
                {
                    if (thrust.Orientation.Forward == (Base6Directions.Direction.Backward))
                    { thrusters_Up.Add(thrust); }
                }
            }

            if (thrusters_Up.Count == 0)
            {
                if (err_TXT.Contains("No thrusters UP !!") == false)
                { err_TXT += "\nNo thrusters UP !!"; }
                isOK = false;
            }

            return isOK;
        }
    }
}
