using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class PrerenderMiddleware
{
    private readonly RequestDelegate _next;

    public PrerenderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // List of known bot user agents
        string[] botUserAgents = {
            "googlebot", "bingbot", "yandex", "duckduckbot", "slurp", "baiduspider"
        };

        // Get the User-Agent header from the request
        var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

        // Check if the request is from a bot
        bool isBotRequest = false;
        foreach (var bot in botUserAgents)
        {
            if (userAgent.Contains(bot))
            {
                isBotRequest = true;
                break;
            }
        }

        if (isBotRequest)
        {
            var prerenderUrl = $"https://service.prerender.io/{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Prerender-Token", "zd3h20XM4geZ9DMXZbth");

                var response = await client.GetAsync(prerenderUrl);
                var content = await response.Content.ReadAsStringAsync();

                context.Response.ContentType = "text/html";
                context.Response.StatusCode = (int)response.StatusCode;
                await context.Response.WriteAsync(content);
            }
        }
        else
        {
            // If it's not a bot, continue with the normal pipeline
            await _next(context);
        }
    }
}