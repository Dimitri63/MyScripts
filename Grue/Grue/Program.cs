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
        IMyTextSurface panel0;

        IMyShipController controllerS1;
        List<IMyExtendedPistonBase> PistonsForwardS1 = new List<IMyExtendedPistonBase>();
        List<IMyExtendedPistonBase> PistonsElvationS1 = new List<IMyExtendedPistonBase>();
        IMyMotorAdvancedStator RotorBaseS1;
        IMyMotorAdvancedStator RotorArm_1S1;
        IMyMotorAdvancedStator RotorArm_2S1;
        IMyMotorAdvancedStator HingeArm_1S1;
        IMyMotorAdvancedStator HingeArm_2S1;

        bool check = false;
        string err_TXR = "";

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
            Echo("Construction site#1 ready : " + check);

            if (check == true)
            {

                var moveX_1 = controllerS1.MoveIndicator.X;
                var moveY_1 = controllerS1.MoveIndicator.Y;
                var moveZ_1 = controllerS1.MoveIndicator.Z;
                var rotateX_1 = controllerS1.RotationIndicator.X;
                var rotateY_1 = controllerS1.RotationIndicator.Y;

                // Construction Site#1
                if (!RotorBaseS1.RotorLock)
                { RotorBaseS1.TargetVelocityRPM = ((rotateY_1) / 4); }
                if (!RotorArm_1S1.RotorLock)
                { RotorArm_1S1.TargetVelocityRPM = ((-rotateX_1) / 2); }
                if (!RotorArm_2S1.RotorLock)
                { RotorArm_2S1.TargetVelocityRPM = ((rotateX_1) / 4); }
                if (!HingeArm_1S1.RotorLock)
                { HingeArm_1S1.TargetVelocityRPM = rotateX_1; }
                if (!HingeArm_2S1.RotorLock)
                { HingeArm_2S1.TargetVelocityRPM = ((rotateY_1) / 4); }

                foreach (var piston in PistonsForwardS1)
                { piston.Velocity = -moveZ_1; }
                foreach (var piston in PistonsElvationS1)
                { piston.Velocity = moveY_1; }
            }
            else
            {
                if (err_TXR == "")
                {
                    if (checkList() == true)
                    { check = true; }
                }
                else
                {
                    Echo(err_TXR);
                }
            }

        }

        public bool checkList()
        {
            bool isOK = true;

            panel0 = Me.GetSurface(0);
            panel0.ContentType = ContentType.TEXT_AND_IMAGE;
            panel0.FontSize = 1;
            panel0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

            // Identify the controller Site#1
            controllerS1 = GridTerminalSystem.GetBlockWithName("Controller Construction (1)") as IMyShipController;
            if (controllerS1 == null)
            {
                isOK = false;
                if (!err_TXR.Contains("Error, Controller Construction (1) no found !"))
                { err_TXR += "\nError, Controller Construction (1) no found !"; }
            }

            // Identify the rotors
            List<IMyMotorAdvancedStator> Rotors = new List<IMyMotorAdvancedStator>();
            GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(Rotors);
            if (Rotors.Count > 0)
            {
                foreach (var rotor in Rotors)
                {

                    if (rotor.CustomName == "Rotor Construction (1) Base")
                    { RotorBaseS1 = rotor; }
                    if (rotor.CustomName == "Rotor Construction (1) Arm#1")
                    { RotorArm_1S1 = rotor; }
                    if (rotor.CustomName == "Rotor Construction (1) Arm#2")
                    { RotorArm_2S1 = rotor; }
                    if (rotor.CustomName == "Hinge Construction (1) Arm#1")
                    { HingeArm_1S1 = rotor; }
                    if (rotor.CustomName == "Hinge Construction (1) Arm#2")
                    { HingeArm_2S1 = rotor; }
                }
                if (RotorArm_1S1 == null)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Rotor Construction (1) Arm#1 no found !"))
                    { err_TXR += "\nError, Rotor Construction (1) Arm#1 no found !"; }
                }
                if (RotorArm_2S1 == null)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Rotor Construction (1) Arm#2 no found !"))
                    { err_TXR += "\nError, Rotor Construction (1) Arm#2 no found !"; }
                }
                if (HingeArm_1S1 == null)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Hinge Construction (1) Arm#1 no found !"))
                    { err_TXR += "\nError, Hinge Construction (1) Arm#1 no found !"; }
                }
                if (HingeArm_2S1 == null)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Hinge Construction (1) Arm#2 no found !"))
                    { err_TXR += "\nError, Hinge Construction (1) Arm#2 no found !"; }
                }
                if (RotorBaseS1 == null)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Rotor Construction (1) Base no found !"))
                    { err_TXR += "\nError, Rotor Construction (2) Base no found !"; }
                }
            }

            // Identify the pistons
            List<IMyExtendedPistonBase> pistons = new List<IMyExtendedPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(pistons);
            if (pistons.Count > 0)
            {
                foreach (var piston in pistons)
                {

                    if (piston.CustomName.Contains("Piston Construction (1) Elv"))
                    { PistonsElvationS1.Add(piston); }
                    if (piston.CustomName.Contains("Piston Construction (1) Forward"))
                    { PistonsForwardS1.Add(piston); }
                }

                if (PistonsForwardS1.Count == 0)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Piston Construction (1) Forward no found !"))
                    { err_TXR += "\nError, Piston Construction (1) Forward no found !"; }
                }
                if (PistonsElvationS1.Count == 0)
                {
                    isOK = false;
                    if (!err_TXR.Contains("Error, Piston Construction (1) Elv no found !"))
                    { err_TXR += "\nError, Piston Construction (1) Elv no found !"; }
                }
            }

            return isOK;
        }
    }
}
