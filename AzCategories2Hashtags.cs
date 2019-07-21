using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.Linq;

namespace CalvinAAllen
{
	public static class AzCategories2Hashtags
    {
        [FunctionName("AzCategories2Hashtags")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try {
                var elementName = req.GetQueryParam("ElementName", "category");
                var attributeName = req.GetQueryParam("AttributeName", "term");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var postXml = XDocument.Parse(requestBody);
                
                var categoryList = postXml
                    .Root
                    .Elements()
                    .Where(x => string.Equals(x.Name.LocalName, elementName, StringComparison.InvariantCultureIgnoreCase))
                    .Attributes()
                    .Where(x => string.Equals(x.Name.LocalName, attributeName, StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => CleanCategory(x.Value))
                    .ToList();

                var categories = string
                    .Join(" ", categoryList);

                return (ActionResult)new OkObjectResult(categories);
            }
            catch (Exception ex) {
                return (ActionResult)new BadRequestObjectResult(ex);
            }
        }

        private static string GetQueryParam(this HttpRequest hr, string paramName, string defaultValue = ""){
            var queryString = hr?.Query;

            if(queryString == null){
                return defaultValue;
            }

            if(queryString.ContainsKey(paramName)){
                return queryString[paramName][0];
            }
            
            return defaultValue;
        }

        private static string CleanCategory(string category){
            var cleanCategory = new string(
                    category
                    .Select(c => char.IsPunctuation(c) ? '-' : c) //replace anything else with a dash
                    .ToArray()
                )
                .Replace("-", "")       // now remove the dash
                .Replace(" ", "");      // remove any spaces

            return $"#{cleanCategory}"; // add the hash
        }
    }
}
