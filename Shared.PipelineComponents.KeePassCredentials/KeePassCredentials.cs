using System;
using System.ComponentModel;
using Microsoft.BizTalk.Component.Interop;
using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.BizTalk.Streaming;
using IComponent = Microsoft.BizTalk.Component.Interop.IComponent;
using BizTalkComponents.Utils;
using System.Reflection;

namespace Shared.PipelineComponents
{
    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [System.Runtime.InteropServices.Guid("062da000-5973-419e-9144-9e6e41465ef3")]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    public partial class KeePassCredentials : IComponent, IBaseComponent, IPersistPropertyBag,IComponentUI
    {
        [ThreadStatic]
        static ConcurrentDictionary<string, QualifiedContextCredentials> transportTypes = null;

        [ThreadStatic]
        static ConcurrentDictionary<string, KeyValuePair<string, string>> keePassEntries = null;

        private string keePassDatabase = Environment.GetEnvironmentVariable("KeePassDatabase");
        private string keePassKey = Environment.GetEnvironmentVariable("KeePassKey");
        private string keePassRoot = Environment.GetEnvironmentVariable("KeePassRoot");

        private ConcurrentDictionary<string, KeyValuePair<string, string>> KeePassEntries
        {
            get
            {
                if (keePassEntries == null)
                {
                    string utilityPath = Path.Combine(keePassRoot, "Shared.Components.KeePassUtility.dll");

                    if (File.Exists(utilityPath) == false)
                        throw new FileNotFoundException();

                    //Utility must exist in the same folder as KeePass.exe
                    Assembly assembly = Assembly.LoadFrom(utilityPath);
                    Type T = assembly.GetType("KeePassUtility.Utility");


                    if (File.Exists(keePassDatabase) == false)
                        throw new FileNotFoundException("KeePass database could not be found!", keePassDatabase);

                    if (File.Exists(keePassKey) == false)
                        throw new FileNotFoundException("KeePass key file could not be found!", keePassKey);


                    object[] parm = new object[] { keePassDatabase, keePassKey, null };

                    T.GetMethod("GetAllEntries").Invoke(null, parm);

                    keePassEntries = (ConcurrentDictionary<string, KeyValuePair<string, string>>)parm[2];


                }

                return keePassEntries;
            }
        }

        private ConcurrentDictionary<string, QualifiedContextCredentials> TransportTypes
        {
            get
            {
                if (transportTypes == null)
                {
                   
                    transportTypes = new ConcurrentDictionary<string, QualifiedContextCredentials>();
                    
                    transportTypes.TryAdd("HTTP",new QualifiedContextCredentials { Username = new HTTP.Username().Name, Password = new HTTP.Password().Name });
                    transportTypes.TryAdd("SOAP", new QualifiedContextCredentials { Username = new SOAP.Username().Name, Password = new SOAP.Password().Name });
                    transportTypes.TryAdd("FILE", new QualifiedContextCredentials { Username = new FILE.Username().Name, Password = new FILE.Password().Name });
                    transportTypes.TryAdd("Windows SharePoint Services", new QualifiedContextCredentials { Username = new WSS.ConfigSharePointOnlineUsername().Name, Password = new WSS.ConfigSharePointOnlinePassword().Name });
                    transportTypes.TryAdd("SMTP", new QualifiedContextCredentials { Username = new SMTP.Username().Name, Password = new SMTP.Password().Name });
                    //transportTypes.TryAdd("SQL", new QualifiedContextCredentials { Username = new SQL.().Name, Password = new SQL.Password().Name });
                    transportTypes.TryAdd("FTP",  new QualifiedContextCredentials { Username = new FTP.UserName().Name, Password = new FTP.Password().Name });
                    //transportTypes.TryAdd("MSMQ", new QualifiedContextCredentials { Username = new MSMQ.UserName().Name, Password = new FTP.Password().Name });
                   // transportTypes.TryAdd("MQSeries", "http://schemas.microsoft.com/BizTalk/2003/mqs-properties");
                    transportTypes.TryAdd("WCF-BasicHttp", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                    transportTypes.TryAdd("WCF-Custom", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                    transportTypes.TryAdd("WCF-WSHttp", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                    transportTypes.TryAdd("WCF-WebHttp", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                    transportTypes.TryAdd("WCF-BasicHttpRelay", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                    transportTypes.TryAdd("SFTP", new QualifiedContextCredentials { Username = new SFTP.UserName().Name, Password = new SFTP.Password().Name });
                    transportTypes.TryAdd("WCF-SQL", new QualifiedContextCredentials { Username = new WCF.UserName().Name, Password = new WCF.Password().Name });
                }
                    

                return transportTypes;
            }
        }

        [DisplayName("Entry")]
        [Description("KeePass entry")]
        [RequiredRuntime]
        public string Entry { get; set; }

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            QualifiedContextCredentials contextCredentials = null;


            ContextProperty OutboundTransportType = new ContextProperty("OutboundTransportType", "http://schemas.microsoft.com/BizTalk/2003/system-properties");

            object transportType = pInMsg.Context.Read(OutboundTransportType);

            if (transportType == null)
                throw new ArgumentException("OutboundTransportType does not exist in context!");

            if(TransportTypes.TryGetValue((string)transportType, out contextCredentials) == false)
                throw new NotImplementedException(String.Format("Transporttype {0} is not handled!", (string)transportType));
            

            KeyValuePair<string, string> credentials;

            if (KeePassEntries.TryGetValue(Entry, out credentials) == false)
                throw new MissingMemberException(String.Format("Could not find entry {0} in KeePass database!", Entry));

           

            pInMsg.Context.Write(contextCredentials.Username.Name, contextCredentials.Username.Namespace, credentials.Key);

           //The context property marked as Sensitive can not be promoted. You can only write them to context.
            pInMsg.Context.Write(contextCredentials.Password.Name, contextCredentials.Password.Namespace, credentials.Value);

            pInMsg.Context.Promote("IsDynamicSend", "http://schemas.microsoft.com/BizTalk/2003/system-properties", true);

            return pInMsg;
        }

        #region Load and Save methods
        //Load and Save are generic, the functions create properties based on the components "public" "read/write" properties.
        public void Load(IPropertyBag propertyBag, int errorLog)
        {
            var props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var prop in props)

            {

                if (prop.CanRead & prop.CanWrite)

                {

                    prop.SetValue(this, PropertyBagHelper.ReadPropertyBag(propertyBag, prop.Name, prop.GetValue(this)));

                }

            }


        }
        
        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            var props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var prop in props)

            {

                if (prop.CanRead & prop.CanWrite)

                {

                    PropertyBagHelper.WritePropertyBag(propertyBag, prop.Name, prop.GetValue(this));

                }

            }

        }

        #endregion
    }
}
