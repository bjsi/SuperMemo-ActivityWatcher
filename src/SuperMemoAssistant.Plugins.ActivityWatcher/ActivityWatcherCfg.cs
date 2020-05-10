using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forge.Forms.Annotations;
using SuperMemoAssistant.Sys.ComponentModel;
using Newtonsoft.Json;
using System.ComponentModel;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;


namespace SuperMemoAssistant.Plugins.ActivityWatcher
{
  [Form(Mode = DefaultFields.None)]
  [Title("Watcher Settings",
  IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
  "Cancel",
  IsCancel = true)]
  [DialogAction("save",
  "Save",
  IsDefault = true,
  Validates = true)]
  public class ActivityWatcherCfg : INotifyPropertyChangedEx
  {

    [Field(Name = "Host")]
    public string Host { get; set; } = "localhost";

    [Field(Name = "Port")]
    public int Port { get; set; } = 5666;

    [Field(Name = "Activity Watch Bucket Name")]
    public string BucketName { get; set; } = "aw-watcher-supermemo";

    [Field(Name = "Activity Watch Bucket Type")]
    public string BucketType { get; set; } = "learning";

    [Field(Name = "Pulsetime (seconds)")]
    public int Pulsetime { get; set; } = 40;

    [Field(Name = "Max Event Rate (seconds)")]
    public int MaxEventRate { get; set; } = 1;
      
    [JsonIgnore]
    public bool IsChanged { get; set; }

    public override string ToString()
    {
      return "ActivityWatcher";
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
