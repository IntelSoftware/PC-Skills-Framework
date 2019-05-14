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

using IntentRouterClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PCSkillExample
{
    public partial class IRCExample : Form
    {
        public const string SKILL_MANUFACTURER = "Intel Corp";
        public const string SKILL_NAME = "PCSkill Example";

        protected IntentRouterClientClass irc = null;

        public IRCExample()
        {
            InitializeComponent();
            irc = IntentRouterClientClass.CreateIntentRouterClient(SKILL_MANUFACTURER, SKILL_NAME, true);
            irc.LogCallback += OnLogMessage;
        }
        
        private void Startup()
        {
            OnLogMessage("Startup", null,true);
            button1.Text = "Stop";
            irc.Start(IntentHandler, this);
        }

        private void Shutdown()
        {
            button1.Text = "Start";
            irc.Stop();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (0 == button1.Text.CompareTo("Start"))
                Startup();
            else
                Shutdown();
        }


        List<object[]> nonCriticalLogs = new List<object[]>();
        void OnLogMessage(string msg, Exception ex, bool critical)
        {
#if DEBUG  // Do nothing if we are running in non-debug mode
            if (LogOutputBox.InvokeRequired)
            {
                if (!critical && ex == null)
                {
                    lock (nonCriticalLogs)
                        nonCriticalLogs.Add(new object[] { "[" + DateTime.UtcNow.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) + "]: " + msg, null });
                }
                else
                    LogOutputBox.Invoke(irc.LogCallback, new object[] { msg, ex, false });
            }
            else lock (nonCriticalLogs)
                {
                    nonCriticalLogs.Add(new object[] { "[" + DateTime.UtcNow.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) + "]: " + msg, ex });
                    foreach (object[] logline in nonCriticalLogs)
                    {
                        LogOutputBox.Items.Add(Regex.Replace(logline[0].ToString(), "[\n\r]", ""));
                        if (logline[1] != null)
                            LogOutputBox.Items.Add("Exception:  " + Regex.Replace(logline[1].ToString(), "[\n\r]", ""));
                    }
                    nonCriticalLogs.Clear();
                    LogOutputBox.SelectedIndex = LogOutputBox.Items.Count - 1;
                }
#endif
        }

        private void IRCExample_SizeChanged(object sender, EventArgs e)
        {
            var logsize = LogOutputBox.Size;
            logsize.Height = LogOutputBox.Bottom - (button1.Bottom + 10);
            LogOutputBox.Size = logsize;
        }
    }
}
