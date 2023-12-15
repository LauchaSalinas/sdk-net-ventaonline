using Decidir.Clients;
using Decidir.Model;
using Decidir.Exceptions;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Decidir.Services
{
    internal class HealthCheck : Service
    {
        public HealthCheck(string endpoint, Dictionary<string, string> headers) : base(endpoint)
        {
            this.restClient = new RestClient(this.endpoint, headers, CONTENT_TYPE_APP_JSON);
        }

        public async Task<HealthCheckResponse> ExecuteAsync()
        {
            await Task.Delay(100);
            HealthCheckResponse response = new HealthCheckResponse();
            RestResponse result = await restClient.GetAsync("healthcheck", "");

            if (result.StatusCode == STATUS_OK && !String.IsNullOrEmpty(result.Response))
            {
                response = HealthCheckResponse.toHealthCheckResponse(result.Response);
            }
            else
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }

            return response;
        }
    }
}
