using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ServerMonitor
{
    class ServerMonitorBehaviour : ModBehaviour
    {
        #region Definitions

        public static float UtilizationWarnState = 0.75f;
        public static float UtilizationAlarmState = 0.9f;

        public enum State
        {
            Normal,
            Warning,
            Alarm
        }

        public struct DataPoint
        {
            public SDateTime DPDateTime;
            public string ServerName;
            public float Load;
            public float Capacity;
            public int JobCount;
            public int NodeCount;
            public uint DID;
            public float Utilization;
            public State MyState;
        }

        #endregion

        #region Unity Calls

        void Start()
        {
            //All ModBehaviours has a function to load settings from the mod's settings file
            //Note that everything is saved in strings
            //This function uses the default string converter for the generic type argument
            SetStrings();
        }

        void Update()
        {
            if (_modActive && GameSettings.Instance != null)
            {
                InitializeUI();
                GatherDataPoints();
                RecordDataPoints();
                UpdateUI();
            }
        }

        //Use this method to clean up your mod
        //The MonoBehaviour is disabled immediatly after this method has been called
        public override void OnDeactivate()
        {
            _modActive = false;
        }

        //Use this method to initiate your mod
        //This method will ALWAYS be called when the game launches if this mod is activated
        //and everytime the mod is toggled on by the user
        public override void OnActivate()
        {
            _modActive = true;
        }

        #endregion

        #region Data Collection

        private void GatherDataPoints()
        {
            _currentDataPoints = new List<ServerMonitor.ServerMonitorBehaviour.DataPoint>();
            SDateTime now = SDateTime.Now();

            foreach(Server server in GameSettings.Instance.GetAllServers())
            {
                DataPoint dp = new DataPoint();

                dp.DPDateTime = now;
                dp.ServerName = server.TServerName;
                dp.Load = server.Items.Select(itm => Mathf.Round(itm.GetLoadRequirement() * 2.2f)).Sum();
                dp.Capacity = server.PowerSum;
                dp.JobCount = server.Items.Count;
                dp.NodeCount = server.Count;
                dp.DID = server.DID;
                dp.Utilization = Mathf.Round((dp.Load / dp.Capacity) * 10000) / 10000;
                dp.MyState = dp.Utilization > UtilizationAlarmState ? State.Alarm : (dp.Utilization > UtilizationWarnState ? State.Warning : State.Normal);
                
                _currentDataPoints.Add(dp);
            }
        }

        private void RecordDataPoints()
        {
            SDateTime now = SDateTime.Now();
            int minutesSinceLastUpdate = now.ToInt() - _lastUpdate.ToInt();
            List<DataPoint> dataPoints = _currentDataPoints;

            if (minutesSinceLastUpdate >= _updateFrequency)
            {
                _lastUpdate = now;
                for(int i = 0; i < _numberOfHistoricalPoints - 1; i++)
                {
                    _historicalPoints[i] = _historicalPoints[i + 1];
                }
                _historicalPoints[_numberOfHistoricalPoints-1] = _currentDataPoints;
            }
        }

        #endregion

        #region UI Methods

        /// <summary>
        /// Create the base UI Elements
        /// </summary>
        private void InitializeUI()
        {
            // Make sure the necessary components exist
            if (_mainButton == null)
            {
                _mainButton = WindowManager.SpawnButton();
                _mainButton.GetComponentsInChildren<Text>()[0].text = "Monitor";
                _origColor = _mainButton.GetComponentsInChildren<Text>()[0].color;
                WindowManager.AddElementToElement(_mainButton.gameObject, HUD.Instance.gameObject, new UnityEngine.Rect(0, 20, 100, 20), new UnityEngine.Rect(0, 0, 0, 0));
                _mainButton.onClick.AddListener(OnMainButton_click);
                _lastUpdate = SDateTime.Now();
            }
            if (_wnd == null)
            {
                _wnd = WindowManager.SpawnWindow();
                _wnd.Title = "";
                _wnd.StartHidden = true;
                _wnd.OnlyHide = true;
                //_wnd.TitleText.text = "Server Monitor";
                WindowManager.AddElementToElement(_wnd.gameObject, HUD.Instance.gameObject, new Rect(0, 40, 300, 150), new Rect(0, 0, 0, 0));
            
                _serverLabel = WindowManager.SpawnLabel();
                _serverLabel.text = "Total Utilization:";
                WindowManager.AddElementToElement(_serverLabel.gameObject, _wnd.gameObject, new Rect(10, 80, 280, 500), new Rect(0, 0, 0, 0));

                float numPoints = (float)_numberOfHistoricalPoints;
                float pbWidth = 280f / (numPoints+1);
                _historicalPoints = new List<DataPoint>[_numberOfHistoricalPoints];
                for(int i=0; i< _numberOfHistoricalPoints; i++)
                {
                    _historicalPoints[i] = new List<DataPoint>();
                    GUIProgressBar pb = WindowManager.SpawnProgressbar();
                    WindowManager.AddElementToElement(pb.gameObject, _wnd.gameObject, new Rect(10 + (pbWidth*i) + i, 40, 40, pbWidth), new Rect(0, 0, 0, 0));
                    pb.transform.Rotate(0, 0, 90f);
                    pb.transform.Translate(-25f, 0f, 0f);
                    pb.Value = 0;
                    _totalUtilizations.Insert(i, pb);
                }
            }
        }

        private void UpdateUI()
        {
            int NormalCount = _currentDataPoints.Where(dp => dp.MyState == State.Normal).Count();
            int WarnCount = _currentDataPoints.Where(dp => dp.MyState == State.Warning).Count();
            int AlarmCount = _currentDataPoints.Where(dp => dp.MyState == State.Alarm).Count();

            string msg = "Servers in Normal State: {0}\nServers in Warning State: {1}\nServers in Alarm State: {2}";
            
            if (AlarmCount > 0)
            {
                if (DateTime.Now.Subtract(_sinceFlip).TotalMilliseconds >= 300)
                {
                    _flipState = !_flipState;
                    _sinceFlip = DateTime.Now;
                }
            
                if (_flipState)
                {
                    _mainButton.image.color = new Color(0.2f, 0f, 0f);
                    _mainButton.GetComponentsInChildren<Text>()[0].color = Color.red;
                }
                else
                {
                    _mainButton.image.color = new Color(1f, 0f, 0f);
                    _mainButton.GetComponentsInChildren<Text>()[0].color = _origColor;
                }
            }
            else if (WarnCount > 0)
            {
                _mainButton.image.color = Color.yellow;
                _mainButton.GetComponentsInChildren<Text>()[0].color = _origColor;
            }
            else
            {
                _mainButton.image.color = Color.green;
                _mainButton.GetComponentsInChildren<Text>()[0].color = _origColor;
            }

            _serverLabel.text = string.Format(msg, NormalCount.ToString(), WarnCount.ToString(), AlarmCount.ToString());

            for (int i = 0; i < _numberOfHistoricalPoints; i++)
            {
                float totalCapacity = _historicalPoints[i].Select(dp => dp.Capacity).Sum();
                float totalLoad = _historicalPoints[i].Select(dp => dp.Load).Sum();
                float totalUtilization = Mathf.Round((totalLoad / totalCapacity) * 10000f) / 10000f;
                _totalUtilizations[i].Value = totalUtilization;

                Color barColor = Color.green;
                if(totalUtilization >= UtilizationAlarmState)
                {
                    barColor = Color.red;
                }
                else if (totalUtilization >= UtilizationWarnState)
                {
                    barColor = Color.yellow;
                }
                _totalUtilizations[i].StartColor = barColor;

                if (i == _numberOfHistoricalPoints - 1)
                {
                    _serverLabel.text += "\nTotal Utilization: " + (totalUtilization * 100).ToString() + "%";
                }
            }
        }

        private void SetStrings()
        {
            Localization.Translation translation = Localization.Translations[Localization.CurrentTranslation];
            if (translation.UI.ContainsKey("ServerMonitor")) { translation.UI.Add("ServerMonitor", "Server Monitor"); }
        }

        #endregion

        #region Internal Events

        void OnMainButton_click()
        {
            if (_wnd.Shown)
            {
                _wnd.Close();
            }
            else
            {
                _wnd.Show();
            }
        }

        #endregion 

        #region Private Members

        private bool _modActive = false;
        private int _updateFrequency = 60;
        private int _numberOfHistoricalPoints = 24;
        private SDateTime _lastUpdate = SDateTime.Now();

        private UnityEngine.UI.Button _mainButton;
        private GUIWindow _wnd;
        private Text _serverLabel;
        private List<GUIProgressBar> _totalUtilizations = new List<GUIProgressBar>();

        private DateTime _sinceFlip;
        private bool _flipState = false;
        private Color _origColor = Color.black;
        private List<DataPoint> _currentDataPoints = new List<DataPoint>();
        private List<DataPoint>[] _historicalPoints;

        #endregion
    }
}