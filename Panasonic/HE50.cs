using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Panasonic
{
    public class HE50
    {
        /* Static public variables, you can set to change behaviour of class*/

        /// <summary>
        /// How many missed webresponses before the camera is considered disconnected.
        /// </summary>
        public static int attemptsBeforeDisconnected = 3;

        /// <summary>
        /// if true= camera send updated to script, if false it doesent, remember that only one software on the computer can recieve updates so you cant run multiple softwares om the same computer.
        /// </summary>
        public static bool cameraUpdates = true;

        /// <summary>
        /// Turn on to show debug messages from script.
        /// </summary>
        public static bool debug = false;

        /// <summary>
        /// Extremly verbose debug messages, use only to debug data from camera.
        /// </summary>
        public static bool expandedDebug = false;

        /// <summary>
        /// Triggers then autofocus is turned on/off (1=on,0=off)
        /// </summary>
        public simpleStatusDelegate onAutoFocus = delegate { };

        /// <summary>
        /// Triggers when awb is changed, status is 0=ATW, 1=AWB A, 2=AWB B, 3=ATW
        /// </summary>
        public simpleStatusDelegate onAWBMode = delegate { };

        /// <summary>
        /// Triggers then red gain is changed, status is between 0-60 where 30 is neutral.
        /// </summary>
        public simpleStatusDelegate onBlueGain = delegate { };

        /// <summary>
        /// Triggers then Blue Gain is changed (only HE120), status is between 0-300 where 150 is neutral.
        /// </summary>
        public simpleStatusDelegate onBlueGain120 = delegate { };

        /// <summary>
        /// BETA! Triggers then Blue pedestal is changed (only HE120), status is between 0-300 where 150 is neutral.
        /// </summary>
        public simpleStatusDelegate onBluePedestal = delegate { };

        /// <summary>
        /// Triggers then chroma is changes, status is between 0-6 where 3 is neutral.
        /// </summary>
        public simpleStatusDelegate onChroma = delegate { };

        /// <summary>
        /// Triggers then chroma is changes, status is between 0-6 where 3 is neutral.
        /// </summary>
        public simpleStatusDelegate onChroma130 = delegate { };

        /// <summary>
        /// Triggers then camera connects/reconnects
        /// </summary>
        public connectedDelegate onConnect = delegate { };

        /// <summary>
        /// Triggers then camera disconnects
        /// </summary>
        public connectedDelegate onDisconnect = delegate { };

        /// <summary>
        /// Triggers when focus is changes, status is the current focus position, status is between 1365 and 4095
        /// </summary>
        public simpleStatusDelegate onFocus = delegate { };

        /// <summary>
        /// Triggers then Gain is changed 8=0db, 11=3db, 14=6db, 17=9db, 20=12db, 23=15db, 26=18db, 128=auto.
        /// </summary>
        public simpleStatusDelegate onGain = delegate { };

        /// <summary>
        /// Triggers when IrisPosition is changes, status is the current IrisPosition position, status is between  0 and 1023
        /// </summary>
        public simpleStatusDelegate onIrisPosition = delegate { };

        /// <summary>
        /// Capture all logmessages from object
        /// </summary>
        public logDelegate onLog = delegate { };

        /// <summary>
        /// Triggers when NDFilter is changes, status is the currentN DFilter, status is between 0 and 3
        /// </summary>
        public simpleStatusDelegate onNDFilter = delegate { };

        /// <summary>
        /// Triggers then pedestal is changes, status is between 0-60 where 30 is neutral.
        /// </summary>
        public simpleStatusDelegate onPedestal = delegate { };

        /// <summary>
        /// Triggers then power is changed, status is 1=power is on, 0=standby mode
        /// </summary>
        public simpleStatusDelegate onPower = delegate { };

        /// <summary>
        /// Triggers when an preset is complete, status is preset number.
        /// </summary>
        public simpleStatusDelegate onPresetComplete = delegate { };

        /// <summary>
        /// Triggers when an preset is complete, status is preset number.
        /// </summary>
        public simpleStatusDelegate onPresetMode = delegate { };

        /// <summary>
        /// Triggers then red gain is changed, status is between 0-60 where 30 is neutral.
        /// </summary>
        public simpleStatusDelegate onRedGain = delegate { };

        /// <summary>
        /// BETA! Triggers then Red pedestal is changed (only HE120), status is between 0-300 where 150 is neutral.
        /// </summary>
        public simpleStatusDelegate onRedGain120 = delegate { };

        /// <summary>
        /// Triggers then Red pedestal is changed (only HE120), status is between 0-300 where 150 is neutral.
        /// </summary>
        public simpleStatusDelegate onRedPedestal = delegate { };

        /// <summary>
        /// Triggers when NDFilter is changes, status is the currentN DFilter, status is between 0 and 3
        /// </summary>
        public simpleStatusDelegate onScene = delegate { };

        /// <summary>
        /// Triggers then shutter is changed and step is choosen as type in web interface. status is 0=shutter off,3= 1/100 or 1/120, 5=1/250, 6=1/500, 7=1000, 8=1/2000, 9=1/4000, 10=1/10000, 11=SyncroScan
        /// </summary>
        public simpleStatusDelegate onShutterStep = delegate { };

        /// <summary>
        /// Triggers then shutter is changed and syncro is choosen as type in web interface, status is between 1-255.
        /// </summary>
        public simpleStatusDelegate onShutterSyncro = delegate { };

        /// <summary>
        /// Triggers then tally is changed, status is 1=tally on, 0=tally off
        /// </summary>
        public simpleStatusDelegate onTally = delegate { };

        /// <summary>
        /// Triggers when zoom is changes, status is the current zoom position, status is between 1365 and 4095
        /// </summary>
        public simpleStatusDelegate onZoom = delegate { };

        /// <summary>
        /// timeout i millisekunder som simpleWebClient låter anropen vänta.
        /// </summary>
        public int webTimeout = 1000;

        /// <summary>
        /// Sets up the connection for the camera.
        /// host is the ip-number or hostname for your camera.
        /// use username and password if you have changed the default Credentials of your camera.
        /// Set encrypted to true if you want to use encryption.
        /// </summary>
        public HE50(string host, string username = "", string password = "", bool encrypted = false)
        {
            this.host = host;
            this.username = username;
            this.password = password;

            if (encrypted)
            {
                protocol = "https";
            }
            else
            {
                protocol = "http";
            }

            if (username.Length > 0 && password.Length > 0)
            {
                useCredentials = true;
            }

            /* create an new thread for the tcpServer .*/
            commandThread = new Thread(new ThreadStart(EventCommandServer));
            commandThread.Name = "commandThread";
            commandThread.Start();

            /* Kontrollerar vilken kameratyp som används, görs alltid vid uppstart. (startListen gör en egen kontroll) */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QID&res=1", onModelNameHandler);

            if (cameraUpdates)
            {
                startListen();
            }
        }

        /// <summary>
        /// Delegate used then camera disconencts/reconnects.
        /// </summary>
        public delegate void connectedDelegate();

        /// <summary>
        /// Delegate used for debug messages,
        /// </summary>
        /// <param name="function">name of function calling delegate</param>
        /// <param name="message">debug message/param>
        public delegate void logDelegate(string function, string message);

        /// <summary>
        /// Delegate used for all simple events an camera triggers
        /// </summary>
        public delegate void simpleStatusDelegate(int status);

        /// <summary>
        /// Delegate used for all simple webClients events, aka when download is complete.
        /// </summary>
        public delegate void simpleWebClientDelegate(string body);

        /// <summary>
        /// Sets autofocus on/off (1=on,0=off)
        /// </summary>
        public int AutoFocus
        {
            set
            {
                if (AutoFocusValue != value && (value >= 0 && value <= 1))
                {
                    AutoFocusValue = value;
                    AutoFocusCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23D1" + AutoFocusValue.ToString("D1") + "&res=1";

                    if (!AutoFocusCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(AutoFocusCmd);
                        AutoFocusCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return AutoFocusValue;
            }
        }

        /// <summary>
        /// Start AWB Execute 1=execute
        /// </summary>
        public int AWBExecute
        {
            set
            {
                if (value == 1)
                {
                    AWBExecuteValue = value;

                    AWBExecuteCmd.cmd = "/cgi-bin/aw_cam?cmd=OWS&res=0";

                    if (!AWBExecuteCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(AWBExecuteCmd);
                        AWBExecuteCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return AWBExecuteValue;
            }
        }

        /// <summary>
        /// Select AWB Mode, acceptable values are 0=ATW, 1=AWB A, 2=AWB B, 3=ATW
        /// </summary>
        public int AWBMode
        {
            set
            {
                if (AWBModeValue != value && (value >= 0 || value <= 3))
                {
                    AWBModeValue = value;

                    AWBModeCmd.cmd = "/cgi-bin/aw_cam?cmd=OAW:" + AWBModeValue.ToString("D1") + "&res=1";

                    if (!AWBModeCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(AWBModeCmd);
                        AWBModeCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return AWBModeValue;
            }
        }

        /// <summary>
        /// sets blue gain, Acceptable values are between 0-60 where 30 is neutral.
        /// </summary>
        public int BlueGain
        {
            set
            {
                if (BlueGainValue != value && (value >= 0 && value <= 60))
                {
                    BlueGainValue = value;
                    BlueGainCmd.cmd = "/cgi-bin/aw_cam?cmd=OBG:" + BlueGainValue.ToString("X2") + "&res=1";

                    if (!BlueGainCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(BlueGainCmd);
                        BlueGainCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return BlueGainValue;
            }
        }

        /// <summary>
        /// sets blue gain on HE-120, Acceptable values are between 0-300 where 150 is neutral.
        /// </summary>
        public int BlueGain120
        {
            set
            {
                if (BlueGain120Value != value && (value >= 0 && value <= 300))
                {
                    BlueGain120Value = value;
                    BlueGain120Cmd.cmd = "/cgi-bin/aw_cam?cmd=OBI:" + BlueGain120Value.ToString("X3") + "&res=1";

                    if (!BlueGain120Cmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(BlueGain120Cmd);
                        BlueGain120Cmd.inQueue = true;
                    }
                }
            }
            get
            {
                return BlueGain120Value;
            }
        }

        /// <summary>
        /// Beta! Sets Blue Pedestal, acceptable values are between 0 and 300, 150 is neutral. Only works with HE-120
        /// </summary>
        public int BluePedestal
        {
            set
            {
                if (BluePedestalValue != value && (value >= 0 && value <= 300) && (cameraType == 120 || cameraType == 130))
                {
                    BluePedestalValue = value;
                    BluePedestalCmd.cmd = "/cgi-bin/aw_cam?cmd=OBP:" + BluePedestalValue.ToString("X3") + "&res=1";

                    if (!BluePedestalCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(BluePedestalCmd);
                        BluePedestalCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return BluePedestalValue;
            }
        }

        /// <summary>
        /// Sets Chroma, acceptable values are between 0 and 6, dont work for HE-130
        /// </summary>
        public int Chroma
        {
            set
            {
                if (ChromaValue != value && (value >= 0 && value <= 6) && cameraType != 130)
                {
                    ChromaValue = value;

                    ChromaCmd.cmd = "/cgi-bin/aw_cam?cmd=OCG:" + ChromaValue.ToString("D2") + "&res=1";

                    if (!ChromaCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(ChromaCmd);
                        ChromaCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return ChromaValue;
            }
        }

        /// <summary>
        /// Sets chroma on camera HE-130, value can be zero (off) or between 29 (-99%) and 168 (+40%).
        /// </summary>
        public int Chroma130
        {
            set
            {
                if (Chroma130Value != value && (value == 0 || (value >= 29 && value <= 168)) && cameraType == 130)
                {
                    Chroma130Value = value;

                    Chroma130Cmd.cmd = "/cgi-bin/aw_cam?cmd=OSD:B0:" + Chroma130Value.ToString("X2") + "&res=1";

                    if (!Chroma130Cmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(Chroma130Cmd);
                        Chroma130Cmd.inQueue = true;
                    }
                }
            }
            get
            {
                return Chroma130Value;
            }

        }



        /// <summary>
        /// is true if we belive that the camera is reachable, is set to false if we have detected that it is disconnected.
        /// </summary>
        public bool connected { get; private set; }

        /* Public values accessiable to control camera */
        /* Constructor / Destructor */

        /// <summary>
        /// sets focus (speed) is changes, Acceptable values are between 01 and 99
        /// </summary>
        public int Focus
        {
            set
            {
                if (FocusValue != value && (value >= 01 && value <= 99))
                {
                    FocusValue = value;
                    FocusCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23F" + FocusValue.ToString("D2") + "&res=1";

                    if (!FocusCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(FocusCmd);
                        FocusCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return FocusValue;
            }
        }

        /// <summary>
        /// sets Focus (Position), Acceptable values are between 1365 (near) and 4095 (far)
        /// </summary>
        public int FocusPosition
        {
            set
            {
                if (FocusPositionValue != value && (value >= 1365 && value <= 4095))
                {
                    FocusPositionValue = value;
                    FocusPositionCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23AXF" + FocusPositionValue.ToString("X3") + "&res=1";

                    if (!FocusPositionCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(FocusPositionCmd);
                        FocusPositionCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return FocusPositionValue;
            }
        }

        /// <summary>
        /// Set Gain level on camera Acceptable values are 8=0db, 11=3db, 14=6db, 17=9db, 20=12db, 23=15db, 26=18db, 128=auto.
        /// </summary>
        public int Gain
        {
            set
            {
                if (GainValue != value && (value == 8 || value == 11 || value == 14 || value == 17 || value == 20 || value == 23 || value == 26 || value == 128))
                {
                    GainValue = value;

                    GainCmd.cmd = "/cgi-bin/aw_cam?cmd=OGU:" + GainValue.ToString("X2") + "&res=1";

                    if (!GainCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(GainCmd);
                        GainCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return GainValue;
            }
        }

        /// <summary>
        /// Set Gain level on Camera HE-120, increments of 1db, goes from 8-26 where 8=0db and 26=18db;
        /// </summary>
        public int Gain120
        {
            set
            {
                if (Gain120Value != value && (value >= 8 && value <= 26))
                {
                    Gain120Value = value;

                    GainCmd.cmd = "/cgi-bin/aw_cam?cmd=OGU:" + Gain120Value.ToString("X2") + "&res=1";

                    if (!GainCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(GainCmd);
                        GainCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return Gain120Value;
            }
        }

        /// <summary>
        /// Sets Iris, acceptable values are between 1 and 99 Where 50 is neutral (no movement).
        /// </summary>
        public int Iris
        {
            set
            {
                if (IrisValue != value && (value >= 1 && value <= 99))
                {
                    IrisValue = value;
                    IrisCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23I" + IrisValue.ToString("D2") + "&res=1";

                    if (!IrisCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(IrisCmd);
                        IrisCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return IrisValue;
            }
        }

        /// <summary>
        /// Sets IrisPosition, acceptable values are between 1365 and 4095
        /// </summary>
        public int IrisPosition
        {
            set
            {
                if (IrisPositionValue != value && (value >= 1365 && value <= 4095))
                {
                    IrisPositionValue = value;
                    IrisPositionCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23AXI" + IrisPositionValue.ToString("X3") + "&res=1";

                    if (!IrisPositionCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(IrisPositionCmd);
                        IrisPositionCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return IrisPositionValue;
            }
        }

        /// <summary>
        /// Set NDFilter, Acceptable values are between 0=through,1=1/4,2=1/16,3=1/64 , Only Works for HE-120
        /// </summary>
        public int NDFilter
        {
            set
            {
                if (NDFilterValue != value && (value >= 0 && value <= 3) && (cameraType == 120 || cameraType == 130))
                {
                    NDFilterValue = value;
                    NDFilterCmd.cmd = "/cgi-bin/aw_cam?cmd=OFT:" + NDFilterValue.ToString("D1") + "&res=1";

                    if (!NDFilterCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(NDFilterCmd);
                        NDFilterCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return NDFilterValue;
            }
        }

        /// <summary>
        /// Set Pan (speed), Acceptable values are between 1-99. Where 50 is Neutral (aka stop)
        /// </summary>
        public int Pan
        {
            set
            {
                if (PanValue != value && (value >= 1 && value <= 99))
                {
                    PanValue = value;
                    MoveCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23PTS" + PanValue.ToString("D2") + TiltValue.ToString("D2") + "&res=1";

                    if (!MoveCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(MoveCmd);
                        MoveCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PanValue;
            }
        }

        /// <summary>
        /// Sets Pedestal, acceptable values are between 0 and 60, 30 is neutral.
        /// </summary>
        public int Pedestal
        {
            set
            {
                if (PedestalValue != value && (value >= 0 && value <= 60))
                {
                    PedestalValue = value;
                    PedestalCmd.cmd = "/cgi-bin/aw_cam?cmd=OTD:" + PedestalValue.ToString("X2") + "&res=1";

                    if (!PedestalCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(PedestalCmd);
                        PedestalCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PedestalValue;
            }
        }

        /// <summary>
        /// Sets Power on/off (1=on,0=off)
        /// </summary>
        public int Power
        {
            set
            {
                if (PowerValue != value && (value >= 0 && value <= 1))
                {
                    PowerValue = value;
                    PowerCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23O" + PowerValue.ToString("D1") + "&res=1";

                    if (!PowerCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(PowerCmd);
                        PowerCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PowerValue;
            }
        }

        /// <summary>
        /// Executes Preset, Acceptable values are between 0-99.
        /// </summary>
        public int Preset
        {
            set
            {
                if (value >= 0 && value <= 99)
                {
                    PresetValue = value;
                    PresetCmdCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23R" + PresetValue.ToString("D2") + "&res=1";

                    if (!PresetCmdCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(PresetCmdCmd);
                        PresetCmdCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PresetValue;
            }
        }

        /// <summary>
        /// Sets PresetMode, 0=Mode A,1=Mode B, 2=Mode C
        /// </summary>
        public int PresetMode
        {
            set
            {
                if (value >= 0 && value <= 2)
                {
                    PresetModeValue = value;
                    PresetModeCmd.cmd = "/cgi-bin/aw_cam?cmd=OSE:71:" + PresetModeValue.ToString("D1") + "&res=1";

                    if (!PresetModeCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(PresetModeCmd);
                        PresetModeCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PresetModeValue;
            }
        }

        /// <summary>
        /// saves current position in Preset, Acceptable values are between 0-99.
        /// </summary>
        public int PresetSave
        {
            set
            {
                if (value >= 0 && value <= 99)
                {
                    PresetSaveValue = value;
                    PresetSaveCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23M" + PresetSaveValue.ToString("D2") + "&res=1";

                    if (!PresetSaveCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(PresetSaveCmd);
                        PresetSaveCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return PresetSaveValue;
            }
        }

        /// <summary>
        /// sets red gain, Acceptable values are between 0-60 where 30 is neutral.
        /// </summary>
        public int RedGain
        {
            set
            {
                if (RedGainValue != value && (value >= 0 && value <= 60))
                {
                    RedGainValue = value;
                    RedGainCmd.cmd = "/cgi-bin/aw_cam?cmd=ORG:" + RedGainValue.ToString("X2") + "&res=1";

                    if (!RedGainCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(RedGainCmd);
                        RedGainCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return RedGainValue;
            }
        }

        /// <summary>
        /// sets blue gain on HE-120, Acceptable values are between 0-300 where 150 is neutral.
        /// </summary>
        public int RedGain120
        {
            set
            {
                if (RedGain120Value != value && (value >= 0 && value <= 300))
                {
                    RedGain120Value = value;
                    RedGain120Cmd.cmd = "/cgi-bin/aw_cam?cmd=ORI:" + RedGain120Value.ToString("X3") + "&res=1";

                    if (!RedGain120Cmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(RedGain120Cmd);
                        RedGain120Cmd.inQueue = true;
                    }
                }
            }
            get
            {
                return RedGain120Value;
            }
        }

        /// <summary>
        /// Beta! Sets Red Pedestal, acceptable values are between 0 and 300, 150 is neutral. Only works with HE-120
        /// </summary>
        public int RedPedestal
        {
            set
            {
                if (RedPedestalValue != value && (value >= 0 && value <= 300) && (cameraType == 120 || cameraType == 130))
                {
                    RedPedestalValue = value;
                    RedPedestalCmd.cmd = "/cgi-bin/aw_cam?cmd=ORP:" + RedPedestalValue.ToString("X3") + "&res=1";

                    if (!RedPedestalCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(RedPedestalCmd);
                        RedPedestalCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return RedPedestalValue;
            }
        }

        /// <summary>
        /// Set Scene, Acceptable values are between 0=3 for HE50/HE60 and 1-4 for HE-120
        /// </summary>
        public int Scene
        {
            set
            {
                if (SceneValue != value &&
                       (
                                ((cameraType == 120 || cameraType == 130) && value >= 1 && value <= 4)
                            ||
                                ((cameraType == 50 || cameraType == 60) && value >= 0 && value <= 3)
                       )
                   )
                {
                    SceneValue = value;
                    SceneCmd.cmd = "/cgi-bin/aw_cam?cmd=XSF:" + NDFilterValue.ToString("D1") + "&res=1";

                    if (!SceneCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(SceneCmd);
                        SceneCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return SceneValue;
            }
        }

        /// <summary>
        /// Sets shutterStep acceptable values are 0=shutter off,
        /// 4= 1/100 or 1/120,
        /// 5=1/250,
        /// 6=1/500,
        /// 7=1000,
        /// 8=1/2000,
        /// 9=1/4000,
        /// 10=1/10000,
        /// 11=SyncroScan
        /// 12=ELC
        /// </summary>
        public int ShutterStep
        {
            set
            {
                if (ShutterStepValue != value && (value >= 4 && value <= 12))
                {
                    /* panasonic har hoppat över värde fyra, tre är det lägsta (1/120 eller 1/100) */
                    if (value == 4)
                    {
                        value = 3;
                    }

                    ShutterStepValue = value;
                    ShutterStepCmd.cmd = "/cgi-bin/aw_cam?cmd=OSH:" + ShutterStepValue.ToString("X1") + "&res=1";

                    if (!ShutterStepCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(ShutterStepCmd);
                        ShutterStepCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return ShutterStepValue;
            }
        }

        /// <summary>
        /// Sets ShutterSyncro, acceptable values between 1-255.
        /// </summary>
        public int ShutterSyncro
        {
            set
            {
                if (ShutterSyncroValue != value && (value >= 1 && value <= 255))
                {
                    ShutterSyncroValue = value;
                    ShutterSyncroCmd.cmd = "/cgi-bin/aw_cam?cmd=OMS:" + ShutterSyncroValue.ToString("X3") + "&res=1";

                    if (!ShutterSyncroCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(ShutterSyncroCmd);
                        ShutterSyncroCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return ShutterSyncroValue;
            }
        }

        /// <summary>
        /// Sets Tally on/off (1=on,0=off)
        /// </summary>
        public int Tally
        {
            set
            {
                if (TallyValue != value && (value >= 0 && value <= 1))
                {
                    TallyValue = value;
                    TallyCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23DA" + TallyValue.ToString("D1") + "&res=1";

                    if (!TallyCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(TallyCmd);
                        TallyCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return TallyValue;
            }
        }

        /// <summary>
        /// Set Tilt (speed), Acceptable values are between 1-99. Where 50 is Neutral (aka stop)
        /// </summary>
        public int Tilt
        {
            set
            {
                if (TiltValue != value && (value >= 1 && value <= 99))
                {
                    TiltValue = value;
                    MoveCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23PTS" + PanValue.ToString("D2") + TiltValue.ToString("D2") + "&res=1";

                    if (!MoveCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(MoveCmd);
                        MoveCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return TiltValue;
            }
        }

        /// <summary>
        /// Sets Zoom speed, 01-99, 50 is neutral.
        /// </summary>
        public int Zoom
        {
            set
            {
                if (ZoomValue != value && (value >= 1 && value <= 99))
                {
                    ZoomValue = value;
                    ZoomCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23Z" + ZoomValue.ToString("D2") + "&res=1";

                    if (!ZoomCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(ZoomCmd);
                        ZoomCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return ZoomValue;
            }
        }

        /// <summary>
        /// Sets ZoomPosition, values betwwen 1365 and 4095 is acceptable.
        /// </summary>
        public int ZoomPosition
        {
            set
            {
                if (ZoomPositionValue != value && (value >= 1365 && value <= 4095))
                {
                    ZoomPositionValue = value;
                    ZoomPositionCmd.cmd = "/cgi-bin/aw_ptz?cmd=%23AXZ" + ZoomPositionValue.ToString("X3") + "&res=1";

                    if (!ZoomPositionCmd.inQueue)
                    {
                        eventQueueSettings.Enqueue(ZoomPositionCmd);
                        ZoomPositionCmd.inQueue = true;
                    }
                }
            }
            get
            {
                return ZoomPositionValue;
            }
        }

        /// <summary>
        /// Increments Gain with one step. (Works for both HE50/HE120)
        /// </summary>
        /// <param name="up"></param>
        public void GainStepDown()
        {
            /* Hämta kamerans nuvarande värde */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QGU&res=1", GainStepDownHandler);
        }

        /// <summary>
        /// Increments Gain with one step. (Works for both HE50/HE120)
        /// </summary>
        /// <param name="up"></param>
        public void GainStepUp()
        {
            /* Hämta kamerans nuvarande värde */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QGU&res=1", GainStepUpHandler);
        }

        /// <summary>
        /// Försöker ta reda på vilken typ av kamera vi använder, sätter i slutändan cameraType
        /// </summary>
        public void onModelNameHandler(string unknownData)
        {
            int tmpModel = onModelNamePattern(unknownData);
            if (tmpModel >= 0)
            {
                log("onModelNameHandler", "onModelName(" + tmpModel + ")");
                onModelName(tmpModel);
            }
            else
            {
                log("onModelNameHandler", "Unknown error, cant read: " + unknownData);
            }
        }

        /// <summary>
        /// an simple function to execute webcommands on the camera.
        /// </summary>
        public void simpleWebClient(string url, simpleWebClientDelegate simpleWebClientCallback = null)
        {
            Thread WebClientThread = new Thread(() => simpleWebClientThread(url, simpleWebClientCallback));
            WebClientThread.Name = "WebClientThread";
            WebClientThread.Start();
        }

        /// <summary>
        /// Destroys all threads
        /// This should only be done when cleaning up and for stopping listening server.
        /// </summary>
        public void stop()
        {
            /* Turn of thread looping*/
            commandThreadActive = false;
            ListenThreadActive = false;

            /* Terminate commandThread */
            commandThread.Join();

            /* stop listening server */
            if (cameraUpdates)
            {
                stopListen();
            }
        }

        /// <summary>
        /// Get an update of the cameras current values.
        /// </summary>
        public void update()
        {
            /* Batchrequest, dont include all info however, more info below.*/
            simpleWebClient("/live/camdata.html", evaluateCamData);
            Thread.Sleep(50);

            simpleWebClient("/cgi-bin/aw_cam?cmd=QSD:B0&res=1", evaluateCamData);
            Thread.Sleep(200);
        }

        private CommandHolder AutoFocusCmd = new CommandHolder();

        private int AutoFocusValue = 0;

        private CommandHolder AWBExecuteCmd = new CommandHolder();

        private int AWBExecuteValue = 0;

        private CommandHolder AWBModeCmd = new CommandHolder();

        private int AWBModeValue = 0;

        private CommandHolder BlueGain120Cmd = new CommandHolder();

        private int BlueGain120Value = 150;

        private CommandHolder BlueGainCmd = new CommandHolder();

        private int BlueGainValue = 30;

        private CommandHolder BluePedestalCmd = new CommandHolder();

        private int BluePedestalValue = 0;

        /// <summary>
        /// Stores camera type in variable, values are 50,60,120
        /// </summary>
        private int cameraType;

        private CommandHolder ChromaCmd = new CommandHolder();
        private int ChromaValue = 1;

        private CommandHolder Chroma130Cmd = new CommandHolder();
        private int Chroma130Value = 1;

        /// <summary>
        /// This thread run <see cref="EventCommandServer"/>
        /// </summary>
        private Thread commandThread;

        /// <summary>
        /// Set to false to turn of all thread loops.
        /// </summary>
        private bool commandThreadActive = true;

        private int connectionAtemptsValue = 1;

        /// <summary>
        /// Counter for all commands done to the server
        /// </summary>
        private int eventQueueCounter = 0;

        /// <summary>
        /// This Queue includes all commands for Tilt/Pan and Zoom
        /// </summary>
        private Queue<CommandHolder> eventQueuePositioning = new Queue<CommandHolder>();

        /// <summary>
        /// Queaus all command settings except Tilt,Pan and Zoom
        /// </summary>
        private Queue<CommandHolder> eventQueueSettings = new Queue<CommandHolder>();

        private CommandHolder FocusCmd = new CommandHolder();

        private CommandHolder FocusPositionCmd = new CommandHolder();

        private int FocusPositionValue = 2730;

        private int FocusValue = 1365;

        private int Gain120Value = 0;

        private CommandHolder GainCmd = new CommandHolder();

        private int GainValue = 0;

        /// <summary>
        /// ip-number or hostname of camera
        /// </summary>
        private string host;

        private CommandHolder IrisCmd = new CommandHolder();

        private CommandHolder IrisPositionCmd = new CommandHolder();

        private int IrisPositionValue = 1;

        private int IrisValue = 1;

        /// <summary>
        /// This thread contains the function <see cref="tcpServer"/> which runs <see cref="tcpListener"/>
        /// </summary>
        private Thread listenThread;

        /// <summary>
        /// Set to false to turn of all thread loops.
        /// </summary>
        private bool ListenThreadActive = true;

        private CommandHolder MoveCmd = new CommandHolder();

        private CommandHolder NDFilterCmd = new CommandHolder();

        private int NDFilterValue = 0;

        private int PanValue = 50;

        /// <summary>
        /// Password to access camera, default password for camera is 12345
        /// </summary>
        private string password;

        private CommandHolder PedestalCmd = new CommandHolder();

        private int PedestalValue = 0;

        private CommandHolder PowerCmd = new CommandHolder();

        private int PowerValue = 0;

        private CommandHolder PresetCmdCmd = new CommandHolder();

        private CommandHolder PresetModeCmd = new CommandHolder();

        private int PresetModeValue = 0;

        private CommandHolder PresetSaveCmd = new CommandHolder();

        private int PresetSaveValue = 0;

        private int PresetValue = 0;

        /// <summary>
        /// containts http or https depending on what protocol the user wants to use to access camera.
        /// </summary>
        private string protocol;

        private CommandHolder RedGain120Cmd = new CommandHolder();

        private int RedGain120Value = 150;

        private CommandHolder RedGainCmd = new CommandHolder();

        private int RedGainValue = 30;

        private CommandHolder RedPedestalCmd = new CommandHolder();

        private int RedPedestalValue = 0;

        private CommandHolder SceneCmd = new CommandHolder();

        private int SceneValue = 0;

        private CommandHolder ShutterStepCmd = new CommandHolder();

        private int ShutterStepValue = 1;

        private CommandHolder ShutterSyncroCmd = new CommandHolder();

        private int ShutterSyncroValue = 1;

        private CommandHolder TallyCmd = new CommandHolder();

        /// <summary>
        /// Private holder for <see cref="Tally"/>
        /// </summary>
        private int TallyValue = 0;

        /// <summary>
        /// <see cref="tcpListener"/> Listens to tcp port <see cref="tcpServerPort"/>
        /// </summary>
        private TcpListener tcpListener;

        /// <summary>
        /// Containts what tcp port-number <see cref="tcpListener"/> is using.
        /// </summary>
        private int tcpServerPort;

        /// <summary>
        /// Private holder for <paramref name="Tilt"/>
        /// </summary>
        private int TiltValue = 50;

        /// <summary>
        /// should credentials (<see cref="username"/>/<see cref="password"/>) be used to send commands? not needed if username and password is default.
        /// </summary>
        private bool useCredentials = false;

        /// <summary>
        /// username to access camera, default username for camera is admin
        /// </summary>
        private string username;

        /// <summary>
        /// Räknar som håller reda på hur många simpleWebClientTreads som har startats.
        /// </summary>
        private int WebClientCounter = 0;

        /// <summary>
        /// CommandHolder for <see cref="ZoomPositio"/>
        /// </summary>
        private CommandHolder ZoomCmd = new CommandHolder();

        /// <summary>
        /// CommandHolder for  <see cref="Zoom"/>
        /// </summary>
        private CommandHolder ZoomPositionCmd = new CommandHolder();

        /// <summary>
        /// Private holder for <see cref="ZoomPosition"/>
        /// </summary>
        private int ZoomPositionValue = 0;

        /// <summary>
        /// Private holder for <see cref="Zoom"/>
        /// </summary>
        private int ZoomValue = 0;

        /// <summary>
        /// The amount of connection
        /// </summary>
        private int connectionAtempts
        {
            set
            {
                if (value == 0)
                {
                    /* Om värdet inte var noll innan du uppdaterade connectionAtempts*/
                    if (connectionAtemptsValue != 0)
                    {
                        /* Meddela användaren.*/
                        connected = true;
                        onConnect();

                        log("connectionAtempts", "onConnect()");

                        /* Om kameran har varit ifrånkopplad (3) och kamera uppdateringar är på, starta lyssningsserven igen.*/
                        if (connectionAtemptsValue >= attemptsBeforeDisconnected && cameraUpdates)
                        {
                            /* Anslut till camera */
                            startListen();
                        }
                    }

                    connectionAtemptsValue = value;
                }
                else
                {
                    connectionAtemptsValue = value;
                    log("connectionAtempts", " value=" + value);

                    /* Vid tre misslyckade försök kör onDisconnect */
                    if (value == attemptsBeforeDisconnected)
                    {
                        // Meddela användaren.
                        connected = false;
                        onDisconnect();
                        log("connectionAtempts", "onDisconnect()");

                        // Koppla ifrån kamera. (fast de borde inte gå :))
                        if (cameraUpdates)
                        {
                            stopListen();
                        }
                    }
                }
            }
            get
            {
                return connectionAtemptsValue;
            }
        }

        /// <summary>
        /// Converts an byte to an decimal value. returns integer.
        /// </summary>
        private static int BinToDec(Byte value)
        {
            return Convert.ToInt32(value.ToString("D3"));
        }

        /// <summary>
        /// Evaluates data from camdata.html
        /// </summary>
        private void evaluateCamData(string body)
        {
            try
            {
                string[] lines = body.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    evaluateData(lines[i]);
                }
            }
            catch (Exception error)
            {
                log("evaluateCamData", error.Message);
            }
        }

        /// <summary>
        /// Matches input string against first recognisable pattern, exits after first match.
        /// </summary>
        private void evaluateData(string unknownData)
        {
            /* Testing pattern is by guessing of what is must common  */
            int tally = onTallyPattern(unknownData);
            if (tally >= 0)
            {
                if (!TallyCmd.inQueue)
                {
                    log("evaluateData", "onTally(" + tally + ")");
                    TallyValue = tally;
                    onTally(tally);
                }

                return;
            }

            /* Lensinformation is most common as its trigged on both zoom,focus and IrisPosition. */
            int[] lensInformation = onLensInformationPattern(unknownData);
            if (lensInformation[0] >= 0)
            {
                if (!ZoomCmd.inQueue && !ZoomPositionCmd.inQueue)
                {
                    log("evaluateData", "onZoom(" + lensInformation[0] + ")");
                    ZoomPositionValue = lensInformation[0];
                    onZoom(lensInformation[0]);
                }

                if (!FocusCmd.inQueue && !FocusPositionCmd.inQueue)
                {
                    log("evaluateData", "onFocus(" + lensInformation[1] + ")");
                    FocusPositionValue = lensInformation[1];
                    onFocus(lensInformation[1]);
                }

                if (!IrisCmd.inQueue && !IrisPositionCmd.inQueue)
                {
                    log("evaluateData", "onIrisPosition(" + lensInformation[2] + ")");
                    IrisPositionValue = lensInformation[2];
                    onIrisPosition(lensInformation[2]);
                }

                return;
            }

            /* Autofocus */
            int tmpAutoFocus = onAutoFocusPattern(unknownData);
            if (tmpAutoFocus >= 0)
            {
                if (!AutoFocusCmd.inQueue)
                {
                    log("evaluateData", "onAutoFocus(" + tmpAutoFocus + ")");
                    AutoFocusValue = tmpAutoFocus;
                    onAutoFocus(tmpAutoFocus);
                }
                return;
            }

            int tmpPedestal = onPedestalPattern(unknownData);
            if (tmpPedestal >= 0)
            {
                if (!PedestalCmd.inQueue)
                {
                    log("evaluateData", "onPedestal(" + tmpPedestal + ")");
                    PedestalValue = tmpPedestal;
                    onPedestal(tmpPedestal);
                }
                return;
            }

            int tmpRedPedestal = onRedPedestalPattern(unknownData);
            if (tmpRedPedestal >= 0)
            {
                if (!RedPedestalCmd.inQueue)
                {
                    log("evaluateData", "onRedPedestal(" + tmpRedPedestal + ")");
                    RedPedestalValue = tmpRedPedestal;
                    onRedPedestal(tmpRedPedestal);
                }
                return;
            }

            int tmpBluePedestal = onBluePedestalPattern(unknownData);
            if (tmpBluePedestal >= 0)
            {
                if (!BluePedestalCmd.inQueue)
                {
                    log("evaluateData", "onBluePedestal(" + tmpBluePedestal + ")");
                    BluePedestalValue = tmpBluePedestal;
                    onBluePedestal(tmpBluePedestal);
                }
                return;
            }

            int tmpGain = onGainPattern(unknownData);
            if (tmpGain >= 0)
            {
                if (!GainCmd.inQueue)
                {
                    log("evaluateData", "onGain(" + tmpGain + ")");
                    GainValue = tmpGain;
                    Gain120Value = tmpGain;
                    onGain(tmpGain);
                }
                return;
            }

            int tmpRedGain120 = onRedGain120Pattern(unknownData);
            if (tmpRedGain120 >= 0)
            {
                if (!RedGain120Cmd.inQueue)
                {
                    log("evaluateData", "onRedGain120(" + tmpRedGain120 + ")");
                    RedGain120Value = tmpRedGain120;
                    onRedGain120(tmpRedGain120);
                }
                return;
            }

            int tmpRedGain = onRedGainPattern(unknownData);
            if (tmpRedGain >= 0)
            {
                if (!RedGainCmd.inQueue)
                {
                    log("evaluateData", "onRedGain(" + tmpRedGain + ")");
                    RedGainValue = tmpRedGain;
                    onRedGain(tmpRedGain);
                }
                return;
            }

            int tmpBlueGain = onBlueGainPattern(unknownData);
            if (tmpBlueGain >= 0)
            {
                if (!BlueGainCmd.inQueue)
                {
                    log("evaluateData", "onBlueGain(" + tmpBlueGain + ")");
                    BlueGainValue = tmpBlueGain;
                    onBlueGain(tmpBlueGain);
                }

                return;
            }

            int tmpBlueGain120 = onBlueGain120Pattern(unknownData);
            if (tmpBlueGain120 >= 0)
            {
                if (!BlueGain120Cmd.inQueue)
                {
                    log("evaluateData", "onBlueGain120(" + tmpBlueGain120 + ")");
                    BlueGain120Value = tmpBlueGain120;
                    onBlueGain120(tmpBlueGain120);
                }

                return;
            }

            int chroma = onChromaPattern(unknownData);
            if (chroma >= 0)
            {
                if (!ChromaCmd.inQueue)
                {
                    log("evaluateData", "onChroma(" + chroma + ")");
                    ChromaValue = chroma;
                    onChroma(chroma);
                }
                return;
            }

            int chroma130 = onChroma130Pattern(unknownData);
            if (chroma130 >= 0)
            {
                if (!Chroma130Cmd.inQueue)
                {
                    log("evaluateData", "onChroma130(" + chroma + ")");
                    Chroma130Value = chroma;
                    onChroma130(chroma130);
                }
                return;
            }


            int tmpShutterSyncro = onShutterSyncroPattern(unknownData);
            if (tmpShutterSyncro >= 0)
            {
                if (!ShutterSyncroCmd.inQueue)
                {
                    log("evaluateData", "onShutterSyncro(" + tmpShutterSyncro + ")");
                    ShutterSyncroValue = tmpShutterSyncro;
                    onShutterSyncro(tmpShutterSyncro);
                }
                return;
            }

            int tmpShutterStep = onShutterStepPattern(unknownData);
            if (tmpShutterStep >= 0)
            {
                if (!ShutterStepCmd.inQueue)
                {
                    log("evaluateData", "onShutterStep(" + tmpShutterStep + ")");
                    ShutterStepValue = tmpShutterStep;
                    onShutterStep(tmpShutterStep);
                }
                return;
            }

            int tmpAwbChange = onAWBModePattern(unknownData);
            if (tmpAwbChange >= 0)
            {
                if (!AWBModeCmd.inQueue)
                {
                    log("evaluateData", "onAWBChange(" + tmpAwbChange + ")");
                    AWBModeValue = tmpAwbChange;
                    onAWBMode(tmpAwbChange);
                }
                return;
            }

            int tmpPresetComplete = onPresetCompletePattern(unknownData);
            if (tmpPresetComplete >= 0)
            {
                if (!PresetCmdCmd.inQueue)
                {
                    log("evaluateData", "onPresetComplete(" + tmpPresetComplete + ")");
                    PresetValue = tmpPresetComplete;
                    onPresetComplete(tmpPresetComplete);
                }

                return;
            }

            int tmpPresetMode = onPresetModePattern(unknownData);
            if (tmpPresetMode >= 0)
            {
                if (!PresetModeCmd.inQueue)
                {
                    log("evaluateData", "onPresetMode(" + tmpPresetMode + ")");
                    PresetModeValue = tmpPresetMode;
                    onPresetMode(tmpPresetMode);
                }
                return;
            }

            int tmpPower = onPowerPattern(unknownData);
            if (tmpPower >= 0)
            {
                if (!PowerCmd.inQueue)
                {
                    log("evaluateData", "onPower(" + tmpPower + ")");
                    PowerValue = tmpPower;
                    onPower(tmpPower);
                }
                return;
            }

            /* /live/camdata.html differs in how it print outs a few values, code below is to fix that. */

            int tmpGainCamData = onGainCamDataPattern(unknownData);
            if (tmpGainCamData >= 0)
            {
                if (!GainCmd.inQueue)
                {
                    log("evaluateData", "onGain(" + tmpGainCamData + ")");
                    GainValue = tmpGainCamData;
                    Gain120Value = tmpGainCamData;
                    onGain(tmpGainCamData);
                }
                return;
            }

            int tmpPedestalCamData = onPedestalCamDataPattern(unknownData);
            if (tmpPedestalCamData >= 0)
            {
                if (!PedestalCmd.inQueue)
                {
                    log("evaluateData", "onPedestal(" + tmpPedestalCamData + ")");
                    PedestalValue = tmpPedestalCamData;
                    onPedestal(tmpPedestalCamData);
                }
                return;
            }

            int tmpRedGainCamData = onRedGainCamDataPattern(unknownData);
            if (tmpRedGainCamData >= 0)
            {
                if (!RedGainCmd.inQueue)
                {
                    log("evaluateData", "onRedGain(" + tmpRedGainCamData + ")");
                    RedGainValue = tmpRedGainCamData;
                    onRedGain(tmpRedGainCamData);
                }
                return;
            }

            int tmpBlueGainCamData = onBlueGainCamDataPattern(unknownData);
            if (tmpBlueGainCamData >= 0)
            {
                if (!BlueGainCmd.inQueue)
                {
                    log("evaluateData", "onBlueGain(" + tmpBlueGainCamData + ")");
                    BlueGainValue = tmpBlueGainCamData;
                    onBlueGain(tmpBlueGainCamData);
                }
                return;
            }

            /* V 1.1 - Start */

            int tmpRedGain120CamData = onRedGain120CamDataPattern(unknownData);
            if (tmpRedGain120CamData >= 0)
            {
                if (!RedGain120Cmd.inQueue)
                {
                    log("evaluateData", "onRedGain120(" + tmpRedGain120CamData + ")");
                    RedGain120Value = tmpRedGain120CamData;
                    onRedGain120(tmpRedGain120CamData);
                }
                return;
            }

            int tmpBlueGain120CamData = onBlueGain120CamDataPattern(unknownData);
            if (tmpBlueGain120CamData >= 0)
            {
                if (!BlueGain120Cmd.inQueue)
                {
                    log("evaluateData", "onBlueGain120(" + tmpBlueGain120CamData + ")");
                    BlueGain120Value = tmpBlueGain120CamData;
                    onBlueGain120(tmpBlueGain120CamData);
                }

                return;
            }

            int tmpRedPedestalCamData = onRedPedestalCamDataPattern(unknownData);
            if (tmpRedPedestalCamData >= 0)
            {
                if (!RedPedestalCmd.inQueue)
                {
                    log("evaluateData", "onRedPedestal(" + tmpRedPedestalCamData + ")");
                    RedPedestalValue = tmpRedPedestalCamData;
                    onRedPedestal(tmpRedPedestalCamData);
                }
                return;
            }

            int tmpBluePedestalCamData = onBluePedestalCamDataPattern(unknownData);
            if (tmpBluePedestalCamData >= 0)
            {
                if (!BluePedestalCmd.inQueue)
                {
                    log("evaluateData", "onBluePedestal(" + tmpBluePedestalCamData + ")");
                    BluePedestalValue = tmpBlueGain120CamData;
                    onBluePedestal(tmpBluePedestalCamData);
                }
                return;
            }

            /* V 1.1 - Slut */

            int tmpZoom = onZoomPattern(unknownData);
            if (tmpZoom >= 0)
            {
                if (!ZoomCmd.inQueue && !ZoomPositionCmd.inQueue)
                {
                    log("evaluateData", "onZoom(" + tmpZoom + ")");
                    ZoomPositionValue = tmpZoom;
                    onZoom(tmpZoom);
                }
                return;
            }

            int tmpFocus = onFocusPattern(unknownData);
            if (tmpFocus >= 0)
            {
                log("evaluateData", "onFocus(" + tmpFocus + ")");
                FocusPositionValue = tmpFocus;
                onFocus(tmpFocus);
                return;
            }

            int tmpNDFilter = onNDFilterPattern(unknownData);
            if (tmpNDFilter >= 0)
            {
                log("evaluateData", "onNDFilter(" + tmpNDFilter + ")");
                NDFilterValue = tmpNDFilter;
                onNDFilter(tmpNDFilter);
                return;
            }

            onModelNameHandler(unknownData);

            int tmpScene = onScenePattern(unknownData);
            if (tmpScene >= 0)
            {
                log("evaluateData", "onScene(" + tmpScene + ")");
                SceneValue = tmpScene;
                onScene(tmpScene);
            }

            int tmpSceneQuery = onSceneQueryPattern(unknownData);
            if (tmpSceneQuery >= 0)
            {
                if (cameraType == 120 || cameraType == 130)
                {
                    tmpSceneQuery++;
                }

                log("evaluateData", "onScene(" + tmpSceneQuery + ")");
                onScene(tmpSceneQuery);
            }
        }

        /// <summary>
        /// Evaluates a single line of data.
        /// </summary>
        private void evaluateQueryData(string body)
        {
            try
            {
                evaluateData(body);
            }
            catch (Exception error)
            {
                log("evaluateQueryData", error.Message);
            }
        }

        /// <summary>
        /// Sends queued commands to camera in certain intervals.
        /// </summary>
        private void EventCommandServer()
        {
            log("EventCommandServer", "Server Started");
            while (commandThreadActive)
            {
                CommandHolder cmd = null;

                /* Gets every second command from the different queues, if queue is empty use the other one.*/
                if ((eventQueueCounter++ % 2) == 0)
                {
                    /* Test if queue is empty*/
                    if (eventQueuePositioning.Count > 0)
                    {
                        cmd = eventQueuePositioning.Dequeue();
                    }
                    else if (eventQueueSettings.Count > 0)
                    {
                        cmd = eventQueueSettings.Dequeue();
                    }
                }
                else
                {
                    /* Test if queue is empty*/
                    if (eventQueueSettings.Count > 0)
                    {
                        cmd = eventQueueSettings.Dequeue();
                    }
                    else if (eventQueuePositioning.Count > 0)
                    {
                        cmd = eventQueuePositioning.Dequeue();
                    }
                }

                /* If we found any command to run. */
                if (cmd != null)
                {
                    cmd.inQueue = false;
                    simpleWebClient(cmd.cmd);
                }

                /* Command server sleeps 150ms between each command. */
                System.Threading.Thread.Sleep(150);
            }
        }

        /// <summary>
        /// Handler for GainStepDown
        /// </summary>
        /// <param name="body"></param>
        private void GainStepDownHandler(string body)
        {
            /* Kolla vad gainen är. */
            int tmpGain = onGainPattern(body);

            log("GainStepDownHandler", "cameraType = " + cameraType.ToString());
            log("GainStepDownHandler", "tmpGain = " + tmpGain.ToString());

            if (cameraType == 50 || cameraType == 60)
            {
                /* 50 kameror */
                if (tmpGain >= 11)
                {
                    switch (tmpGain)
                    {
                        case 11:
                            Gain = 8;
                            break;

                        case 14:
                            Gain = 11;
                            break;

                        case 17:
                            Gain = 14;
                            break;

                        case 20:
                            Gain = 17;
                            break;

                        case 23:
                            Gain = 20;
                            break;

                        case 26:
                            Gain = 23;
                            break;
                    }
                }
            }
            else
            {
                log("GainStepDownHandler", "This is HE-120");
                /* 120/130 kameror */
                if (tmpGain > 8 && tmpGain <= 26)
                {
                    log("GainStepDownHandler", "Decresing Gain by -1");
                    Gain120 = --tmpGain;
                }
            }
        }

        /// <summary>
        /// Handler for GainStepUp
        /// </summary>
        /// <param name="body"></param>
        private void GainStepUpHandler(string body)
        {
            /* Kolla vad gainen är. */
            int tmpGain = onGainPattern(body);

            if (cameraType == 50 || cameraType == 60)
            {
                /* 50 kameror */
                if (tmpGain >= 8)
                {
                    switch (tmpGain)
                    {
                        case 8:
                            Gain = 11;
                            break;

                        case 11:
                            Gain = 14;
                            break;

                        case 14:
                            Gain = 17;
                            break;

                        case 17:
                            Gain = 20;
                            break;

                        case 20:
                            Gain = 23;
                            break;

                        case 23:
                            Gain = 26;
                            break;
                    }
                }
            }
            else
            {
                /* 120/130 kameror */
                if (tmpGain >= 8 && tmpGain < 26)
                {
                    Gain120 = ++tmpGain;
                }
            }
        }

        /* Private values, stores camera information */
        /* Stores temporary data of cameras options */
        /* Holds commands for different camera options */
        /* This is all the events that exists on the camera */
        /* Delegates used */
        /* Private functions. */

        /// <summary>
        /// if debug=true this functions prints out debug messages in the console.
        /// </summary>
        private void log(string function, string message)
        {
            if (debug)
            {
                onLog(function, message);
            }
        }

        private int onAutoFocusPattern(string line)
        {
            return simplePatternPattern(@"^d1(\d)$", line);
        }

        private int onAWBModePattern(string line)
        {
            return simplePatternPattern(@"^OAW:([\d]{1,})$", line);
        }

        private int onBlueGain120CamDataPattern(string line)
        {
            return simplePatternPattern(@"^OBI:0x([\w\d]{3})$", line, true);
        }

        private int onBlueGain120Pattern(string line)
        {
            return simplePatternPattern(@"^OBI:([\w\d]{3})$", line, true);
        }

        private int onBlueGainCamDataPattern(string line)
        {
            return simplePatternPattern(@"^OBG:0x([\w\d]{2})$", line, true);
        }

        private int onBlueGainPattern(string line)
        {
            return simplePatternPattern(@"^OBG:([\w\d]{2})$", line, true);
        }

        private int onBluePedestalCamDataPattern(string line)
        {
            return simplePatternPattern(@"^OBP:0x([\w\d]{3})$", line, true);
        }

        private int onBluePedestalPattern(string line)
        {
            return simplePatternPattern(@"^OBP:([\w\d]{3})$", line, true);
        }

        private int onChromaPattern(string line)
        {
            return simplePatternPattern(@"^OCG:([\d]{2})$", line);
        }

        private int onChroma130Pattern(string line)
        {
            return simplePatternPattern(@"^OSD:B0:([\d]{2})$", line);
        }

        private int onFocusPattern(string line)
        {
            return simplePatternPattern(@"^axf([\w\d]{3})$", line, true);
        }

        private int onGainCamDataPattern(string line)
        {
            return simplePatternPattern(@"^OGU:0x([\w\d]{2})$", line, true);
        }

        private int onGainPattern(string line)
        {
            return simplePatternPattern(@"^OGU:([\w\d]{2})$", line, true);
        }

        private int[] onLensInformationPattern(string line)
        {
            Regex regexp = new Regex(@"^LPI([\w\d]{3})([\w\d]{3})([\w\d]{3})$", RegexOptions.IgnoreCase);
            Match matches = regexp.Match(line);
            int[] lensInformation = new int[3];

            /* Nollställ data */
            lensInformation[0] = -1;
            lensInformation[1] = -1;
            lensInformation[2] = -1;

            while (matches.Success)
            {
                for (int i = 1; i <= matches.Groups.Count; i++)
                {
                    Group matchGroup = matches.Groups[i];
                    CaptureCollection matchCollection = matchGroup.Captures;

                    for (int j = 0; j < matchCollection.Count; j++)
                    {
                        string hex = matchCollection[j].ToString();
                        lensInformation[i - 1] = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                matches = matches.NextMatch();
            }

            return lensInformation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        private void onModelName(int name)
        {

            /* Set cameratype */
            cameraType = name;
            
            
            switch (name)
            {
                case 120:
                case 130:
                    log("onModelName", "HE-120");

                    /* Pedestal */
                    simpleWebClient("/cgi-bin/aw_cam?cmd=QTD&res=1", evaluateQueryData);

                    /* Blue Pedestal */
                    simpleWebClient("/cgi-bin/aw_cam?cmd=QBP&res=1", evaluateQueryData);

                    /* NDFilter */
                    simpleWebClient("/cgi-bin/aw_cam?cmd=QFT&res=1", evaluateQueryData);

                    Thread.Sleep(400);
                    break;
            }

            /* Default extra info */

            /* Chroma */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QCG&res=1", evaluateQueryData);
            Thread.Sleep(400);

            /* Tally */
            simpleWebClient("/cgi-bin/aw_ptz?cmd=%23DA&res=1", evaluateQueryData);
            Thread.Sleep(400);

            /* Shutter  */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QSH&res=1", evaluateQueryData);
            Thread.Sleep(400);

            /* Syncro  */
            simpleWebClient("/cgi-bin/aw_cam?cmd=QMS&res=1", evaluateQueryData);
            Thread.Sleep(400);
        }

        private int onModelNamePattern(string line)
        {
            return simplePatternPattern(@"^OID:AW-HE(40|50|60|120|130)$", line);
        }

        private int onNDFilterPattern(string line)
        {
            return simplePatternPattern(@"^OFT:([\d]{1})$", line);
        }

        private int onPedestalCamDataPattern(string line)
        {
            return simplePatternPattern(@"^OTD:0x([\w\d]{2})$", line, true);
        }

        private int onPedestalPattern(string line)
        {
            return simplePatternPattern(@"^OTD:([\w\d]{2})$", line, true);
        }

        private int onPowerPattern(string line)
        {
            return simplePatternPattern(@"^p([\d]{1})$", line, true);
        }

        private int onPresetCompletePattern(string line)
        {
            return simplePatternPattern(@"^q([\d]{1,})$", line);
        }

        private int onPresetModePattern(string line)
        {
            return simplePatternPattern(@"^OSE:71:([\d]{1,})$", line);
        }

        private int onRedGain120CamDataPattern(string line)
        {
            return simplePatternPattern(@"^ORI:0x([\w\d]{3})$", line, true);
        }

        private int onRedGain120Pattern(string line)
        {
            return simplePatternPattern(@"^ORI:([\w\d]{3})$", line, true);
        }

        private int onRedGainCamDataPattern(string line)
        {
            return simplePatternPattern(@"^ORG:0x([\w\d]{2})$", line, true);
        }

        private int onRedGainPattern(string line)
        {
            return simplePatternPattern(@"^ORG:([\w\d]{2})$", line, true);
        }

        private int onRedPedestalCamDataPattern(string line)
        {
            return simplePatternPattern(@"^ORP:0x([\w\d]{3})$", line, true);
        }

        private int onRedPedestalPattern(string line)
        {
            return simplePatternPattern(@"^ORP:([\w\d]{3})$", line, true);
        }

        private int onScenePattern(string line)
        {
            return simplePatternPattern(@"^XSF:([\d]{1})$", line);
        }

        private int onSceneQueryPattern(string line)
        {
            return simplePatternPattern(@"^OSF:([\d]{1})$", line);
        }

        private int onShutterStepPattern(string line)
        {
            return simplePatternPattern(@"^OSH:([\w\d]{1})$", line, true);
        }

        private int onShutterSyncroPattern(string line)
        {
            return simplePatternPattern(@"^OMS:([\w\d]{3})$", line, true);
        }

        private int onTallyPattern(string line)
        {
            return simplePatternPattern(@"^dA([\d]{1})$", line, true);
        }

        private int onZoomPattern(string line)
        {
            return simplePatternPattern(@"^axz([\w\d]{3})$", line, true);
        }

        /// <summary>
        /// Takes an pattern and returns the first capturecollection in the first group of the match in line, value is return like an integer, if no match return -1.
        /// </summary>
        private int simplePatternPattern(string pattern, string line, bool isHex = false)
        {
            Regex regexp = new Regex(pattern, RegexOptions.IgnoreCase);
            Match matches = regexp.Match(line);

            while (matches.Success)
            {
                for (int i = 1; i <= matches.Groups.Count; i++)
                {
                    Group matchGroup = matches.Groups[i];
                    CaptureCollection matchCollection = matchGroup.Captures;

                    for (int j = 0; j < matchCollection.Count; j++)
                    {
                        if (isHex)
                        {
                            string hex = matchCollection[j].ToString();
                            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                        }
                        else
                        {
                            return Convert.ToInt32(matchCollection[j].ToString());
                        }
                    }
                }
                matches = matches.NextMatch();
            }
            return -1;
        }

        /// <summary>
        /// Function som anropar en önskad hemsida, och exkverar simpleWebClientCallback om den har definiterats.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="simpleWebClientCallback"></param>
        private void simpleWebClientThread(string url, simpleWebClientDelegate simpleWebClientCallback = null)
        {
            /* fix url, temoves leading slash because Panasonic fails if there is dubbel slash in the url, like // */
            char slash = '/';
            url = url.TrimStart(slash);
            Uri uri = new Uri(protocol + "://" + host + "/" + url);

            int ThreadNmber = WebClientCounter++;

            if (debug)
            {
                log("simpleWebClientThread", "ThreadNumber=" + ThreadNmber + ", username=" + username + ", password=" + password + ", url=" + uri.AbsoluteUri);
            }

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.ToString());
                request.Timeout = webTimeout;
                request.ReadWriteTimeout = webTimeout;
                request.ServicePoint.Expect100Continue = false;
                request.ServicePoint.MaxIdleTime = webTimeout;
                request.ServicePoint.ConnectionLeaseTimeout = -1;
                request.ServicePoint.ConnectionLimit = 4;

                /* OM användare har fyllt i användarnamn och lösenord skicka med credentials i anropet.*/
                if (useCredentials)
                {
                    request.Credentials = new NetworkCredential(username, password);
                }

                using (WebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (simpleWebClientCallback != null)
                    {
                        Stream stream = response.GetResponseStream();
                        StreamReader streamReader = new StreamReader(stream);
                        string body = streamReader.ReadToEnd();
                        simpleWebClientCallback(body);
                    }
                }

                request = null;

                log("simpleWebClientThread", "ThreadNumber=" + ThreadNmber + ", Done=Yes");
                connectionAtempts = 0;
            }
            catch (WebException e)
            {
                connectionAtempts++;
                log("simpleWebClientThread", "ThreadNumber=" + ThreadNmber + ",Error=" + e.Status + ", connectionAtempts=" + connectionAtempts);
            }
        }

        /// <summary>
        /// Starts listening server
        /// </summary>
        private void startListen()
        {
            /* create an new thread for the tcpServer .*/
            listenThread = new Thread(new ThreadStart(tcpServer));
            listenThread.Name = "tcpServer";
            listenThread.Start();
        }

        /// <summary>
        /// stops listening server
        /// </summary>
        private void stopListen()
        {
            /* Unregister the tcpserver from the camera. */
            simpleWebClient("/cgi-bin/event?connect=stop&my_port=" + tcpServerPort + "&uid=0");

            /* Stop tcpListener */
            if (tcpListener != null)
            {
                ListenThreadActive = false;
                tcpListener.Server.Close();
                tcpListener.Stop();
            }

            /* Stop Thread*/
            if (listenThread != null)
            {
                listenThread.Join();
            }
        }

        /// <summary>
        /// Handles all incoming connections from clients, one execution per connection.
        /// </summary>
        private void tcpClientPattern(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[556];
            int bytesRead = 0;

            try
            {
                //blocks until a client sends a message
                bytesRead = clientStream.Read(message, 0, 556);
            }
            catch
            {
                //a socket error has occured
                tcpClient.Close();
                return;
            }

            int removeBefore = 28;
            int removeAfter = 24;
            int newLength = bytesRead - (removeBefore + removeAfter);

            if (newLength > 0)
            {
                byte[] updateNotificationInformationByteMessage = new byte[newLength];

                /* Hämta hur lång datan är. */
                Array.Copy(message, removeBefore, updateNotificationInformationByteMessage, 0, newLength);

                /* Omvandla byte data till string */
                string updateNotificationInformationMessage = Encoding.UTF8.GetString(updateNotificationInformationByteMessage);

                /* Matcha mottaget meddelande mot [CR][LF] data [CR][LF]  */
                string updateNotificationInformationPattern = @"[\r\n](.+?)[\r\n]";
                Regex r = new Regex(updateNotificationInformationPattern, RegexOptions.IgnoreCase);
                Match updateNotificationInformationMatches = r.Match(updateNotificationInformationMessage);

                /* Loopa igenom varje träff vi fick ovan av [CR][LF]DCB:1[CR][LF] */
                while (updateNotificationInformationMatches.Success)
                {
                    /* grupper specifieras i mönstret genom användandet av () */
                    for (int i = 1; i <= updateNotificationInformationMatches.Groups.Count; i++)
                    {
                        Group matchGroup = updateNotificationInformationMatches.Groups[i];
                        CaptureCollection matchCollection = matchGroup.Captures;
                        for (int j = 0; j < matchCollection.Count; j++)
                        {
                            Capture c = matchCollection[j];
                            log("tcpClientPattern", "Event=" + c);
                            evaluateData(c.ToString());
                        }
                    }
                    updateNotificationInformationMatches = updateNotificationInformationMatches.NextMatch();
                }
            }

            tcpClient.Close();
        }

        /// <summary>
        /// Sets up an tcp server that listens to an radom port, registers it to the Panasonic Camera and waits for events.
        /// </summary>
        private void tcpServer()
        {
            /* Port zero is choosen to get an random free port. */
            this.tcpListener = new TcpListener(IPAddress.Any, 0);
            this.tcpListener.Start();

            /* Retrieves the random tcp port to be stored as an varaible.*/
            tcpServerPort = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            log("tcpServer", "tcpServerPort=" + tcpServerPort);

            /* Registers the tcp server on the camera to get events. We also turns on LPI on the camera. */
            simpleWebClient("/cgi-bin/event?connect=start&my_port=" + tcpServerPort + "&uid=0");
            Thread.Sleep(300);

            simpleWebClient("/cgi-bin/aw_ptz?cmd=%23LPC1&res=1");
            Thread.Sleep(400);

            /* Get cameras current state.*/
            update();

            /* Loops forever while listening on clients.*/
            while (ListenThreadActive)
            {
                try
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this.tcpListener.AcceptTcpClient();

                    if (debug)
                    {
                        log("tcpServer", "client connected (" + client.Client.RemoteEndPoint.ToString() + ")");
                    }

                    //create a thread to handle communication
                    //with connected client
                    Thread clientThread = new Thread(new ParameterizedThreadStart(tcpClientPattern));
                    clientThread.Name = "tcpClientPattern";
                    clientThread.Start(client);
                }
                catch (SocketException se)
                {
                    log("tcpServer", se.Message);
                    continue;
                }
            }
        }
    }

    internal class CommandHolder
    {
        public string cmd = "";
        public bool inQueue = false;
    }
}