using Decidir.Clients;
using Decidir.Exceptions;
using Decidir.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Decidir.Services
{
    internal class Payments : Service
    {
        private string privateApiKey;
        private string publicApiKey;
        private string validateApiKey;
        private string merchant;
        private string request_host;
        private RestClient restClientValidate;
        private RestClient restClientGetTokenBSA;
        private RestClient restClientGetCryptogram;
        Dictionary<string, string> headers;
        private string endpointInternalToken;

        public Payments(String endpoint, String endpointInternalToken, String privateApiKey, Dictionary<string, string> headers, String validateApiKey = null, String merchant = null, string request_host = null, string publicApiKey = null) : base(endpoint)
        {
            this.privateApiKey = privateApiKey;
            this.validateApiKey = validateApiKey;
            this.merchant = merchant;
            this.request_host = request_host;
            this.publicApiKey = publicApiKey;
            this.headers = headers;
            this.restClient = new RestClient(this.endpoint, this.headers, CONTENT_TYPE_APP_JSON);
            this.endpointInternalToken = endpointInternalToken;
        }

        public async Task<PaymentResponse> ExecutePaymentAsync(OfflinePayment payment)
        {
            Payment paymentCopy = payment.copyOffline();

            return await DoPaymentAsync(paymentCopy);
        }

        public async Task<PaymentResponse> ExecutePaymentAsync(Payment payment)
        {
            return await DoPaymentAsync(payment);

        }

        public async Task<PaymentResponse> InstructionThreeDSAsync(string xConsumerUsername, Instruction3dsData instruction3DsData)
        {
            return await sendInstructionThreeDSAsync(xConsumerUsername, instruction3DsData);
        }

        public async Task<CapturePaymentResponse> CapturePaymentAsync(long paymentId, long amount)
        {

            CapturePaymentResponse response = null;
            RestResponse result = await restClient.PutAsync(String.Format("payments/{0}", paymentId.ToString()), "{\"amount\": " + amount.ToString() + " }");

            if (result.StatusCode != STATUS_NOCONTENT && result.StatusCode != STATUS_OK)
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }
            else
            {
                if (!String.IsNullOrEmpty(result.Response))
                {
                    response = JsonConvert.DeserializeObject<CapturePaymentResponse>(result.Response);
                }
            }

            return response;
        }

        public async Task<GetAllPaymentsResponse> GetAllPaymentsAsync(long? offset = null, long? pageSize = null, string siteOperationId = null, string merchantId = null)
        {
            GetAllPaymentsResponse payments = null;
            string queryString = GetAllPaymentsQuery(offset, pageSize, siteOperationId, merchantId);
            RestResponse result = await restClient.GetAsync("payments", queryString);

            if (result.StatusCode == STATUS_OK && !String.IsNullOrEmpty(result.Response))
            {
                payments = JsonConvert.DeserializeObject<GetAllPaymentsResponse>(result.Response);
            }
            else
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }

            return payments;
        }

        public async Task<PaymentResponse> GetPaymentInfoAsync(long paymentId)
        {
            PaymentResponse payment = null;

            string parameter = String.Format("/{0}", paymentId.ToString());

            RestResponse result = await restClient.GetAsync("payments", String.Format("/{0}?expand=card_data", paymentId.ToString()));

            if (result.StatusCode == STATUS_OK && !String.IsNullOrEmpty(result.Response))
            {
                payment = JsonConvert.DeserializeObject<PaymentResponseExtend>(result.Response);
            }
            else
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }

            return payment;
        }

        public async Task<RefundPaymentResponse> RefundAsync(long paymentId, string refundBody)
        {
            RefundPaymentResponse refund = null;
            RestResponse result = await restClient.PostAsync(String.Format("payments/{0}/refunds", paymentId.ToString()), refundBody);

            if (result.StatusCode == STATUS_CREATED && !String.IsNullOrEmpty(result.Response))
            {
                refund = JsonConvert.DeserializeObject<RefundPaymentResponse>(result.Response);
            }
            else
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }

            return refund;
        }

        public async Task<RefundResponse> DeleteRefundAsync(long paymentId, long? refundId)
        {
            RefundResponse refund = null;
            RestResponse result = await restClient.DeleteAsync(String.Format("payments/{0}/refunds/{1}", paymentId.ToString(), refundId.ToString()));

            if (result.StatusCode == STATUS_OK && !String.IsNullOrEmpty(result.Response))
            {
                refund = JsonConvert.DeserializeObject<RefundResponse>(result.Response);
            }
            else
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ResponseException(result.StatusCode + " - " + result.Response);
            }

            return refund;
        }

        public async Task<RefundResponse> DeletePartialRefundAsync(long paymentId, long? refundId)
        {
            return await DeleteRefundAsync(paymentId, refundId);
        }

        protected async Task<PaymentResponse> sendInstructionThreeDSAsync(string xConsumerUsername, Instruction3dsData instruction3DsData)
        {
            PaymentResponse response = null;
            Model3dsResponse model3ds = null;

            this.headers.Add("X-Consumer-Username", xConsumerUsername);
            this.restClient = new RestClient(this.endpoint, headers, CONTENT_TYPE_APP_JSON);
            RestResponse result = await restClient.PostAsync("threeds/instruction", toJson(instruction3DsData));

            if (!String.IsNullOrEmpty(result.Response))
            {
                try
                {
                    response = JsonConvert.DeserializeObject<PaymentResponse>(result.Response);
                    if (response.status == STATUS_CHALLENGE_PENDING
                              || response.status == STATUS_FINGERPRINT_PENDING)
                    {
                        model3ds = JsonConvert.DeserializeObject<Model3dsResponse>
                        (result.Response);
                    }
                }
                catch (JsonReaderException j)
                {
                    ErrorResponse ErrorPaymentResponse = new ErrorResponse();
                    ErrorPaymentResponse.code = "502";
                    ErrorPaymentResponse.error_type = "Error en recepción de mensaje";
                    ErrorPaymentResponse.message = "No se pudo leer la respuesta";
                    ErrorPaymentResponse.validation_errors = null;
                    throw new PaymentResponseException(ErrorPaymentResponse.code, ErrorPaymentResponse, 502, j);
                }
            }

            if (response != null)
            {
                response.statusCode = result.StatusCode;
            }
            if (result.StatusCode == STATUS_ACCEPTED)
            {
                throw new PaymentAuth3dsResponseException(result.StatusCode + " - " + result.Response, model3ds, result.StatusCode);
            }
            else if (result.StatusCode != STATUS_CREATED)
            {
                if (isErrorResponse(result.StatusCode))
                    throw new PaymentResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response), result.StatusCode);
                else
                    throw new PaymentResponseException(result.StatusCode + " - " + result.Response, response, result.StatusCode);
            }

            return response;
        }

        protected async Task<PaymentResponse> DoPaymentAsync(Payment paymentCopy)
        {
            PaymentResponse response = null;
            Model3dsResponse model3ds = null;

            RestResponse result = await restClient.PostAsync("payments", Payment.toJson(paymentCopy));

            if (!String.IsNullOrEmpty(result.Response))
            {
                try
                {
                    if (paymentCopy.cardholder_auth_required)
                    {

                        response = JsonConvert.DeserializeObject<PaymentResponse>
                            (result.Response);

                        if (response.status == STATUS_CHALLENGE_PENDING
                               || response.status == STATUS_FINGERPRINT_PENDING)
                        {
                            model3ds = JsonConvert.DeserializeObject<Model3dsResponse>
                            (result.Response);
                        }
                    }
                    else
                    {
                        response = JsonConvert.DeserializeObject<PaymentResponse>(result.Response);

                    }
                }
                catch (JsonReaderException j)
                {
                    ErrorResponse ErrorPaymentResponse = new ErrorResponse();
                    ErrorPaymentResponse.code = "502";
                    ErrorPaymentResponse.error_type = "Error en recepción de mensaje";
                    ErrorPaymentResponse.message = "No se pudo leer la respuesta";
                    ErrorPaymentResponse.validation_errors = null;
                    throw new PaymentResponseException(ErrorPaymentResponse.code, ErrorPaymentResponse, 502, j);
                }
            }
            if (response != null)
            {

                response.statusCode = result.StatusCode;
            }

            if (result.StatusCode != STATUS_CREATED)
            {
                if (result.StatusCode == STATUS_ERROR)
                {
                    throw new PaymentResponseException(result.StatusCode + " - " + result.Response, JsonConvert.DeserializeObject<ErrorResponse>(result.Response), result.StatusCode);
                }
                else
                {

                    if (isErrorResponse(result.StatusCode))
                    {

                        throw new PaymentResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response), result.StatusCode);
                    }
                    else
                    {
                        if (paymentCopy.cardholder_auth_required)
                        {
                            if (result.StatusCode == STATUS_ACCEPTED)
                            {
                                throw new PaymentAuth3dsResponseException(result.StatusCode + " - " + result.Response, model3ds, result.StatusCode);
                            }
                            else
                            {
                                throw new PaymentResponseException(result.StatusCode + " - " + result.Response, response, result.StatusCode);
                            }
                        }
                    }

                }
            }


            return response;
        }

        public static string toJson(Object payment)
        {
            return JsonConvert.SerializeObject(payment, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        private string GetAllPaymentsQuery(long? offset, long? pageSize, string siteOperationId, string merchantId)
        {
            StringBuilder result = new StringBuilder();
            bool isNotNull = false;
            result.Append("?");

            //if (offset != null)
            //{
            //    isNotNull = true;
            //    result.Append(string.Format("{0}={1}", "offset", offset));
            //}

            //if (pageSize != null)
            //{
            //    isNotNull = true;
            //    result.Append(string.Format("{0}={1}", "pageSize", pageSize));
            //}

            //if (!String.IsNullOrEmpty(siteOperationId))
            //{
            //    isNotNull = true;
            //    result.Append(string.Format("{0}={1}", "siteOperationId", siteOperationId));
            //}

            //if (!String.IsNullOrEmpty(merchantId))
            //{
            //    isNotNull = true;
            //    result.Append(string.Format("{0}={1}", "merchantId", merchantId));
            //}

            result.Append("siteId=00170741&dateFrom=2023-12-03&dateTo=2023-12-04");
            isNotNull = true;

            if (isNotNull)
                return result.ToString();

            return String.Empty;
        }


        public async Task<ValidateResponse> DoValidateAsync(ValidateData validatePayment)
        {
            ValidateResponse response = null;

            this.headers["apikey"] = this.validateApiKey;
            this.headers.Add("X-Consumer-Username", this.merchant);

            this.restClientValidate = new RestClient(this.request_host + "/web/", headers, CONTENT_TYPE_APP_JSON);

            RestResponse result = await restClientValidate.PostAsync("validate", ValidateData.toJson(validatePayment));

            if (!String.IsNullOrEmpty(result.Response))
            {
                response = JsonConvert.DeserializeObject<ValidateResponse>(result.Response);
            }

            response.statusCode = result.StatusCode;

            if (result.StatusCode != STATUS_CREATED)
            {
                if (isErrorResponse(result.StatusCode))
                    throw new ValidateResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new ValidateResponseException(result.StatusCode + " - " + result.Response, response);
            }

            return response;
        }

        public async Task<ValidateResponse> ValidatePaymentAsync(ValidateData validateData)
        {
            return await DoValidateAsync(validateData);
        }

        public async Task<GetTokenResponse> GetTokenByCardTokenBsaAsync(CardTokenBsa card_token)
        {
            string cardTokenJson = CardTokenBsa.toJson(card_token);
            return await DoGetTokenAsync(cardTokenJson);
        }

        public async Task<GetTokenResponse> GetTokenAsync(TokenRequest token)
        {
            string cardTokenJson = TokenRequest.toJson(token);
            return await DoGetTokenAsync(cardTokenJson);
        }

        public async Task<GetInternalTokenResponse> GetInternalTokenAsync(InternalTokenRequest token)
        {
            return await DoGetInternalTokenAsync(InternalTokenRequest.toJson(token));
        }

        public async Task<GetCryptogramResponse> GetCryptogramAsync(CryptogramRequest cryptogramRequest)
        {
            return await DoGetCryptogramAsync(toJson(cryptogramRequest));
        }

        private async Task<GetInternalTokenResponse> DoGetInternalTokenAsync(string cardTokenJson)
        {
            GetInternalTokenResponse response = null;

            this.headers["apikey"] = this.publicApiKey;

            this.restClientGetTokenBSA = new RestClient(this.endpointInternalToken, this.headers, CONTENT_TYPE_APP_JSON);
            RestResponse result = await restClientGetTokenBSA.PostAsync("tokens", cardTokenJson);

            if (result.StatusCode == STATUS_CREATED)
            {
                if (!String.IsNullOrEmpty(result.Response))
                {
                    response = JsonConvert.DeserializeObject<GetInternalTokenResponse>(result.Response);
                }

            }
            else
            {
                throw new GetTokenResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorInternalTokenResponse>(result.Response), result.StatusCode);
            }

            return response;

        }

        private async Task<GetCryptogramResponse> DoGetCryptogramAsync(string cryptogramJson)
        {
            GetCryptogramResponse response = null;

            this.headers["apikey"] = this.privateApiKey;

            this.restClientGetCryptogram = new RestClient(this.endpointInternalToken, this.headers, CONTENT_TYPE_APP_JSON);
            RestResponse result = await restClientGetCryptogram.PostAsync("payments", cryptogramJson);

            if (result.StatusCode == STATUS_CREATED)
            {
                if (!String.IsNullOrEmpty(result.Response))
                {
                    response = JsonConvert.DeserializeObject<GetCryptogramResponse>(result.Response);
                }

            }
            else
            {
                throw new GetTokenResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorInternalTokenResponse>(result.Response), result.StatusCode);
            }

            return response;

        }

        private async Task<GetTokenResponse> DoGetTokenAsync(string cardTokenJson)
        {
            GetTokenResponse response = null;

            this.headers["apikey"] = this.publicApiKey;

            this.restClientGetTokenBSA = new RestClient(this.endpoint, this.headers, CONTENT_TYPE_APP_JSON);
            RestResponse result = await restClientGetTokenBSA.PostAsync("tokens", cardTokenJson);

            if (!String.IsNullOrEmpty(result.Response))
            {
                response = JsonConvert.DeserializeObject<GetTokenResponse>(result.Response);
            }

            if (result.StatusCode != STATUS_CREATED)
            {
                if (isErrorResponse(result.StatusCode))
                    throw new GetTokenResponseException(result.StatusCode.ToString(), JsonConvert.DeserializeObject<ErrorResponse>(result.Response));
                else
                    throw new GetTokenResponseException(result.StatusCode + " - " + result.Response, response);
            }

            return response;

        }
    }
}
