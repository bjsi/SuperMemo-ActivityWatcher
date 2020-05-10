using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  // For debugging
  public enum EventOrigin 
  { 
    Keyboard,
    Mouse,
    ElementWdw
  }

  public class SMEvent
  {
    public IElement element { get; set; }
    public int ChildrenCount { get; set; }
    public string content { get; set; }
    public EventOrigin eventOrigin { get; set; }

    public SMEvent(IElement element, string content, EventOrigin eventOrigin)
    {
      this.element = element;
      this.content = content;
      this.ChildrenCount = element.ChildrenCount;
      this.eventOrigin = eventOrigin;
    }

    public bool IsValid()
    {
      bool ret = true;

      if (element == null)
        ret = false;

      if (content == null)
        content = string.Empty;

      return ret;
    }
  }
}
