using UnityEngine;
using System;
using System.Linq;

namespace ServerMonitor
{
    public class Main : ModMeta
    {
        //This function is used to generate the content in the "Mods" section of the options window
        //The behaviors array contains all behaviours that have been spawned for this mod, one for each implementation
        public void ConstructOptionsScreen(RectTransform parent, ModBehaviour[] behaviours)
        {
            //Start by spawning a label
            var label = WindowManager.SpawnLabel();
            label.text = "Server Monitor is a simple monitoring utility to show server utilization.";
            //Add the label to the mod panel at (0, 0) with 96 width and 32 height, anchored to the top left
            WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 390, 64),
                new Rect(0, 0, 0, 0));

            //Add an inputfield where the user can write text to change the maximum floor level
            //var inputField = WindowManager.SpawnInputbox();
            ////We need a reference to a behavior to read and write from the mod settings file
            //var floorBehavior = behaviours.OfType<FloorBehaviour>().First();
            //inputField.text = floorBehavior.LoadSetting("MaxFloor", 20).ToString();
            //inputField.onValueChange.AddListener(x =>
            //{
            //    try
            //    {
            //        var max = Convert.ToInt32(x);
            //        floorBehavior.MaxFloor = max;
            //        //Settings are saved to a text file with the dll, all ModBehaviours has a function to do this
            //        floorBehavior.SaveSetting("MaxFloor", max.ToString());
            //    }
            //    catch (Exception)
            //    {
            //    }
            //});
            //WindowManager.AddElementToElement(inputField.gameObject, parent.gameObject, new Rect(100, 0, 96, 32),
            //    new Rect(0, 0, 0, 0));
        }

        public string Name
        {
            //This will be displayed as the header in the Options window
            get { return "Server Monitor"; }
        }
    }
}
