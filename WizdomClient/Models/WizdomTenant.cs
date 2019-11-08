using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wizdom.Client
{
    public class WizdomTenant
    {
        public int LicenseID { get; set; }
        public string TenantId { get; set; }
        public string LicenseName { get; set; }
        public string SharepointSiteUrl { get; set; }
        public string WizdomHostUrl { get; set; }
        public string MobileAppUrl { get; set; }
        public string PowerPanelUrl { get; set; }
    }
}
