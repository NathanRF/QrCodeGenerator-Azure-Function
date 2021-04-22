using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;
using Utf8Json;

namespace QrCodeGenerator_Azure_Function
{
    public static class QRGenerator
    {
        [FunctionName("QRGenerator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HTTP triggered function processed a request.");

            string url = req.Query["url"];

            dynamic data = await JsonSerializer.DeserializeAsync<dynamic>(req.Body);
            url = url ?? data?.url;
            if (string.IsNullOrEmpty(url))
            {
                return new BadRequestResult();
            }
            var isAbsoluteUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri resultUrl);
            if (!isAbsoluteUrl)
            {
                return new BadRequestResult();
            }

            var generator = new Url(resultUrl.AbsoluteUri);
            var payload = generator.ToString();

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeAsPng = qrCode.GetGraphic(20);
                return new FileContentResult(qrCodeAsPng, "image/png");
            }
        }
    }
}

