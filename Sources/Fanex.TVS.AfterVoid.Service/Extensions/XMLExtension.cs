using System.Collections.Generic;
using System.Linq;
using System.Text;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Extensions
{
    public static class XMLExtension
    {
        public static string ToVoidResultsXML(this IEnumerable<Result> results)
        {
            if (results == null || !results.Any())
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();
            foreach (var result in results)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMsg))
                {
                    var line = $"<VoidResult Message='{result.ErrorMsg}' Type='{result.ErrorCode}' Licensees='{result.Data}' />";
                    stringBuilder.AppendLine(line);
                }
            }

            return $"<VoidResults>{stringBuilder.ToString().Trim()}</VoidResults>";
        }
    }
}