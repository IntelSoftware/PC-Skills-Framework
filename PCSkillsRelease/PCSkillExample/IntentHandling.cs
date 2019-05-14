// Copyright 2019 Intel Corporation.
//
// The source code, information and material ("Material") contained herein is owned by
// Intel Corporation or its suppliers or licensors, and title to such Material remains
// with Intel Corporation or its suppliers or licensors. The Material contains
// proprietary information of Intel or its suppliers and licensors. The Material is
// protected by worldwide copyright laws and treaty provisions. No part of the
// Material may be used, copied, reproduced, modified, published, uploaded, posted,
// transmitted, distributed or disclosed in any way without Intel's prior express
// written permission. No license under any patent, copyright or other intellectual
// property rights in the Material is granted to or conferred upon you, either
// expressly, by implication, inducement, estoppel or otherwise. Any license under
// such intellectual property rights must be express and approved by Intel in writing.
//
// Unless otherwise agreed by Intel in writing, you may not remove or alter this
// notice or any other notice embedded in Materials by Intel or Intel's suppliers or
// licensors in any way.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PCSkillExample
{
    public partial class IRCExample : Form
    {
        ControlAppWindows winCTL = new ControlAppWindows();
        private static Dictionary<string, Func<IRCExample, string, string, string, string>> RequestHandlers =
            new Dictionary<string, Func<IRCExample, string, string, string, string>>()
            {
                //REQUIRED intent handling
                { "LaunchRequest", ((thisObj, target, topic, requestbody) => thisObj.LaunchRequest(target,topic,requestbody))},
                { "AMAZON.CancelIntent", ((thisObj, target, topic, requestbody) => thisObj.CancelIntent(target,topic,requestbody))},
                { "AMAZON.StopIntent", ((thisObj, target, topic, requestbody) => thisObj.StopRequest(target,topic,requestbody))},
                { "AMAZON.HelpIntent", ((thisObj, target, topic, requestbody) => thisObj.HelpRequest(target,topic,requestbody))},
                { "AMAZON.FallbackIntent", ((thisObj, target, topic, requestbody) => thisObj.FallBackIntent(target,topic,requestbody))},
                { "AMAZON.NavigateHomeIntent", ((thisObj, target, topic, requestbody) => thisObj.NavigateHomeIntent(target,topic,requestbody))},
                { "SessionEndedRequest" , ((thisObj, target, topic, requestbody) => thisObj.SessionEndedRequest(target,topic,requestbody))},
                { "System.ExceptionEncountered" , ((thisObj, target, topic, requestbody) => thisObj.ExceptionEncountered(target,topic,requestbody))},
                //CUSTOM intent handling
                { "SearchPCIntent",  ((thisObj, target, topic, requestbody) => thisObj.PCSearch(target,topic,requestbody))},
                { "PCTaskIntent",  ((thisObj, target, topic, requestbody) => thisObj.TakePCAction(target,topic,requestbody))},
                //{ "PCInfoIntent",  ((thisObj, target, topic, requestbody) => thisObj.GetPCInformation(target,topic,requestbody))},
                { "MinMaxTileUndoIntent",  ((thisObj, target, topic, requestbody) => thisObj.MinMaxTileUndo(target,topic,requestbody))},
                { "ArrangeIntent",  ((thisObj, target, topic, requestbody) => thisObj.Arrange(target,topic,requestbody))}
            };

        private static string IntentHandler(object context, string target, string topic, string reqbody)
        {
            string ret = "{}";
            if (RequestHandlers.ContainsKey(topic))
            {
                string baseRequest = ((IRCExample)context).irc.GetBaseRequest(reqbody);
                string reqType = ((IRCExample)context).irc.GetBaseRequestType(baseRequest);
                if (string.Compare(reqType, "CanFulfillIntentRequest") == 0)
                {
                    //topic is the proposed intent name.  For now, just say yes, rather than check the slots each intent might expect.
                    ret = "{ \"version\":\"1.0\"," +
                            "\"response\":{ " +
                                "\"canFulfillIntent\":{" +
                                    "\"canFulfill\": \"YES\"" +
                          "}}}";
                }
                else
                    ret = RequestHandlers[topic]((IRCExample)context, target, topic, reqbody);
            }
            return ret;
        }

        private string UnhandledRequest(string target, string topic, string reqbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "I'm sorry.  This request is not implemented yet", "", true)));
        }

        private string SessionEndedRequest(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(null, new { }));
        }

        private string ExceptionEncountered(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(null, new { }));
        }

        private string FallBackIntent(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "I am sorry. I didn't understand.", "", true)));
        }

        private string NavigateHomeIntent(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "", "", true)));
        }

        private string HelpRequest(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "This is your help", "", true)));
        }

        private string StopRequest(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                    irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "Goodbye", "", true)));
        }

        private string CancelIntent(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "", "", true)));
        }

        private string LaunchRequest(string target, string topic, string requestbody)
        {
            return (new JavaScriptSerializer()).Serialize(
                    irc.BuildResponse(new { },
                    irc.BuildSpeechletResponse(topic,
                        "What do you want me to do ?", "", false)));
        }

        #region PC Content Search Code
        private string PCSearch(string target, string topic, string requestbody)
        {
            object response = irc.ProgressDialog(requestbody);
            if (response == null)
            {
                string fileLocationValue = irc.GetSlotString(requestbody, "fileLocation");
                string fileTypeValue = irc.GetSlotString(requestbody, "fileType");
                string searchQueryValue = irc.GetSlotString(requestbody, "searchQuery");
                string dateValue = irc.GetSlotString(requestbody, "date");
                OnLogMessage(string.Format("SearchPCIntent-> fileLocationValue: {0}, fileTypeValue: {1}, dateValue: {2}, searchQueryValue: {3}", fileLocationValue, fileTypeValue, dateValue, searchQueryValue), null, true);
                PerformPCSearch(fileLocationValue, fileTypeValue, dateValue, searchQueryValue);
                response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                irc.BuildSpeechletResponse(topic, "Ok, here you go.", "", true));
            }
            return (new JavaScriptSerializer()).Serialize(response);
        }

        public void PerformPCSearch(string fileLocationValue, string fileTypeValue, string dateValue, string searchQueryValue = "")
        {
            fileLocationValue = GetFileLocation(fileLocationValue);
            StringBuilder queryBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(searchQueryValue))
            {
                queryBuilder.Append("search-ms:displayname=Searching for query%20" + searchQueryValue + "%20in%20" + fileLocationValue);
                if (!fileTypeValue.Equals("everything"))
                    queryBuilder.Append("&crumb=System.Generic.String%3A" + searchQueryValue + "%20kind%3A%3D" + fileTypeValue);
                else
                    queryBuilder.Append("&crumb=System.Generic.String%3A" + searchQueryValue);
            }
            else
            {
                queryBuilder.Append("search-ms:displayname=Search%20Results%20in%20" + fileLocationValue);
                if (!fileTypeValue.Equals("everything"))
                    queryBuilder.Append("&crumb=kind%3A%3D" + fileTypeValue);
            }
            if (!string.IsNullOrEmpty(dateValue))
                queryBuilder.Append("&crumb=datemodified%3A" + dateValue);
            queryBuilder.Append("&crumb=location:" + fileLocationValue);
            Process.Start(queryBuilder.ToString());
        }

        /// <summary>
        /// Get Current machine file locaiton
        /// </summary>
        /// <param name="fileLocationValue"></param>
        /// <returns></returns>
        private string GetFileLocation(string fileLocationValue)
        {
            switch (fileLocationValue.ToLower())
            {
                case "documents":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case "desktop":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    break;
                case "downloads":
                    fileLocationValue = System.Convert.ToString(Microsoft.Win32.Registry.GetValue(
                                        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
                                        , "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty));
                    break;
                case "user":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    break;
                case "music":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    break;
                case "pictures":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    break;
                case "recent":
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                    break;
                case "everywhere":
                    fileLocationValue = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                    break;
                default:
                    fileLocationValue = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    break;
            }
            return fileLocationValue;
        }
        #endregion

        #region PC Automation code

        #region PC Component Code
        //private const string WIN32_SCOPE = "root\\CIMV2";
        //private const string WIN32_PROCESSOR = "SELECT * FROM Win32_Processor";
        //private const string WIN32_COMPUTER_SYSTEM = "SELECT * FROM Win32_ComputerSystem";
        //private const string WIN32_OPERATING_SYSTEM = "SELECT * FROM Win32_OperatingSystem";

        //class DeviceInformation
        //{
        //    public string SystemName { get; set; }
        //    public string SystemType { get; set; }
        //    public string SystemDrive { get; set; }
        //    public string ProcessorName { get; set; }
        //    public string ProcessorFrequency { get; set; }
        //    public string ProcessorArchitecture { get; set; }
        //    public string NumberOfProcessors { get; set; }
        //    public string NumberOfLogicalProcessors { get; set; }
        //    public string NumberOfCores { get; set; }
        //    public string Memory { get; set; }
        //    public string TotalPhysicalMemory { get; set; }
        //    public string ModelName { get; set; }
        //    public string OSVersion { get; set; }
        //    public string OSArchitecture { get; set; }
        //    public string OSBuildNumber { get; set; }
        //}

        //public enum CPUArchitecture
        //{
        //    x86 = 0,
        //    MIPS = 1,
        //    Alpha = 2,
        //    PowerPC = 3,
        //    ia64 = 6,
        //    x64 = 9
        //}

        //private static string FormatBytes(long bytes)
        //{
        //    string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
        //    int i;
        //    double dblSByte = bytes;
        //    for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        //    {
        //        dblSByte = bytes / 1024.0;
        //    }

        //    return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        //}

        //private ManagementObjectCollection GetQueryObject(string query)
        //{
        //    ManagementObjectSearcher searcher = null;
        //    try
        //    {
        //        searcher = new ManagementObjectSearcher(WIN32_SCOPE, query);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return searcher.Get();
        //}

        //private string GetPCInformation(string target, string topic, string requestbody)
        //{
        //    object response = irc.ProgressDialog(requestbody);
        //    if (response == null)
        //    {
        //        string value = irc.GetSlotString(requestbody, "component");
        //        value = (string.IsNullOrEmpty(value)) ? "" : value.Replace(" ", string.Empty);
        //        OnLogMessage(string.Format("PCInfoIntent-> Component Name: {0}", value), null, true);

        //        string taskName = "about";
        //        string responseText = string.Empty;
        //        DeviceInformation info = new DeviceInformation();
        //        foreach (ManagementObject queryObj in GetQueryObject(WIN32_COMPUTER_SYSTEM))
        //        {
        //            info.SystemType = queryObj["SystemType"].ToString();
        //            info.SystemName = queryObj["Name"].ToString();
        //            info.ModelName = queryObj["Model"].ToString();
        //            info.TotalPhysicalMemory = queryObj["TotalPhysicalMemory"].ToString();
        //        }
        //        foreach (ManagementObject queryObj in GetQueryObject(WIN32_PROCESSOR))
        //        {
        //            string name = queryObj["Name"].ToString().Replace("(R)", "").Replace("(TM)", "").Replace("CPU", "");
        //            int index = name.IndexOf("@");

        //            info.ProcessorName = name.Substring(0, index - 2);
        //            info.ProcessorFrequency = name.Substring(index + 2);
        //            info.NumberOfCores = queryObj["NumberOfCores"].ToString();
        //            info.ProcessorArchitecture = queryObj["Architecture"].ToString();
        //            info.NumberOfLogicalProcessors = queryObj["NumberOfLogicalProcessors"].ToString();
        //        }
        //        foreach (ManagementObject queryObj in GetQueryObject(WIN32_OPERATING_SYSTEM))
        //        {
        //            info.OSVersion = queryObj["Caption"].ToString();
        //            info.SystemDrive = queryObj["SystemDrive"].ToString();
        //            info.OSArchitecture = queryObj["OSArchitecture"].ToString();
        //            info.OSBuildNumber = queryObj["BuildNumber"].ToString();
        //        }

        //        if (string.Equals(value, "computer", StringComparison.OrdinalIgnoreCase))
        //        {
        //            if (string.IsNullOrEmpty(info.ModelName.Trim()))
        //                responseText = string.Format("Unable to find the model name. Opening system details.");
        //            else
        //                responseText = string.Format("You got a  " + info.ModelName + " PC.");
        //        }
        //        else if (string.Equals(value, "operatingsystem", StringComparison.OrdinalIgnoreCase))
        //            responseText = string.Format("You got a  " + info.OSArchitecture + " " + info.OSVersion + " with build number " + info.OSBuildNumber);
        //        else if (string.Equals(value, "memory", StringComparison.OrdinalIgnoreCase))
        //            responseText = string.Format("The PC got " + FormatBytes(Convert.ToInt64(info.TotalPhysicalMemory)) + " GB of Memory or RAM.");
        //        else if (string.Equals(value, "processor", StringComparison.OrdinalIgnoreCase))
        //            responseText = string.Format("You got a  " + info.ProcessorName + " with a frequency of  " + info.ProcessorFrequency + ". It has " + info.NumberOfCores + " cores and uses " + (CPUArchitecture)Convert.ToInt32(info.ProcessorArchitecture) + " architecture.");
        //        else if (string.Equals(value, "storage", StringComparison.OrdinalIgnoreCase))
        //        {
        //            taskName = "storagesense";
        //            long totalSize = 0;
        //            long availableFreeSpace = 0;
        //             // get all drives ?
        //            foreach (DriveInfo drive in DriveInfo.GetDrives())
        //            {
        //                if (drive.IsReady)
        //                {
        //                    totalSize +=  drive.TotalSize;
        //                    availableFreeSpace += drive.AvailableFreeSpace;
        //                }
        //            }
        //            responseText = string.Format("The PC got " + FormatBytes(availableFreeSpace) + " GB of available disk space from total of " + FormatBytes(totalSize));
        //        }                
        //        else if (string.Equals(value, "display", StringComparison.OrdinalIgnoreCase))
        //        {
        //            taskName = "display";
        //            // For each screen, add the screen properties to a list box.
        //            foreach (var screen in Screen.AllScreens)
        //            {
        //                responseText += "Screen resolution for "+ screen.DeviceName.Replace(@"\","").Replace(".", "") + " is : " + screen.Bounds.Width.ToString() + " by " + screen.Bounds.Height.ToString() + ". ";
        //            }
        //        }
        //        else
        //            responseText = string.Format("I am sorry, I cannot help with this right now.");

        //        LaunchProgram(taskName);
        //        response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
        //                            irc.BuildSpeechletResponse(topic, responseText, "", true));
        //    }
        //    return (new JavaScriptSerializer()).Serialize(response);
        //}
        #endregion

        private string TakePCAction(string target, string topic, string requestbody)
        {
            object response = irc.ProgressDialog(requestbody);
            if (response == null)
            {
                string value = irc.GetSlotString(requestbody, "taskName");
                value = (string.IsNullOrEmpty(value)) ? "" : value.Replace(" ", string.Empty);
                OnLogMessage(string.Format("PCTaskIntent-> Task Name: {0}", value), null, true);
                //Configure Wifi settings
                if (string.Equals(value, "wifi", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Wifi settings.", "", true));
                    LaunchProgram("network-wifi");
                }

                //Configure Ethernet settings
                else if (string.Equals(value, "ethernet", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Ethernet settings.", "", true));
                    LaunchProgram("network-ethernet");
                }

                //Configure notifications

                else if (string.Equals(value, "notifications", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Notifications settings.", "", true));
                    LaunchProgram("privacy-notifications");
                }

                //Configure privacy settings

                else if (string.Equals(value, "privacy", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "apppermissions", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Privacy settings.", "", true));
                    LaunchProgram("privacy-general");
                }


                //Configure display and multi moniotor

                else if (string.Equals(value, "display", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "multimonitor", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the " + value + " settings.", "", true));
                    LaunchProgram("display");
                }

                //Configure Power
                else if (string.Equals(value, "power", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "sleep", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "powerandsleep", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the " + value + " settings.", "", true));
                    LaunchProgram("powersleep");
                }

                //Configure location

                else if (string.Equals(value, "location", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Location settings.", "", true));
                    LaunchProgram("privacy-location");
                }

                //ms-settings:dateandtime

                else if (string.Equals(value, "date", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Date and time settings.", "", true));
                    LaunchProgram("dateandtime");
                }

                //Configure fonts for Accessibility

                else if (string.Equals(value, "accessibility", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Accessibility settings.", "", true));
                    LaunchProgram("easeofaccess-narrator");
                }

                else if (string.Equals(value, "font", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "icon", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Icon and Font settings.", "", true));
                    PerformPCAction("font");
                }

                //Configure Windows hello

                else if (string.Equals(value, "windowshello", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Windows hello setup settings and follow the instructions.", "", true));
                    LaunchProgram("signinoptions-launchfaceenrollment");
                }

                //ms-settings:backup

                else if (string.Equals(value, "backup", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the backup and restore settings.", "", true));
                    LaunchProgram(value);
                }

                //ms - settings:appsfeatures

                else if (string.Equals(value.Replace(" ", ""), "uninstallprograms", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Apps and features settings. Select from the list of apps you want to uninstall.", "", true));
                    LaunchProgram("appsfeatures");
                }

                else if (string.Equals(value, "bluetooth", StringComparison.OrdinalIgnoreCase))
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the Bluetooth settings.", "", true));
                    LaunchProgram("bluetooth");
                }

                else if (string.Equals(value.Replace(" ", ""), "bitlocker", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(value.Replace(" ", ""), "DiskEncryption", StringComparison.OrdinalIgnoreCase))
                { 
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Sure. Opening the "+ value +" settings.", "", true));
                    value = "bitlocker";
                    PerformPCAction(value);
                }

                else
                {
                    response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "I am sorry, I cannot help with this right now.", "", true));
                }
            }
            return (new JavaScriptSerializer()).Serialize(response);
        }

        private void LaunchProgram(string taskname)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("ms-settings:" + taskname);
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = builder.ToString();
            Process.Start(pInfo);
        }

        public void PerformPCAction(string taskName)
        {

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.FileName = "cmd.exe";

            if (string.Equals(taskName, "Backup", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.Arguments = " /C control /name Microsoft.BackupAndRestore";
            }

            else if (string.Equals(taskName, "bitlocker", StringComparison.OrdinalIgnoreCase))
            {

                startInfo.Arguments = " /C control /name Microsoft.BitLockerDriveEncryption";
            }

            else if (string.Equals(taskName, "font", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.Arguments = " /C control fonts";
            }

            process.StartInfo = startInfo;
            process.Start();
        }
        #endregion

        #region PC Manage Windows

        public enum ArrangeAction
        {
            DoQuadrants,
            DoSideBySide,
            DoTopAndBottom
        }

        private string MinMaxTileUndo(string target, string topic, string requestbody)
        {
            object response = irc.ProgressDialog(requestbody);
            if (response == null)
            {
                int result = 0;
                string speechText = "";
                string position = irc.GetSlotString(requestbody, "position");
                string files = irc.GetSlotString(requestbody, "filename");

                if (string.Equals("Maximize", position, StringComparison.OrdinalIgnoreCase))
                    result = winCTL.MaximizeWindow(files);
                else if (string.Equals("Minimize", position, StringComparison.OrdinalIgnoreCase))
                    result = winCTL.MinimizeWindow(files);
                else if (string.Equals("Tile", position, StringComparison.OrdinalIgnoreCase))
                    result = winCTL.TileWindows(files);
                else if (string.Equals("Undo", position, StringComparison.OrdinalIgnoreCase))
                {
                    bool res = winCTL.RestoreSavedState();
                    if (res == true)
                        speechText = string.Format("Last action reversed");
                    else
                        speechText = string.Format("I am sorry.  I cannot undo the arrangement.");
                }
                if (!string.Equals("Undo", position, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals("all", files, StringComparison.OrdinalIgnoreCase))
                    {
                        if (result > 0)
                            speechText = string.Format("Performed action " + position + " on " + files + " windows.");
                        else
                            speechText = string.Format("I am sorry. I do not understand that.");

                    }
                    else
                    {
                        if (result > 0)
                            speechText = string.Format("Performed action " + position + " on " + result + " " + files + " windows.");
                        else
                            speechText = string.Format("I am sorry. I do not understand that.");
                    }

                }
                response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, speechText, "", false));
            }
            return (new JavaScriptSerializer()).Serialize(response);
        }

        private string Arrange(string target, string topic, string requestbody)
        {
            object response = irc.ProgressDialog(requestbody);
            if (response == null)
            {
                string speechText = "";
                string action = irc.GetSlotString(requestbody, "action");
                string app = irc.GetSlotString(requestbody, "app");
                string appA = irc.GetSlotString(requestbody, "appA");
                string appB = irc.GetSlotString(requestbody, "appB");

#if DEBUG
                irc.LogMessage(string.Format("Arrange - action = {0}, app={1}, appA={2}, appB={3}", action, app, appA, appB));
#endif

                if (string.Equals("in quadrants", action, StringComparison.OrdinalIgnoreCase))
                {
                    speechText = PerformArrange(app, appA, appB, ArrangeAction.DoQuadrants);
                }
                else if (string.Equals("top and bottom", action, StringComparison.OrdinalIgnoreCase))
                {
                    speechText = PerformArrange(app, appA, appB, ArrangeAction.DoTopAndBottom);
                }
                else if (string.Equals("side-by-side", action, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("side by side", action, StringComparison.OrdinalIgnoreCase))
                {
                    speechText = PerformArrange(app, appA, appB, ArrangeAction.DoSideBySide);
                }

                response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, speechText, "", false));
            }
            return (new JavaScriptSerializer()).Serialize(response);
        }

        private string PerformArrange(string application, string appA, string appB, ArrangeAction actionName)
        {
            int result = 0;
            string speechText = "";
            if (!string.IsNullOrEmpty(appA) && !string.IsNullOrEmpty(appB))
            {
                string[] appNames = { appA, appB };
                result = winCTL.ArrangeWindows(actionName, appNames);
                if (result > 0)
                    speechText = string.Format("Arranged " + result + " " + appA + " and " + appB + " windows.");
                else
                    speechText = string.Format("I'm sorry.  I cannot arrange those windows");
            }
            else
            {
                string applicationName = "";
                if (!string.IsNullOrEmpty(application))
                    applicationName = application;
                else if (!string.IsNullOrEmpty(appA))
                    applicationName = appA;
                else if (!string.IsNullOrEmpty(appB))
                    applicationName = appB;

                result = winCTL.ArrangeWindows(actionName, applicationName);
                if (result > 0)
                    speechText = string.Format("Arranged " + result + " " + applicationName + " windows.");
                else
                    speechText = string.Format("I'm sorry.  I can't arrange those windows");
            }
            return speechText;
        }
        #endregion
    }
}
