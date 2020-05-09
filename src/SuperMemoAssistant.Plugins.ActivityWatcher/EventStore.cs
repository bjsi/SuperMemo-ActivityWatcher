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
  public class EventStore : ConcurrentDictionary<DateTime, SMEvent>
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

      if (IsEmpty)
      {
        TryAdd(now, Event);
        return;
      }

      var orderedEvents = this.OrderBy(x => x.Key);
      var lastEvent = orderedEvents.Last();

      if ((lastEvent.Value.element.Id != Event.element.Id)
        || (now - lastEvent.Key).TotalSeconds > Config.Pulsetime)
      {
        // Send Backlog
        await CombineAndSend(orderedEvents);
        // New Store with new event
        Clear();
      }
      TryAdd(now, Event);
    }

    public async Task CombineAndSend(IOrderedEnumerable<KeyValuePair<DateTime, SMEvent>> events)
    {
      if (events == null || events.Count() < 2)
        return;

      if (!CheckIsValid(events)) 
      {
        var list = this.OrderBy(x => x.Key).ToList();
        LogTo.Error($"Failed to send the event backlog");
      }

      var firstEvent = events.First();
      var lastEvent = events.Last();

      IElement firstElement = firstEvent.Value.element;
      IElement lastElement = lastEvent.Value.element;


      var x = firstElement.Deleted;
      var x1 = firstElement.ChildrenCount;

      var y = lastElement.Deleted;
      var y2 = lastElement.ChildrenCount;

      var template = Svc.SM.Registry.Template[5];
      var t = template.Name;

      DateTime start = firstEvent.Key;
      DateTime end = lastEvent.Key;


      string initialContent = firstEvent.Value.content
                                              .GetHtmlInnerText();
      string finalContent = lastEvent.Value.content
                                           .GetHtmlInnerText();

      string diff = DiffEx.CreateDiffList(initialContent, finalContent).Jsonify();

      var awEvent = new AWEvent(firstElement, start, end, diff);
      await AWService.SendEvent(awEvent);
    }

    public bool CheckIsValid(IOrderedEnumerable<KeyValuePair<DateTime, SMEvent>> events)
    {
      if (events == null || events.Count() == 0)
        return false;

      bool ret = true;

      var pairwiseEvents = events.Zip(events.Skip(1), (a, b) => Tuple.Create(a, b));

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
