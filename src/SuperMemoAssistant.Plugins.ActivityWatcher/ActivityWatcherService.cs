using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;
using Anotar.Serilog;
using SuperMemoAssistant.Sys.Remoting;
using System.Threading;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  public class ActivityWatcherService
  {

    private ActivityWatcherCfg Config => Svc<ActivityWatcherPlugin>.Plugin.Config;
    private HttpClient httpClient;
    private Boolean Connected = false;

    private string BaseApiURL => $"http://{Config.Host}:{Config.Port}/api/0";
    private string BucketApiURL => $"{BaseApiURL}/buckets/{Config.BucketName}";
    private string HeartbeatApiURL => $"{BucketApiURL}/heartbeat?pulsetime={Config.Pulsetime}";
    private string EventApiURL => $"{BucketApiURL}/events";

    public ActivityWatcherService()
    {
      httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Accept.Clear();
      httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task SendEvent(AWEvent Event)
    {

      if (!this.Connected || Event == null)
        return;

      await SendHttpPostRequestAsync(this.EventApiURL, JsonConvert.SerializeObject(Event));

    }

    //public async Task SendHeartbeat(ActivityWatchEvent Event)
    //{

    //  if (!this.Connected)
    //    return;

    //  // This is the first event
    //  if (lastEvent == null)
    //  {
    //    await SendHttpPostRequestAsync(HeartbeatApiURL, JsonConvert.SerializeObject(Event));
    //  }
    //  else
    //  {
    //    if ((DateTime.UtcNow - lastEvent.timestamp).TotalSeconds > 1 && Event.data.element_id == lastEvent.data.element_id)
    //    {
    //      // Resend the last event
    //      // Allows AW Server to merge the two events into a larger event
    //      Event = lastEvent;
    //      Event.timestamp = DateTime.UtcNow;
    //      await SendHttpPostRequestAsync(HeartbeatApiURL, JsonConvert.SerializeObject(Event));
    //    }
    //    else if (Event.data.element_id != lastEvent.data.element_id)
    //    {
    //      await SendHttpPostRequestAsync(HeartbeatApiURL, JsonConvert.SerializeObject(Event));
    //    }
    //  }
    //}

    public async Task CreateBucket()
    {
      await SendHttpPostRequestAsync(BucketApiURL, JsonConvert.SerializeObject(new AWBucket()));
    }

    private async Task SendHttpPostRequestAsync(string path, string json)
    {
      using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
      {

        HttpResponseMessage responseMsg = null;

        try
        {
          responseMsg = await httpClient.PostAsync(path, content);
          int resCode = (int)responseMsg.StatusCode;

          if (resCode == 0)
          {
            Connected = false;
          }
          else if (resCode >= 100 && resCode < 300 || resCode == 304)
          {
            Connected = true;
          }
          else
          {
            Connected = false;
            Console.WriteLine($"aw-server did not accept our request with status code {resCode}");
          }
        }
        catch (Exception e)
        {
          // Log To error?
          return;
        }
        finally
        {
          responseMsg?.Dispose();
        }
      }
    }

    /// <summary>
    /// Send an HTTP Get request to the URL.
    /// </summary>
    /// <param name="url">
    /// The URL to send the request to.
    /// </param>
    /// <returns>
    /// A string representing the content of the response.
    /// </returns>
    public async Task<string> SendHttpGetRequest(string url)
    {
      HttpResponseMessage responseMsg = null;

      try
      {
        responseMsg = await httpClient.GetAsync(url);

        if (responseMsg.IsSuccessStatusCode)
        {
          return await responseMsg.Content.ReadAsStringAsync();
        }
        else
        {
          return null;
        }
      }
      catch (HttpRequestException)
      {
        if (responseMsg != null && responseMsg.StatusCode == System.Net.HttpStatusCode.NotFound)
          return null;
        else
          throw;
      }
      catch (OperationCanceledException)
      {
        return null;
      }
      finally
      {
        responseMsg?.Dispose();
      }
    }

  }
}
