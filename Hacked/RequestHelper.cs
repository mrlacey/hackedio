//-----------------------------------------------------------------------
// <copyright file="Requesthelper.cs" company="Microsoft Limited">
//     Copyright (c) Microsoft Limited, Microsoft Consulting Services, UK. All rights reserved.
// </copyright>
//----------------------------------------------------------------------

namespace Hacked
{
    using System;
    using System.IO;
    using System.Net;

    using Newtonsoft.Json;

    /// <summary>
    /// Web request helper class
    /// </summary>
    internal class RequestHelper
    {
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="body">The body.</param>
        /// <param name="callback">The callback.</param>
        /// <exception cref="System.Exception">Invalid request</exception>
        public static void ExecuteRequest(string url, string body, Action<Exception, string> callback)
        {
            byte[] requestBody = System.Text.Encoding.UTF8.GetBytes(body);

            MakeRequest(callback, url, requestBody);
        }

        /// <summary>
        /// Deserializes the JSON to the specified type.
        /// </summary>
        /// <typeparam name="T">type to deserialize to</typeparam>
        /// <param name="source">The source.</param>
        /// <returns>Deserialized instance of the object</returns>
        public static T DeserializeJsonTo<T>(string source) where T : class
        {
            var settings = new JsonSerializerSettings
            {
                Error = (s, args) =>
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
            };

            return JsonConvert.DeserializeObject<T>(source, settings);
        }

        /// <summary>
        /// Makes the request.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="url">The URL.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="canRetry">if set to <c>true</c> the request can retry automatically if cancelled.</param>
        private static void MakeRequest(Action<Exception, string> callback, string url, byte[] requestBody, bool canRetry = true)
        {
            var webRequest = WebRequest.Create(url);

            if (url.Contains("state"))
            {
                webRequest.Method = "PUT";
            }
            else
            {
                webRequest.Method = "POST";
            }

            webRequest.ContentLength = requestBody.Length;

            webRequest.BeginGetRequestStream(
                result =>
                    {
                        using (var requestStream = webRequest.EndGetRequestStream(result))
                        {
                            requestStream.Write(requestBody, 0, requestBody.Length);
                            requestStream.Close();
                        }

                        webRequest.BeginGetResponse(
                            result2 =>
                                {
                                    try
                                    {
                                        using (var webResponse = webRequest.EndGetResponse(result2))
                                        {
                                            var webStream = webResponse.GetResponseStream();
                                            byte[] buffer = new byte[webResponse.ContentLength];
                                            webStream.BeginRead(
                                                buffer,
                                                0,
                                                (int)webResponse.ContentLength,
                                                readResult =>
                                                    {
                                                        try
                                                        {
                                                            webStream.EndRead(readResult);
                                                            var memStream = new MemoryStream(buffer);
                                                            webStream.Dispose();

                                                            TextReader tr = new StreamReader(memStream);
                                                            var resultString = tr.ReadToEnd();

                                                            System.Diagnostics.Debug.WriteLine("SERVER RETURNED :" + resultString);

                                                            if (resultString != null)
                                                            {
                                                                callback(null, resultString);
                                                            }
                                                            else
                                                            {
                                                                callback(new Exception(resultString), null);
                                                            }
                                                        }
                                                        catch (Exception exception)
                                                        {
                                                            callback(exception, null);
                                                        }
                                                    },
                                                null);
                                        }
                                    }
                                    catch (WebException webEx)
                                    {
                                        if (webEx.Status == WebExceptionStatus.RequestCanceled)
                                        {
                                            // Request canceled: should be because of Fast app switch or Tombstoning.
                                            // Retry the request, once, transparently to the user
                                            if (canRetry)
                                            {
                                                MakeRequest(callback, url, requestBody, canRetry: false);
                                            }
                                        }
                                        else
                                        {
                                            callback(webEx, null);
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        callback(exception, null);
                                    }
                                },
                            null);
                    },
                null);
        }
    }
}
