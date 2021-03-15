//using RadiusR_Customer_Website.GenericCustomerServiceReference;
using MasterISS.CustomerService.GenericCustomerServiceReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website
{
    public class ServiceUtilities
    {
        MasterISS.CustomerService.GenericCustomerServiceReference.GenericCustomerServiceClient client = new MasterISS.CustomerService.GenericCustomerServiceReference.GenericCustomerServiceClient();
        public CustomerServiceGenericAppSettingsResponse CustomerWebsiteGenericSettings()
        {
            var baseRequest = new GenericServiceSettings();
            var result = client.GenericAppSettings(new CustomerServiceGenericAppSettingsRequest()
            {
                Username = baseRequest.Username,
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand
            });
            return result;
        }
        public CustomerServiceGetCustomerFileResponse GetCustomerDocuments(long? subscriptionId)
        {
            var baseRequest = new GenericServiceSettings();
            var result = client.GetCustomerFiles(new CustomerServiceBaseRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = subscriptionId
                }
            });
            return result;
        }
        public CustomerServiceGetClientAttachmentResponse GetClientAttachment(long? subscriptionId, string fileName)
        {
            var baseRequest = new GenericServiceSettings();
            var result = client.GetClientAttachment(new CustomerServiceGetClientAttachmentRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                GetClientAttachment = new GetClientAttachmentRequest()
                {
                    FileName = fileName,
                    SubscriptionId = subscriptionId
                }
            });
            return result;
        }
        //public CustomerServiceSaveSupportAttachmentResponse SaveSupportAttachment(long stageId, string fileName, byte[] fileContent, string fileExtention, long? supportRequestId)
        //{
        //    var baseRequest = new GenericServiceSettings();
        //    var result = client.SaveSupportAttachment(new CustomerServiceSaveSupportAttachmentRequest()
        //    {
        //        Culture = baseRequest.Culture,
        //        Hash = baseRequest.Hash,
        //        Rand = baseRequest.Rand,
        //        Username = baseRequest.Username,
        //        SaveSupportAttachmentParameters = new SaveSupportAttachmentRequest()
        //        {
        //            FileContent = fileContent,
        //            FileExtention = fileExtention,
        //            FileName = fileName,
        //            StageId = stageId,
        //            SupportRequestId = supportRequestId
        //        }
        //    });
        //    return result;
        //}
        public CustomerServicGetSupportAttachmentListResponse GetSupportAttachmentList(long? supportId)
        {
            var baseRequest = new GenericServiceSettings();
            var result = client.GetSupportAttachmentList(new CustomerServiceGetSupportAttachmentListRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                GetSupportAttachmentsParameters = new GetSupportAttachmentListRequest()
                {
                    RequestId = supportId
                }
            });
            return result;
        }
        public CustomerServiceGetSupportAttachmentResponse GetSupportAttachment(long? supportId, string fileName)
        {
            var baseRequest = new GenericServiceSettings();
            var result = client.GetSupportAttachment(new CustomerServiceGetSupportAttachmentRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                GetSupportAttachmentParameters = new GetSupportAttachmentRequest()
                {
                    FileName = fileName,
                    SupportRequestId = supportId
                }
            });
            return result;
        }
        public CustomerServiceGetCustomerInfoResponse GetCustomerInfo(long subscriptionId)
        {
            var baseRequest = new GenericServiceSettings();
            var subscription = client.GetCustomerInfo(new CustomerServiceBaseRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Username = baseRequest.Username,
                Rand = baseRequest.Rand,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = subscriptionId
                }
            });
            return subscription;
        }
    }
}