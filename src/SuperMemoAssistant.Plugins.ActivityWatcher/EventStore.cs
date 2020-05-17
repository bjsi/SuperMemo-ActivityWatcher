using Anotar.Serilog;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  public class EventStore : SortedDictionary<DateTime, SMEvent>
  {
    private ActivityWatcherService AWService { get; set; }
    private ActivityWatcherCfg Config => Svc<ActivityWatcherPlugin>.Plugin.Config;

    public EventStore(ActivityWatcherService AWService)
    {
      this.AWService = AWService;
    }

    public async Task AddEvent(SMEvent Event)
    {
      if (Event == null || !Event.IsValid())
        return;

      var now = DateTime.UtcNow;

      if (!this.Any())
      {
        Add(now, Event);
        return;
      }

      var lastElement = this.Values.Last();
      var lastTimestamp = this.Keys.Last();

      if ((lastElement.element.Id != Event.element.Id)
        || (now - lastTimestamp).TotalSeconds > Config.Pulsetime)
      {
        // Send Backlog
        await CombineAndSend();
        // New Store with new event
        Clear();
      }

      SMEvent _;
      if (!this.TryGetValue(now, out _))
      {
        Add(now, Event);
      }
    }

    public async Task CombineAndSend()
    {
      if (this == null || this.Count < 2)
        return;

      if (!CheckIsValid())
      {
        Clear();
        LogTo.Error($"Failed to send the event backlog");
      }

      var firstEvent = this.First();
      var lastEvent = this.Last();

      IElement firstElement = firstEvent.Value.element;
      IElement lastElement = lastEvent.Value.element;
      DateTime start = firstEvent.Key;
      DateTime end = lastEvent.Key;
      int childrenDelta = lastEvent.Value.ChildrenCount - firstEvent.Value.ChildrenCount;
      string initialContent = firstEvent.Value.content.GetHtmlInnerText();
      string finalContent = lastEvent.Value.content.GetHtmlInnerText();
      string diff = DiffEx.CreateDiffList(initialContent, finalContent).Jsonify();

      var awEvent = new AWEvent(lastElement, start, end, diff, childrenDelta);
      await AWService.SendEvent(awEvent);
    }

    public bool CheckIsValid()
    {
      if (this == null || !this.Any())
        return false;

      bool ret = true;

      var pairwiseEvents = this.Zip(this.Skip(1), (a, b) => Tuple.Create(a, b));

      foreach (var eventPair in pairwiseEvents)
      {
        // Check timestamp
        if ((eventPair.Item2.Key - eventPair.Item1.Key).TotalSeconds > Config.Pulsetime)
          ret = false;

        // Check element id
        if (eventPair.Item1.Value.element.Id != eventPair.Item2.Value.element.Id)
          ret = false;
      }
      return ret;
    }
  }
}
