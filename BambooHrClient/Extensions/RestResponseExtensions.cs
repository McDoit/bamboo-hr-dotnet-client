﻿using RestSharp;
using System.Linq;

namespace BambooHrClient
{
    public static class RestResponseExtensions
    {
        private static readonly string _bambooHrErrorMessageHeaderName = "X-BambooHR-Error-Message";

        public static string GetBambooHrErrorMessage(this RestResponse response)
        {
            var error = response?.Headers.FirstOrDefault(x => x.Name == _bambooHrErrorMessageHeaderName);

            return error?.Value.ToString();
        }
    }
}
