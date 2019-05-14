"""
IntentRouter skill repackages Alexa requests and routes them to a remote system
"""
from __future__ import print_function
import datetime
import boto3
import json
import base64


def route_request(target, topic, requestID, requestbody):
    responsebody = {
                    "version": "1.0",
                    "sessionAttributes": {}, 
                    "response": {
                        "outputSpeech": {
                            "type": "PlainText", 
                            "text": "The computer for the skill is not responding."
                        },
                        "card": {
                            "type": "Simple",
                            "title": "Error",
                            "content": "The computer for the skill is not responding."
                        }, 
                        "shouldEndSession": True}}
    sqs = boto3.resource('sqs')
    callqueue = None
    
    if target == None:
        print("Route resolution failed.  Use the Alexa app to set your location, including device name") 
    else:
        try:
            callqueue = sqs.get_queue_by_name(QueueName=target + '_s2p')
        except:
            print("SQS call queue '" + target + "' not found or access is denied") 

    if not callqueue == None: 
        response = callqueue.send_message(MessageBody=topic, MessageAttributes={
            'target': {
                'StringValue': target,
                'DataType': 'String'
            },
            'topic': {
                'StringValue': topic,
                'DataType': 'String'
            },
            'reqid': {
                'StringValue': requestID,
                'DataType': 'String'
            },        
            'reqbody': {
                'StringValue': json.dumps(requestbody),
                'DataType': 'String'
            }
        })


        retqueue = None
        try:
            retqueue = sqs.get_queue_by_name(QueueName=target + '_p2s')    
        except:
            print("SQS receive queue '" + target + "' not found or access is denied") 

        while not retqueue == None:
            
            response = []
            try:
                response = retqueue.receive_messages(
                    MessageAttributeNames=['All'],
                    MaxNumberOfMessages=1,
                    WaitTimeSeconds=14
                )    
            except:
                print("SQS receive timed out")
                

            if not response:
                print("No response received for request " + requestID)
                retqueue = None
                responsebody = {
                    "version": "1.0",
                    "sessionAttributes": {}, 
                    "response": {
                        "outputSpeech": {
                            "type": "PlainText", 
                            "text": "The skill timed out waiting for a response"
                        },
                        "card": {
                            "type": "Simple",
                            "title": "Time Out Error",
                            "content": "The skill timed out waiting for a response."
                        },
                        "shouldEndSession": True}}
            else:
                for msg in response:
                    #print(msg.message_attributes)
                    try:
                        if (requestID == msg.message_attributes["reqid"]["StringValue"]):
                            print("handling matched response: " + str(msg.message_attributes))
                            responsebody = json.loads(msg.message_attributes["respbody"]["StringValue"])
                            msg.delete()
                            retqueue = None
                            break
                        else:
                            #must have had a bad message in the queue first.  Try again...
                            print("Response with mismatched requestID")
                            msg.delete()
                    except KeyError as err:
                        print("Message key error: " + str(err))
                        msg.delete()
                        continue
    return responsebody


# --------------- Main handler ------------------

def lambda_handler(event, context):
    
    starttime = datetime.datetime.now()
    print('lambda entry time: ' + str(starttime))
    print("event: " + str(event)) #json.dumps(event))


    """ Route the incoming request based on type (LaunchRequest, IntentRequest,
    etc.) The JSON body of the request is provided in the event parameter.
    """

    """
    Prevent someone else from configuring a skill that sends requests to this
    function. Uncomment and include your skill's ID rather than the SKILL ID string
    """
    #if (event['session']['application']['applicationId'] !=
    #         "SKILL ID"):
    #     raise ValueError("Invalid Application ID")

    
    """
    default response if nothing else can be done with the request
    """
    response = {
        "version": "1.0",
        "sessionAttributes": {}, 
        "response": {
            "outputSpeech": {
                "type": "PlainText", 
                "text": "There was a problem with the skill lambda handler"
            },
            "card": {"type": "LinkAccount"},
            "shouldEndSession": True}}


    """
    Pull the event apart to know the details
    """
    topic = event['request']['type']
    reqid = event['request']['requestId']
    if topic == "IntentRequest":
        topic = event['request']['intent']['name']

    """
    Ensure the skill has been linked
    """
    usertoken = None
    try:
        usertoken = event['session']['user']['accessToken']
        event['session']['user']['accessToken'] = '<CENSORED>'
    except KeyError as err:
        print("access token missing: " + str(err))

    
    """
    route the request based on the device address
    """
    if usertoken == None:
        response['response']['outputSpeech']['text'] = "The skill has not been linked.  Please visit the alexa app and enable the skill"
    else:
        print("resolving route")
        cidpclient = boto3.client('cognito-idp');
        userinfo = cidpclient.get_user(AccessToken=usertoken)
        print("userinfo: " + str(userinfo))
        routename = None
        try:
            #print ('user attributes: ' + str(userinfo['UserAttributes']))
            for attrib in userinfo['UserAttributes']:
                print(str(attrib))
                if attrib['Name'] == 'custom:Client':
                    routename=attrib['Value']
                    print("route name: " + routename)
        except:
            print("user attributes do not include custom client field.   This means nobody installed a skill client for this account")
            

        if routename == None:
            response['response']['outputSpeech']['text'] = "This account has not been configured. At least one PC skill client must be installed"
        else:
            response = route_request(routename, topic, reqid, event)
            
    print('response: ' + str(response))
    return response
    