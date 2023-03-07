using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Zeptomoby.OrbitTools;

namespace EphemerisFunctions
{
    public class EphemerisRetrieve
        /*
         * Will retrieve Satellite Ephmemeris Information and record it to a blob table
         * Will auto-refresh periodically as Satelitte Ephemeris changes constantly
         */
    {
        [FunctionName("Function")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            Dictionary<string, Tle> satellites = null; satellites = await DownloadEphemeris();
        }

        private static async Task<Dictionary<string, Tle>> DownloadEphemeris()
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

            string satName = "";
            string tle2 = "";

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
                    // Once we have line 2 we can process
                    Tle satelliteTle = new Tle(satName, tle2, tleLine);
                    satMap.Add(satName, satelliteTle);
                    lineLocated = true;
                }
                if (tleLine.Length > 2 && lineLocated == false)
                {
                    // Name Line
                    satName = tleLine.Trim();
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
