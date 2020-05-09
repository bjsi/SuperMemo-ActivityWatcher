using Newtonsoft.Json;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{
  [Serializable]
  public class AWBucket
  {
    [JsonIgnore]
    private ActivityWatcherCfg Config => Svc<ActivityWatcherPlugin>.Plugin.Config;

    public string name => Config.BucketName;
    public string hostname => Dns.GetHostName();
    public string client => Config.BucketName;
    public string type => Config.BucketType;
  }
}
