using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class CombineMessageModel
    {
        public CombineMessageModel()
        {
            Results = new List<Result>();
            SuccessfulLicensees = new List<string>();
            FailedLicensees = new List<string>();
        }

        public List<Result> Results { get; set; }

        public List<string> SuccessfulLicensees { get; set; }

        public List<string> FailedLicensees { get; set; }
    }
}