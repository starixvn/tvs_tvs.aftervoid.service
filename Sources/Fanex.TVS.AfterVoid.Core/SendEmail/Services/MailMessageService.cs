using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Resources;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface MailMessageService
    {
        Result CreateSuccess(string siteName, string[] listEmail);

        Result CreateFail(string siteName);

        List<Result> InsertCombineLicenseesName(List<Result> results, string siteNameSuccess, string siteNameFails);
    }

    public class MailMessageServiceImp : MailMessageService
    {
        public Result CreateSuccess(string siteName, string[] listEmail)
        {
            return new Result
            {
                ErrorCode = ErrorCode.SendEmailToLicenseeSuccess,
                ErrorMsg = string.Format(Labels.VoidBetDetailsSendMailSuccess, siteName, string.Join("; ", listEmail)),
            };
        }

        public Result CreateFail(string siteName)
        {
            return new Result
            {
                ErrorCode = ErrorCode.SendEmailToLicenseeFail,
                ErrorMsg = string.Format(Labels.VoidBetDetailsSendMailFail, siteName),
            };
        }

        public List<Result> InsertCombineLicenseesName(List<Result> results, string siteNameSuccess, string siteNameFails)
        {
            InsertSuccessfulCombine(results, siteNameSuccess);
            InsertFailedCombine(results, siteNameFails);
            return results;
        }

        private static void InsertSuccessfulCombine(List<Result> results, string licensees)
        {
            if (string.IsNullOrEmpty(licensees))
            {
                return;
            }

            var successLicensees = new Result
            {
                ErrorCode = ErrorCode.LicenseeSuccess,
                ErrorMsg = string.Format(Labels.VoidBetSendMailSuccess, licensees),
            };
            results.Add(successLicensees);
        }

        private static void InsertFailedCombine(List<Result> results, string licensees)
        {
            if (string.IsNullOrEmpty(licensees))
            {
                return;
            }

            var failedLicenses = new Result
            {
                ErrorCode = ErrorCode.LicenseeFail,
                ErrorMsg = string.Format(Labels.VoidBetSendMailFail, licensees),
            };
            results.Add(failedLicenses);
        }
    }
}