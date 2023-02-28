using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Zeptomoby.OrbitTools;
using System.Collections.Generic;
using System.Net.Http;


namespace EphemerisFunctions
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            log.LogInformation("C# HTTP trigger function processed a request.");

            Dictionary<string, Tle> satellites = null; satellites = await Function1.DownloadEphemeris();


            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            Tle satTle;
            satellites.TryGetValue(name, out satTle);
            Satellite sat = new Satellite(satTle);
            EciTime eci = sat.PositionEci(DateTime.UtcNow);

            // Reference Postion to my location
            Site siteLocal = new Site(-31.9505, 115.86, 0); // 0.00 N, 100.00 W, 0 km altitude
            Geo g = new Geo(eci, new Julian(DateTime.UtcNow));


            // Other thing to Watch - Longtitude is 0 to 360 degrees..
            g.LatitudeDeg.ToString();
            g.LongitudeDeg.ToString();
            g.Altitude.ToString();

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        public static async Task<Dictionary<string, Tle>> DownloadEphemeris()
        {
            HttpClient httpClient = new HttpClient();
            String tle = await httpClient.GetStringAsync("https://www.celestrak.com/NORAD/elements/amateur.txt");

           
            String[] tleLines = tle.ToString().Split(new[] { Environment.NewLine },
                                    StringSplitOptions.None);

            // Remember its 4 lines
            // Name
            // Line 1
            // Line 2
            // Gap

            string tle1 = "";
            string tle2 = "";
            string tle3 = "";

            var satMap = new Dictionary<string, Tle>();

            foreach (var tleLine in tleLines)
            {
                bool lineLocated = false;
                // Check if not a 1 or 2
                if (tleLine.StartsWith("1 "))
                {
                    // line 1
                    lineLocated = true;
                    tle2 = tleLine;
                    lineLocated = true;
                }
                if (tleLine.StartsWith("2 "))
                {
                    // line 2
                    tle3 = tleLine;
                    // Once we have line 2 we can process
                    Tle satelliteTle = new Tle(tle1, tle2, tle3);
                    satMap.Add(tle1, satelliteTle);
                    lineLocated = true;
                }
                if (tleLine.Length > 2 && lineLocated == false)
                {
                    // Name Line
                    tle1 = tleLine.Trim();
                    Console.WriteLine(tleLine);
                    // Increment as we have found a satellite
                }
                else
                {
                    // Blank Line
                }
            }
            return satMap;

        }
    }
}
