using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Lambda
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var port = 5050;
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            Console.WriteLine($"✅ Server listening on: http://localhost:{port}/");
            Console.WriteLine("⏳ Waiting for requests... (Press Ctrl+C to stop)\n");

            var function = new Function();

            while (true)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(async () => await HandleRequest(context, function));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Server Error: {ex.Message}");
                }
            }
        }

        static async Task HandleRequest(HttpListenerContext context, Function function)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                Console.WriteLine($"\n🔔 New Request - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Method: {request.HttpMethod}");
                Console.WriteLine($"   URL: {request.Url?.PathAndQuery}");

                if (request.HttpMethod != "POST")
                {
                    await SendResponse(response, 405, new { error = "Only POST method allowed" });
                    Console.WriteLine("   ❌ 405 - Method Not Allowed");
                    return;
                }

                string body = "";
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                Console.WriteLine($"   Body: {body}");

                var lambdaRequest = JsonConvert.DeserializeObject<Function.CreateTransactionRequest>(body);
                
                if (lambdaRequest == null)
                {
                    await SendResponse(response, 400, new { error = "Invalid JSON" });
                    Console.WriteLine("   ❌ 400 - Invalid JSON");
                    return;
                }

                // Invocar Lambda handler
                var result = await function.Handler(lambdaRequest, null!);

                await SendResponse(response, 200, result);
                Console.WriteLine("   ✅ 200 - Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                await SendResponse(response, 500, new { 
                    error = ex.Message, 
                    type = ex.GetType().Name 
                });
            }
        }

        static async Task SendResponse(HttpListenerResponse response, int statusCode, object data)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var bytes = Encoding.UTF8.GetBytes(json);
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            finally
            {
                response.Close();
            }
        }
    }
}
