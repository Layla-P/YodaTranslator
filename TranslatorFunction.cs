using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace YodaFunction
{
    public class TranslatorFunction
    {
        [FunctionName("TranslatorFunction")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string body = req.Query["name"];

            if (string.IsNullOrEmpty(body))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var formData = FormData(requestBody);
                var postBody = formData["Body"];

                body = postBody;
            }

            var translated = await Translator(body);

            
            return body != null
                ? (ActionResult) new ContentResult{Content=$"<Response><Message>Hello, {translated}</Message></Response>", ContentType = "application/xml", StatusCode = 200}
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            
        }
        
       
        private Dictionary<string, string> FormData(string formQuery)
        {
            var formDictionary = new Dictionary<string, string>();
            var formArray = formQuery.Split('&');
            foreach (var item in formArray)
            {
                var pair = item.Split('=');
                var key = WebUtility.UrlDecode(pair[0]);
                var value = WebUtility.UrlDecode(pair[1]);
                formDictionary.Add(key, value);
            }

            return formDictionary;
        }

        private async Task<string> Translator(string text)
        {
            var textEncoded = HttpUtility.UrlEncode (text, System.Text.Encoding.UTF8);
            var endpoint = $"https://api.funtranslations.com/translate/sith.json?text={textEncoded}";
            HttpResponseMessage response;
            string translatedText = "default";
            

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var responseMessage = await
                    client
                        .GetAsync(endpoint);
                
                if (responseMessage.IsSuccessStatusCode)
                {
                    string jsonContent = await responseMessage.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(jsonContent);
                    translatedText = data.contents.translated;       
                }
                else if(responseMessage.StatusCode == (HttpStatusCode)429)  {
                    //Fun Translations API rate limits to 5 calls an hour unless you are using the paid for service.
                    translatedText = "too much yoda speak, the force be with you for an hour";
                }
                
                
            }
           
            return translatedText;
        }
    }
}