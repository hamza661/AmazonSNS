using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
namespace AmazonSNS
{
   /*
  * Created By Saif Iqbal
  * Dated : Aug 24 2015
  * Purpose: To raise any notification on amazon sns whenever any changes made to patient intake form.
  */
    public class AWSSNS
    {
        #region Private Methods
        private void PublishSNSEvent(string Message, string AmazonSNSKey, string AmazonSNSSecretKey, string TopicName, string EventType)
        {
            string topicname = ProcessSNSTopicName(TopicName);
            try
            {
            
                var client = new AmazonSimpleNotificationServiceClient(AmazonSNSKey, AmazonSNSSecretKey);
                string topicarn = string.Empty;
                PublishResult publishResult = new PublishResult();
                if (IsTopicExists(client, topicname, out topicarn))
                {
                    PublishRequest publishRequest = new PublishRequest();
                    publishRequest.Message = Message;
                    publishRequest.TopicArn = topicarn;
                    PublishResponse publishResponse = client.Publish(publishRequest);
                    publishResult = publishResponse.PublishResult;
                }
                else
                {
                    CreateTopicRequest topicRequest = new CreateTopicRequest();
                    topicRequest.Name = topicname;
                    CreateTopicResponse topicResponse = client.CreateTopic(topicRequest);
                    CreateTopicResult result = topicResponse.CreateTopicResult;
                    PublishRequest publishRequest = new PublishRequest();
                    publishRequest.Message = Message;
                    publishRequest.TopicArn = result.TopicArn;
                    PublishResponse publishResponse = client.Publish(publishRequest);
                    publishResult = publishResponse.PublishResult;
                }
                if (publishResult.IsSetMessageId())
                {
                    CreateLogs(Message, EventType, topicname, true);
                }
            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                /*Log details to logentries server.*/
                CreateLogs(Message, EventType, topicname, false);
                throw ex;
            }
        }
        private string ProcessSNSTopicName(string TopicName)
        {
            string topicname = string.Empty;
            try
            {
                Regex r = new Regex("(?:[^a-zA-Z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                topicname = r.Replace(TopicName, String.Empty);
                topicname = topicname.Replace(" ", string.Empty);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return topicname;
        }
        private string CreateMessageBody(string IntakeID, string UserID, string Eventtype)
        {
            string Message = "";
            try
            {
                Message = String.Format(Constants.SNSMessageBody, Eventtype, IntakeID, UserID);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Message;
        }
        private void CreateLogs(string message, string EventName, string TopicName, bool IsSucess)
        {
            try
            {
                DbWrapper.ExecuteNonQuery("CreateSNSLogs",
                      message, TopicName, EventName,
                       IsSucess);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void ProcessSNSEvent(string IntakeID, string UserID, string EventType, string AmazonSNSKey, string AmazonSNSSecretKey, string TopicName)
        {
            try
            {
                    string message = CreateMessageBody(IntakeID.ToString(), UserID, EventType);
                    PublishSNSEvent(message, AmazonSNSKey, AmazonSNSSecretKey, TopicName, EventType);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private bool IsTopicExists(AmazonSimpleNotificationServiceClient client, string Topic, out string TopicArn)
        {
            ListTopicsRequest lstTopicRequest = new ListTopicsRequest();
            ListTopicsResponse lstTopicResponse = client.ListTopics(lstTopicRequest);
            ListTopicsResult lstTopicResult = lstTopicResponse.ListTopicsResult;
            List<Topic> TopicsList = lstTopicResult.Topics;
            TopicArn = string.Empty;
            bool IsTopicExists = TopicsList.Exists(x => x.TopicArn.Split(':')[x.TopicArn.Split(':').Count() - 1] == Topic);
            if (IsTopicExists)
            {
                string topicarn = TopicsList.Where(x => x.IsSetTopicArn() & x.TopicArn.Split(':')[x.TopicArn.Split(':').Count() - 1] == Topic).Select(s => s.TopicArn).First();
                TopicArn = topicarn;
            }
            return IsTopicExists;
        }
        #endregion

        #region Public Methods
        public void CreateSNSEvent(int FormID, string IntakeID, string UserID, string AmazonSNSKey, string AmazonSNSSecretKey, string TopicName, string eventType = "")
        {
            try
            {
                string EventType = string.Empty;

                switch (FormID)
                {
                    case 1002:            /**/
                    EventType="Updated";
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(eventType))
                {
                    EventType = eventType;
                }

                ProcessSNSEvent(IntakeID, UserID, EventType, AmazonSNSKey, AmazonSNSSecretKey, TopicName);


                            
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void SubscribeEvents(string emailaddress)
        {
            

        }
        #endregion 
    }

}