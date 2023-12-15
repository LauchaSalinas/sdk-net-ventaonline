using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Decidir.Constants;
using Decidir.Exceptions;
using Decidir.Model;
using Decidir.Services;
using Newtonsoft.Json;

namespace Decidir
{
    public class DecidirConnector
    {
        #region Constants
        public const string versionDecidir = "1.4.8";

        private const string request_host_sandbox = "https://developers.decidir.com";
        private const string request_host_production = "https://live.decidir.com";
        private const string request_host_qa = "https://qa.decidir.com";
        private const string request_path_payments = "/api/v2/";
        private const string request_path_validate = "/web/";
        private const string request_path_closureQA = "/api/v1/";
        private const string request_path_internal_token = "/api/v1/transaction_gateway/";



        private const string endPointSandbox = request_host_sandbox + request_path_payments; // https://developers.decidir.com/api/v2/;
        private const string endPointProduction = request_host_production + request_path_payments; //https://live.decidir.com/api/v2/;
        private const string endPointQA = request_host_qa + request_path_payments; //https://qa.decidir.com/api/v2/;
        private const string endPointQAClosure = request_host_qa + request_path_closureQA;

        private const string endPointInternalTokenSandbox = request_host_sandbox + request_path_internal_token;
        private const string endPointInternalTokenProduction = request_host_production + request_path_internal_token;
        private const string endPointInternalTokenQA = request_host_qa + request_path_internal_token;

        private const string emptyObject = "{}";

        #endregion

        private string privateApiKey;
        private string publicApiKey;
        private string endpoint;
        private string request_host;

        private string validateApiKey;
        private string merchant;
        private string grouper;
        private string developer;

        private string endPointInternalToken;

        private HealthCheck healthCheckService;
        private Payments paymentService;
        private UserSite userSiteService;
        private CardTokens cardTokensService;
        private BatchClosure bathClosureService;

        private Dictionary<string, string> headers;

        public DecidirConnector(int ambiente, string privateApiKey, string publicApiKey, string validateApiKey = null, string merchant = null, string grouper = "", string developer = "")
        {
            init(ambiente, privateApiKey, publicApiKey, validateApiKey, merchant, grouper, developer);
        }

        public DecidirConnector(string request_host, string request_path, string privateApiKey, string publicApiKey, string validateApiKey = null, string merchant = null, string grouper = "", string developer = "")
        {
            this.request_host = request_host;
            endpoint = request_host + request_path;
            init(-1, privateApiKey, publicApiKey, validateApiKey, merchant, grouper, developer);
        }

        private void init(int ambiente, string privateApiKey, string publicApiKey, string validateApiKey, string merchant, string grouper, string developer)
        {
            this.privateApiKey = privateApiKey;
            this.publicApiKey = publicApiKey;
            this.validateApiKey = validateApiKey;
            this.merchant = merchant;
            this.grouper = grouper;
            this.developer = developer;

            headers = new Dictionary<string, string>();
            headers.Add("apikey", privateApiKey);
            headers.Add("Cache-Control", "no-cache");
            headers.Add("X-Source", getXSource(grouper, developer));

            bathClosureService = new BatchClosure(endpoint, privateApiKey, validateApiKey, merchant, request_host, publicApiKey);

            if (ambiente == Ambiente.AMBIENTE_PRODUCCION)
            {
                endpoint = endPointProduction;
                request_host = request_host_production;
                endPointInternalToken = endPointInternalTokenProduction;
            }
            else if (ambiente == Ambiente.AMBIENTE_QA)
            {
                endpoint = endPointQA;
                request_host = request_host_qa;
                endPointInternalToken = endPointInternalTokenQA;
                bathClosureService = new BatchClosure(endPointQAClosure, privateApiKey, validateApiKey, merchant, request_host, publicApiKey);
            }
            else if (ambiente == Ambiente.AMBIENTE_SANDBOX)
            {
                endpoint = endPointSandbox;
                request_host = request_host_sandbox;
                endPointInternalToken = endPointInternalTokenSandbox;
            }


            healthCheckService = new HealthCheck(endpoint, headers);
            paymentService = new Payments(endpoint, endPointInternalToken, privateApiKey, headers, validateApiKey, merchant, request_host, publicApiKey);
            userSiteService = new UserSite(endpoint, privateApiKey, headers);
            cardTokensService = new CardTokens(endpoint, privateApiKey, headers);

        }

        public HealthCheckResponse HealthCheck()
        {
            return healthCheckService.ExecuteAsync().GetAwaiter().GetResult();
        }

        public async Task<HealthCheckResponse> HealthCheckAsync()
        {
            return await healthCheckService.ExecuteAsync();
        }

        public PaymentResponse Payment(Payment payment)
        {
            return paymentService.ExecutePaymentAsync(payment).GetAwaiter().GetResult();
        }

        public async Task<PaymentResponse> PaymentAsync(Payment payment)
        {
            return await paymentService.ExecutePaymentAsync(payment);
        }

        public async Task<GetCryptogramResponse> CryptogramAsync(CryptogramRequest cryptogramRequest)
        {
            return await paymentService.GetCryptogramAsync(cryptogramRequest);
        }

        public async Task<PaymentResponse> PaymentAsync(OfflinePayment payment)
        {
            return await paymentService.ExecutePaymentAsync(payment);
        }

        public CapturePaymentResponse CapturePayment(long paymentId, long amount)
        {
            return paymentService.CapturePaymentAsync(paymentId, amount).GetAwaiter().GetResult();
        }

        public async Task<CapturePaymentResponse> CapturePaymentAsync(long paymentId, long amount)
        {
            return await paymentService.CapturePaymentAsync(paymentId, amount);
        }

        public GetAllPaymentsResponse GetAllPayments(long? offset = null, long? pageSize = null, string siteOperationId = null, string merchantId = null)
        {
            return paymentService.GetAllPaymentsAsync(offset, pageSize, siteOperationId, merchantId).GetAwaiter().GetResult();
        }

        public async Task<GetAllPaymentsResponse> GetAllPaymentsAsync(long? offset = null, long? pageSize = null, string siteOperationId = null, string merchantId = null)
        {
            return await paymentService.GetAllPaymentsAsync(offset, pageSize, siteOperationId, merchantId);
        }

        public PaymentResponse GetPaymentInfo(long paymentId)
        {
            return paymentService.GetPaymentInfoAsync(paymentId).GetAwaiter().GetResult();
        }

        public async Task<PaymentResponse> GetPaymentInfoAsync(long paymentId)
        {
            return await paymentService.GetPaymentInfoAsync(paymentId);
        }

        public RefundPaymentResponse Refund(long paymentId)
        {
            return paymentService.RefundAsync(paymentId, emptyObject).GetAwaiter().GetResult();
        }

        public async Task<RefundPaymentResponse> RefundAsync(long paymentId)
        {
            return await paymentService.RefundAsync(paymentId, emptyObject);
        }

        public RefundPaymentResponse RefundSubPayment(long paymentId, RefundSubPaymentRequest refundSubPaymentRequest)
        {
            return paymentService.RefundAsync(paymentId, ObjectToJson(refundSubPaymentRequest)).GetAwaiter().GetResult();
        }

        public async Task<RefundPaymentResponse> RefundSubPaymentAsync(long paymentId, RefundSubPaymentRequest refundSubPaymentRequest)
        {
            return await paymentService.RefundAsync(paymentId, ObjectToJson(refundSubPaymentRequest));
        }

        public BatchClosureResponse BatchClosure(string batchClosure)
        {
            return bathClosureService.BatchClosureActiveAsync(batchClosure).GetAwaiter().GetResult();
        }

        public async Task<BatchClosureResponse> BatchClosureAsync(string batchClosure)
        {
            return await bathClosureService.BatchClosureActiveAsync(batchClosure);
        }

        public RefundResponse DeleteRefund(long paymentId, long? refundId)
        {
            return paymentService.DeleteRefundAsync(paymentId, refundId).GetAwaiter().GetResult();
        }

        public async Task<RefundResponse> DeleteRefundAsync(long paymentId, long? refundId)
        {
            return await paymentService.DeleteRefundAsync(paymentId, refundId);
        }

        public async Task<RefundPaymentResponse> PartialRefundAsync(long paymentId, double amount)
        {
            RefundAmount partialRefund = new RefundAmount();

            try
            {
                partialRefund.amount = Convert.ToInt64(amount * 100);
            }
            catch (Exception ex)
            {
                throw new ResponseException(ex.Message);
            }
            return await paymentService.RefundAsync(paymentId, ObjectToJson(partialRefund));
        }

        public RefundPaymentResponse PartialRefund(long paymentId, RefundAmount amount)
        {
            return paymentService.RefundAsync(paymentId, ObjectToJson(amount)).GetAwaiter().GetResult();
        }

        public async Task<RefundPaymentResponse> PartialRefundAsync(long paymentId, RefundAmount amount)
        {
            return await paymentService.RefundAsync(paymentId, ObjectToJson(amount));
        }

        public RefundResponse DeletePartialRefund(long paymentId, long? refundId)
        {
            return paymentService.DeletePartialRefundAsync(paymentId, refundId).GetAwaiter().GetResult();
        }

        public async Task<RefundResponse> DeletePartialRefundAsync(long paymentId, long? refundId)
        {
            return await paymentService.DeletePartialRefundAsync(paymentId, refundId);
        }

        public GetAllCardTokensResponse GetAllCardTokens(string userId)
        {
            return userSiteService.GetAllTokensAsync(userId).GetAwaiter().GetResult();
        }
        public async Task<GetAllCardTokensResponse> GetAllCardTokensAsync(string userId)
        {
            return await userSiteService.GetAllTokensAsync(userId);
        }

        public bool DeleteCardToken(string token)
        {
            return cardTokensService.DeleteCardTokenAsync(token).GetAwaiter().GetResult();
        }

        public async Task<bool> DeleteCardTokenAsync(string token)
        {
            return await cardTokensService.DeleteCardTokenAsync(token);
        }

        public ValidateResponse Validate(ValidateData validateData)
        {
            return paymentService.ValidatePaymentAsync(validateData).GetAwaiter().GetResult();
        }

        public async Task<ValidateResponse> ValidateAsync(ValidateData validateData)
        {
            return await paymentService.ValidatePaymentAsync(validateData);
        }

        public GetTokenResponse GetTokenByCardTokenBsa(CardTokenBsa card_token_bsa)
        {
            return paymentService.GetTokenByCardTokenBsaAsync(card_token_bsa).GetAwaiter().GetResult();
        }

        public async Task<GetTokenResponse> GetTokenByCardTokenBsaAsync(CardTokenBsa card_token_bsa)
        {
            return await paymentService.GetTokenByCardTokenBsaAsync(card_token_bsa);
        }

        public GetTokenResponse GetToken(TokenRequest token)
        {
            return paymentService.GetTokenAsync(token).GetAwaiter().GetResult();
        }

        public async Task<GetTokenResponse> GetTokenAsync(TokenRequest token)
        {
            return await paymentService.GetTokenAsync(token);
        }

        public GetInternalTokenResponse GetInternalToken(InternalTokenRequest token)
        {
            return paymentService.GetInternalTokenAsync(token).GetAwaiter().GetResult();
        }

        public async Task<GetInternalTokenResponse> GetInternalTokenAsync(InternalTokenRequest token)
        {
            return await paymentService.GetInternalTokenAsync(token);
        }

        public PaymentResponse InstructionThreeDS(string xConsumerUsername, Instruction3dsData instruction3DsData)
        {
            return paymentService.InstructionThreeDSAsync(xConsumerUsername, instruction3DsData).GetAwaiter().GetResult();
        }

        public async Task<PaymentResponse> InstructionThreeDSAsync(string xConsumerUsername, Instruction3dsData instruction3DsData)
        {
            return await paymentService.InstructionThreeDSAsync(xConsumerUsername, instruction3DsData);
        }

        private string getXSource(String grouper, String developer)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("service", "SDK-NET");
            header.Add("grouper", grouper);
            header.Add("developer", developer);

            String headerJson = JsonConvert.SerializeObject(header, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            byte[] headerJsonBytes = System.Text.Encoding.UTF8.GetBytes(headerJson);

            return System.Convert.ToBase64String(headerJsonBytes);
        }

        private String ObjectToJson(Object obj)
        {
            return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None);
        }

    }
}
