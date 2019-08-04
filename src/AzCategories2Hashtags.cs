using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzCategories2Hashtags
{
	public static class AzCategories2Hashtags
    {
        [FunctionName("AzCategories2Hashtags")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var contentType = req.Headers["Content-Type"].FirstOrDefault() ?? "application/xml";
                var elementName = req.GetQueryParam("ElementName", "category");

                log.LogInformation(contentType);
                log.LogInformation(elementName);
                
                if(contentType.Equals("application/xml", StringComparison.InvariantCultureIgnoreCase)){
                    log.LogInformation("Executing XML Conversion...");

                    var attributeName = req.GetQueryParam("AttributeName", "term");
                    var postXml = XDocument.Parse(requestBody);

                    var categoryList = postXml
                        .Root?
                        .Elements()
                        .Where(x => string.Equals(x.Name.LocalName, elementName, StringComparison.InvariantCultureIgnoreCase))
                        .Attributes()
                        .Where(x => string.Equals(x.Name.LocalName, attributeName, StringComparison.InvariantCultureIgnoreCase))
                        .Select(x => CleanCategory(x.Value))
                        .ToList();

                    var categories = string
                        .Join(" ", categoryList);

                    return new OkObjectResult(categories);
                }

                if(contentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase)){
                    JObject json = JsonConvert.DeserializeObject<JObject>(requestBody);
                    var categoryList = JsonConvert
                        .DeserializeObject<JArray>(json[elementName].ToString())
                        .Select(x => CleanCategory(x.Value<string>()));

                    var categories = string
                        .Join(" ", categoryList);

                    return new OkObjectResult(categories);
                }

                return new OkObjectResult(string.Empty);
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex);
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
