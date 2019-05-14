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
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PCSkillExample
{
    public partial class IRCExample : Form
    {
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
                { "HelloIntent",  ((thisObj, target, topic, requestbody) => thisObj.HelloIntent(target,topic,requestbody))}
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
                        "Your example skill is now listening.  Ask to say hello", "", false)));
        }


        private string HelloIntent(string target, string topic, string requestbody)
        {
            object response = irc.ProgressDialog(requestbody);
            if (response == null)
            {
                response = irc.BuildResponse(irc.GetSessionAttributes(requestbody),
                                    irc.BuildSpeechletResponse(topic, "Hello World", "", true));
            }

            return (new JavaScriptSerializer()).Serialize(response);
        }
    }
}
