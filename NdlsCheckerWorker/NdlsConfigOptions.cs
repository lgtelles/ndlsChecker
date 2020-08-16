using System;

namespace NdlsCheckerWorker
{
    public class NdlsConfigOptions
    {
        public const string NdlsConfig = "NdlsConfig";

        public string NoEarlierThan { get; set; }
        public string NoLaterThan { get; set; }

        public DateTime GetNoEarlierThan()
        {
            return Convert.ToDateTime(NoEarlierThan);
        }
        
        public DateTime GetNoLaterThan()
        {
            return Convert.ToDateTime(NoLaterThan);
        }
    }
}