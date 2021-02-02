using System;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using Bitventure_Assignment.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml;
using RestSharp;
using System.Linq;

namespace Bitventure_Assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            //read in json file stored in /bin/debug/netcore/netcoreapp3.1
            var jsonFile = File.ReadAllText("bonus_endpoints.json");
            JSONServiceStructure.Root myJsonObject = JsonConvert.DeserializeObject<JSONServiceStructure.Root>(jsonFile);

            //loop through each service
            foreach(JSONServiceStructure.Service service in myJsonObject.services)
            {
                if (service.enabled)
                {
                    //loop through each endpoint
                    foreach(JSONServiceStructure.Endpoint endpoint in service.endpoints)
                    {
                        if (endpoint.enabled)
                        {
                            Console.WriteLine("Checking " + service.baseURL + endpoint.resource);

                            //returns the response received from the endpoint using RestSharp
                            //a 0 is added into the function as it enters a recursive loop, and should make it stop eventually
                            string response = GetResponse(service.baseURL + endpoint.resource, 0);
                            if (!String.IsNullOrEmpty(response))
                            {
                                Console.WriteLine("Valid response receieved:");
                                Console.WriteLine(response);
                                
                                //void function that prints to terminal all the info about the received data from the endpoint
                                //it uses identifiers defined for each service to check each element
                                CheckResponse(response, endpoint.response, service.datatype, service.identifiers);

                            }
                            else
                            {
                                Console.WriteLine("Error - Endpoint does not produce a valid response");
                            }


                        }
                    }
                }

            }

            Console.ReadLine();

        }

        static string GetResponse(string endPoint, int recBreak)
        {
           IRestResponse response = null;
            //only allow recursive function to execute twice
            //once without a slash and once with a slash
           if(recBreak < 2)
           {
                var client = new RestClient(endPoint);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                response = client.Execute(request);

                if (String.IsNullOrEmpty(response.Content))
                {
                    //Due to no trailing slash on endpoint resulting in a 301 server error, this check needs to be done
                    string newEndpoint = endPoint + "/";
                    return GetResponse(newEndpoint, recBreak += 1);
                }
           }

            return response.Content;

        }

        static void CheckResponse(string responseString, JSONServiceStructure.Response[] responses, string dataType, JSONServiceStructure.Identifier[] identifiers)
        {
            string valueToCheck;
            foreach(JSONServiceStructure.Response response in responses)
            {
                Console.Write("Checking element: " + response.element);

                //response valuetype could either be JSON or XML
                if(dataType == "JSON")
                {
                    var jsonObject = JToken.Parse(responseString);
                    valueToCheck = FindJSONValueRecursive(jsonObject, response.element, 0);
                }
                else
                {
                    XmlDocument xmlObject = SerializeXML(responseString);
                    valueToCheck = FindXMLValue(xmlObject, response.element);
                }


                if (!String.IsNullOrEmpty(valueToCheck))
                {
                    if(response.identifier != null)
                    {
                        string identifier = GetIdentifier(identifiers, response.identifier);

                        if (!String.IsNullOrEmpty(identifier))
                        {
                            Console.WriteLine(" matches identifier: " + identifier);
                            Console.WriteLine("Result: ");
                            if (valueToCheck == identifier)
                            {
                                Console.WriteLine("True");
                            }
                            else
                            {
                                Console.WriteLine("False");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nCould not find the requested identifier: " + response.identifier);
                        }

                        
                    }
                    else
                    {
                        Console.WriteLine(" matches regex: " + response.regex + "?");
                        Regex rgx = new Regex(response.regex);
                        if (rgx.IsMatch(valueToCheck))
                        {
                            Console.WriteLine("True");
                        }
                        else
                        {
                            Console.WriteLine("False");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Could not find element: " + response.element);
                }
            }
        }

        //recursively add wildcards in order to search nested elements for the element i need to find
        static string FindJSONValueRecursive(JToken jsonObject, string valueToFind, int wildCards)
        {
            //only allow searching up to 10 levels of nesting in the JSON object
            if(wildCards < 10)
            {
                string wildCardString = "";
                for (int i = 0; i < wildCards; i++)
                {
                    wildCardString += "*.";
                }
                wildCardString += valueToFind;

                //SelectToken can work by including as many "*." as deep as the element is in the object
                var value = jsonObject.SelectToken(wildCardString)?.ToObject<string>();
                if (value != null)
                {
                    return value;

                }
                else
                {
                    wildCards += 1;
                    return FindJSONValueRecursive(jsonObject, valueToFind, wildCards);
                }               
            }
            else
            {
                //if the element does not exist within the first 10 levels of the object
                return null;
            }

        }

        static string FindXMLValue(XmlDocument xmlDoc, string valueToFind)
        {
            XmlNodeList elemList = xmlDoc.GetElementsByTagName(valueToFind);

            return elemList[0].InnerXml.ToString();

        }


        static XmlDocument SerializeXML(string xml)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                return xmlDoc;
            }
            catch (Exception e)
            { 
                throw new Exception(e.Message);
            }
        }

        static string GetIdentifier(JSONServiceStructure.Identifier[] identifiers, string itemToIdentify)
        {
            //? operator to not allow value.ToString to execute if identifiers does not have the required key
            string value = identifiers.ToList().Find(x => x.key == itemToIdentify)?.value.ToString();
           
            return value;
        }


    }
}
