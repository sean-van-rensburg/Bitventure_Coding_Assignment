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

namespace Bitventure_Assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            //read in json file stored in /bin/debug/netcore/netcoreapp3.1
            var jsonFile = File.ReadAllText("bonus_endpoints.json");
            JSONServiceStructure.Root myJsonObject = JsonConvert.DeserializeObject<JSONServiceStructure.Root>(jsonFile);

            foreach(JSONServiceStructure.Service service in myJsonObject.services)
            {
                if (service.enabled)
                {
                    foreach(JSONServiceStructure.Endpoint endpoint in service.endpoints)
                    {
                        if (endpoint.enabled)
                        {
                            Console.WriteLine("Checking " + service.baseURL + endpoint.resource);
                            string response = GetResponse(service.baseURL + endpoint.resource);
                            if (!String.IsNullOrEmpty(response))
                            {
                                Console.WriteLine("Valid response receieved:");
                                Console.WriteLine(response);
                                
                                CheckResponse(response, endpoint.response, service.datatype, service.identifiers);

                            }
                            else
                            {
                                Console.WriteLine("Error - Endpoint does not produce a valid JSON");
                            }


                        }
                    }
                }

            }

            Console.ReadLine();

        }

        static string GetResponse(string endPoint)
        {
            var client = new RestClient(endPoint);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "*/*");
            IRestResponse response = client.Execute(request);

            if (String.IsNullOrEmpty(response.Content))
            {
                //Due to no trailing slash resulting in a 301 server error, this check needs to be done
                string newEndpoint = endPoint + "/";
                return GetResponse(newEndpoint);
            }

            return response.Content;

        }

        static void CheckResponse(string responseString, JSONServiceStructure.Response[] responses, string dataType, JSONServiceStructure.Identifier[] identifiers)
        {
            string valueToCheck;
            foreach(JSONServiceStructure.Response response in responses)
            {
                Console.Write("Checking element: " + response.element);

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


        static string FindJSONValueRecursive(JToken jsonObject, string valueToFind, int wildCards)
        {
            if(wildCards < 10)
            {
                string wildCardString = "";
                for (int i = 0; i < wildCards; i++)
                {
                    wildCardString += "*.";
                }
                wildCardString += valueToFind;

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
            if (String.IsNullOrEmpty(xml)) throw new NotSupportedException("Empty string!!");

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
            foreach(var item in identifiers)
            {
                if(item.key == itemToIdentify)
                {
                    return item.value;
                }
            }
            return null;
        }


    }
}
