using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Linq;

#nullable enable
namespace Networking
{
    public static partial class ServerAPI
    {
        public static string BASE_URL => Environment.GetEnvironmentVariable("BASE_URL") ?? "http://127.0.0.1:8000/";
        public static string BASE_URL_API => Environment.GetEnvironmentVariable("BASE_URL_API") ?? "http://127.0.0.1:8000/api/";
        public static int TIMEOUT { get; set; } = 5;

        public static async Task<string?> TryPostAsync(string endpoint, string jsonPayload, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            try
            {
                return await PostAsync(endpoint, jsonPayload, cancellationToken, includeBaseUrl);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> PostAsync(string endpoint, string jsonPayload, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            string url = includeBaseUrl ? BASE_URL_API + endpoint : endpoint;
            using UnityWebRequest request = UnityWebRequest.Post(url, jsonPayload, "application/json");
            await PreprocessRequest(request);

            cancellationToken.Register(() => request.Abort());
            await request.SendWebRequest();
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"{DateTime.Now}: POST api/{endpoint} -> {jsonPayload} ... {request.responseCode} -> {response}");
                return response;
            }
            else
            {
                Debug.LogError($"{DateTime.Now}: POST api/{endpoint} -> {jsonPayload} ... {request.responseCode}");
                string? responseBody = request.downloadHandler?.text;
                throw new ServerAPIException(HttpVerb.Post, endpoint, request.responseCode, request.error, responseBody);
            }
        }

        public static async Task<string?> TryGetAsync(string endpoint, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            try
            {
                return await GetAsync(endpoint, cancellationToken, includeBaseUrl);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            string url = includeBaseUrl ? BASE_URL_API + endpoint : endpoint;
            using UnityWebRequest request = UnityWebRequest.Get(url);
            await PreprocessRequest(request);

            cancellationToken.Register(() => request.Abort());
            await request.SendWebRequest();
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"{DateTime.Now}: GET api/{endpoint} ... {request.responseCode} -> {response}");
                return response;
            }
            else
            {
                Debug.LogError($"{DateTime.Now}: GET api/{endpoint} ... {request.responseCode}");
                string? responseBody = request.downloadHandler?.text;
                throw new ServerAPIException(HttpVerb.Get, endpoint, request.responseCode, request.error, responseBody);
            }
        }

        public static async Task<string?> TryDeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                return await DeleteAsync(endpoint, cancellationToken);
            }
            catch
            {
                return null;
            }
        }
        public static async Task<string> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            using UnityWebRequest request = UnityWebRequest.Delete(BASE_URL_API + endpoint);
            await PreprocessRequest(request, cancellationToken);

            cancellationToken.Register(() => request.Abort());
            await request.SendWebRequest();
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }
            else
            {
                string? responseBody = request.downloadHandler?.text;
                throw new ServerAPIException(HttpVerb.Get, endpoint, request.responseCode, request.error, responseBody);
            }
        }

        public static async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            string response = await GetAsync(endpoint, cancellationToken, includeBaseUrl);
            return JsonConvert.DeserializeObject<T>(response)
                ?? throw new ServerAPIInvalidFormatException<T>(HttpVerb.Get, endpoint, response);
        }
        public static async Task<T> PostAsync<T>(string endpoint, string jsonPayload, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            string response = await PostAsync(endpoint, jsonPayload, cancellationToken, includeBaseUrl);
            return JsonConvert.DeserializeObject<T>(response)
                ?? throw new ServerAPIInvalidFormatException<T>(HttpVerb.Post, endpoint, response);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> PostAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default, bool includeBaseUrl = true)
        {
            return await PostAsync<T>(endpoint, JsonConvert.SerializeObject(payload), cancellationToken, includeBaseUrl);
        }

        private static async Task<bool> PreprocessRequest(UnityWebRequest request, CancellationToken cancellationToken = default)
        {
            request.timeout = TIMEOUT;
            request.SetRequestHeader("Accept", "application/json");
            return await PopulateSessionHeaders(request, cancellationToken);
        }

        public static async Task<bool> PopulateSessionHeaders(UnityWebRequest request, CancellationToken cancellationToken = default)
        {
            if (SessionManager.ShouldRefreshToken())
            {
                await SessionManager.RefreshTokenAsync(cancellationToken);
            }

            if (SessionManager.AuthToken != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.AuthToken);
                return true;
            }
            return false;
        }

        public static void ShowServerFormErrors(ServerAggregateValidationResult validationResult, Dictionary<string, DataField> fields, Label? globalErrorLabel, bool clearPreviousErrors = true, bool treatUnexpectedFieldNamesAsGlobal = false)
        {
            if (clearPreviousErrors)
            {
                foreach (var field in fields.Values)
                {
                    field.ErrorText = string.Empty;
                }
                if (globalErrorLabel != null)
                {
                    globalErrorLabel.text = string.Empty;
                    globalErrorLabel.style.display = DisplayStyle.None;
                }
            }
            List<string> newErrors = new() { validationResult.Message };
            foreach (var fieldValidation in validationResult.Fields)
            {
                if (fields.TryGetValue(fieldValidation.Key, out var field))
                {
                    field.ErrorText = string.Join(", ", fieldValidation.Value);
                    continue;
                }

                if (treatUnexpectedFieldNamesAsGlobal)
                {
                    newErrors.AddRange(fieldValidation.Value);
                    continue;
                    
                }
                throw new InternalServerAPIException($"Found unexpected field key \"{fieldValidation.Key}\" while showing validation error. Expected fields are {string.Join(", ", fields.Keys)}.");
            }

            foreach (var error in newErrors)
            {
                // show label
                if (globalErrorLabel == null)
                {
                    Debug.LogWarning($"Received global error in field validation {error}, but no global errors were expected.");
                    continue;
                }
                globalErrorLabel.style.display = DisplayStyle.Flex;

                // add line to label
                string newGlobalText = globalErrorLabel.text;
                if (!string.IsNullOrEmpty(newGlobalText)) { newGlobalText += "<br>"; }
                newGlobalText += error;
                globalErrorLabel.text = newGlobalText;
            }
        }
    }
}