using System;
using System.Collections;
using System.Linq;
using BizTalkComponents.Utils;
using System.ComponentModel;

namespace Shared.PipelineComponents
{
    public partial class KeePassCredentials
    {
        [Browsable(false)]
        public string Name { get { return "KeePassCredentials"; } }
        [Browsable(false)]
        public string Version { get { return "1.0"; } }
        [Browsable(false)]
        public string Description { get { return "Get KeePass Credentials"; } }

        [Browsable(false)]
        public void GetClassID(out Guid classID)
        {
            classID = new Guid("062da000-5973-419e-9144-9e6e41465ef3");
        }

        [Browsable(false)]
        public void InitNew()
        {

        }

        public IEnumerator Validate(object projectSystem)
        {
           
            return ValidationHelper.Validate(this, false).ToArray().GetEnumerator();
        }

        public bool Validate(out string errorMessage)
        {
            var errors = ValidationHelper.Validate(this, true).ToArray();

            if (errors.Any())
            {
                errorMessage = string.Join(",", errors);

                return false;
            }

            keePassDatabase = Environment.GetEnvironmentVariable("KeePassDatabase");
            keePassKey = Environment.GetEnvironmentVariable("KeePassKey");
            keePassRoot = Environment.GetEnvironmentVariable("KeePassRoot");

            if (String.IsNullOrEmpty(keePassDatabase) || String.IsNullOrEmpty(keePassKey) || String.IsNullOrEmpty(keePassRoot))
            {
                errorMessage = "One or more KeePass environment variables are missing! Expected environment variables (KeePassDatabase,KeePassKey,KeePassRoot)";

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

        [Browsable(false)]
        public IntPtr Icon { get { return IntPtr.Zero; } }
    }
}
