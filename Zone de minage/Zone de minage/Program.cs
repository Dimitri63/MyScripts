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
        List<IMyCargoContainer> cargoList = new List<IMyCargoContainer>();
        List<IMyExtendedPistonBase> pistonsList = new List<IMyExtendedPistonBase>();
        List<IMyShipDrill> drillList = new List<IMyShipDrill>();
        IMyMotorAdvancedStator rotorDrill;
        IMyTextSurface panel0;

        string err_TXT = "";
        string statusInventory = "";
        string statusInvGraph = "";
        string status = "";
        bool check = false;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
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
            panel0 = Me.GetSurface(0);
            panel0.ContentType = ContentType.TEXT_AND_IMAGE;
            panel0.FontSize = 1.2f;
            panel0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

            panel0.WriteText(status + "\n\nCargo Inventory : \n" + statusInventory +
                                      "\n" + statusInvGraph);
            Echo(status + "\n\nCargo Inventory : \n" + statusInventory +
                                      "\n" + statusInvGraph);
            if (check == true)
            {
                if (inventoryCargoFull() == true)
                {
                    if (!status.Contains("Cargo Full !"))
                    { status += "\nCargo Full !"; }
                    foreach (var piston in pistonsList)
                    {
                        if (piston.Enabled == true)
                        { piston.ApplyAction("OnOff_Off"); }
                    }
                    foreach (var drill in drillList)
                    {
                        if (drill.Enabled == true)
                        { drill.ApplyAction("OnOff_Off"); }
                    }
                    if (rotorDrill.TargetVelocityRPM != 0)
                    { rotorDrill.TargetVelocityRPM = 0; }

                }
                else
                {
                    float minLimit = rotorDrill.LowerLimitRad;
                    float maxLimit = rotorDrill.UpperLimitRad;
                    float currentPos = rotorDrill.Angle;
                    status = "Mining area Ready" + "timer = " +
                    "\nAngle = " + currentPos +
                             "\nMinLimit = " + minLimit +
                             "\nMaxLimit = " + maxLimit;
                    foreach (var piston in pistonsList)
                    {
                        if (piston.Enabled == false)
                        { piston.ApplyAction("OnOff_On"); }
                        piston.Velocity = 0.001f;
                    }
                    foreach (var drill in drillList)
                    {
                        if (drill.Enabled == false)
                        { drill.ApplyAction("OnOff_On"); }
                    }
                    if (rotorDrill.Angle <= rotorDrill.LowerLimitRad)
                    { rotorDrill.TargetVelocityRPM = 1; }
                    else
                    {
                        if (rotorDrill.Angle >= rotorDrill.UpperLimitRad)
                        { rotorDrill.TargetVelocityRPM = -1; }
                    }
                }
            }
            else
            {
                status = "Mining area not Ready";
                if (err_TXT == "")
                {
                    if (checkList() == true)
                    { check = true; }
                }
                else
                { Echo(err_TXT); }
            }

        }

        public bool checkList()
        {
            bool isOK = true;
            string nameGrid = Me.CubeGrid.CustomName;

            List<IMyCargoContainer> _cargoList = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(_cargoList);
            if (_cargoList.Count > 0)
            {
                foreach (var cargo in _cargoList)
                {
                    if (cargo.CubeGrid.CustomName == nameGrid)
                    { cargoList.Add(cargo); }
                }
                if (cargoList.Count == 0)
                {
                    isOK = false;
                    if (!err_TXT.Contains("Error, cargo no found"))
                    { err_TXT += "\nError, cargo no found"; }
                }
            }
            List<IMyExtendedPistonBase> _pistonsList = new List<IMyExtendedPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(_pistonsList);
            if (_pistonsList.Count > 0)
            {
                foreach (var piston in _pistonsList)
                {
                    if (piston.CubeGrid.CustomName == nameGrid)
                    { pistonsList.Add(piston); }
                }
                if (pistonsList.Count == 0)
                {
                    isOK = false;
                    if (!err_TXT.Contains("Error, piston no found"))
                    { err_TXT += "\nError, piston no found"; }
                }
                else
                {
                    List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();
                    GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(rotors);
                    if (rotors.Count > 0)
                    {
                        foreach (var piston in pistonsList)
                        {
                            string piston_TopGridname = piston.TopGrid.CustomName;
                            foreach (var rotor in rotors)
                            {
                                if (rotor.CubeGrid.CustomName == piston_TopGridname)
                                { rotorDrill = rotor; }
                            }
                            if (rotorDrill == null)
                            {
                                isOK = false;
                                if (!err_TXT.Contains("Error, rotor no found"))
                                { err_TXT += "\nError, rotor no found"; }
                            }
                        }
                    }
                }
            }
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drillList);
            if (drillList.Count == 0)
            {
                isOK = false;
                if (!err_TXT.Contains("Error, drill no found"))
                { err_TXT += "\nError, drill no found"; }
            }

            return isOK;
        }

        public bool inventoryCargoFull()
        {
            bool isFull = false;
            int cargoCount = 0;
            MyFixedPoint currentinventory = 0;
            MyFixedPoint maxinventory = 0;
            foreach (var cargo in cargoList)
            {
                if (cargo.GetInventory(0).IsFull)
                { cargoCount++; }
                else
                {
                    currentinventory += cargo.GetInventory(0).CurrentVolume;
                    maxinventory += cargo.GetInventory(0).MaxVolume;
                }
            }
            double currentInv = AsDouble(currentinventory);
            double maxInv = AsDouble(maxinventory);
            if (currentInv <= ((maxInv / 100) * 10))
            {
                statusInventory = "10%";
                statusInvGraph = "#####################" +
                               "\n#||||" +
                               "\n#####################";
            }
            else
            {
                if (currentInv <= ((maxInv / 100) * 20))
                {
                    statusInventory = "20%";
                    statusInvGraph = "######################" +
                                   "\n#||||||||" +
                                   "\n######################";
                }
                else
                {
                    if (currentInv <= ((maxInv / 100) * 30))
                    {
                        statusInventory = "30%";
                        statusInvGraph = "######################" +
                                       "\n#||||||||||||" +
                                       "\n######################";
                    }
                    else
                    {
                        if (currentInv <= ((maxInv / 100) * 40))
                        {
                            statusInventory = "40%";
                            statusInvGraph = "######################" +
                                           "\n#||||||||||||||||" +
                                           "\n######################";
                        }
                        else
                        {
                            if (currentInv <= ((maxInv / 100) * 50))
                            {
                                statusInventory = "50%";
                                statusInvGraph = "######################" +
                                               "\n#||||||||||||||||||||" +
                                               "\n######################";
                            }
                            else
                            {
                                if (currentInv <= ((maxInv / 100) * 60))
                                {
                                    statusInventory = "60%";
                                    statusInvGraph = "######################" +
                                                   "\n#||||||||||||||||||||||||" +
                                                   "\n######################";
                                }
                                else
                                {
                                    if (currentInv <= ((maxInv / 100) * 70))
                                    {
                                        statusInventory = "70%";
                                        statusInvGraph = "######################" +
                                                       "\n#||||||||||||||||||||||||||||" +
                                                       "\n######################";
                                    }
                                    else
                                    {
                                        if (currentInv <= ((maxInv / 100) * 80))
                                        {
                                            statusInventory = "80%";
                                            statusInvGraph = "######################" +
                                                           "\n#||||||||||||||||||||||||||||||||" +
                                                           "\n######################";
                                        }
                                        else
                                        {
                                            if (currentInv <= ((maxInv / 100) * 90))
                                            {
                                                statusInventory = "90%";
                                                statusInvGraph = "######################" +
                                                               "\n#||||||||||||||||||||||||||||||||||||" +
                                                               "\n######################";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (cargoCount == cargoList.Count)
            { isFull = true; }
            return isFull;
        }

        public double AsDouble(MyFixedPoint point)
        {
            return (double)point; // double.Parse(point.ToString());
        }
    }
}
