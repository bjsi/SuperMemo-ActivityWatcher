using Anotar.Serilog;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Interop.SuperMemo.Learning;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  [Serializable]
  public class PathEntry
  {
    public string element_title { get; set; }
    public int element_id { get; set; }

    public PathEntry(IElement element)
    {
      this.element_id = element.Id;
      this.element_title = element.Title;
    }
  }

  [Serializable]
  public class AWEvent
  {

    public DateTime timestamp { get; set; }
    public double duration { get; set; }
    public AWData data { get; set; }

    public AWEvent(IElement element, DateTime start, DateTime end, string diffedContent, int childrenDelta)
    {

      if (start > end)
      {
        LogTo.Error($"Failed to create AWEvent - start time is after the end");
        return;
      }

      if (element == null)
        return;

      duration = (end - start).TotalSeconds;
      timestamp = start;
      data = new AWData(element, diffedContent, childrenDelta);
    }
  }

  [Serializable]
  public class AWData
  {

    public int element_id { get; set; }
    public string collection_name { get; set; }
    public string element_title { get; set; }
    public string learning_mode { get; set; }
    public int concept_id { get; set; }
    public string concept_name { get; set; }
    public string element_type { get; set; }
    public List<PathEntry> full_path { get; set; }
    public List<PathEntry> category_path { get; set; }
    public string element_content { get; set; }
    public bool deleted { get; set; }
    public int children_delta { get; set; }

    // TODO: add template and multiple html content components
    // TODO: add element_status
    // TODO: add element_priority
    // TODO: add whether extracts were created / how many
    // --> Currently only indirectly tracked via children
    // TODO: add whether a grade was given to an item

    public AWData(IElement element, string content, int childrenDelta)
    {
      this.element_id = element.Id;
      this.element_title = string.IsNullOrEmpty(element.Title) ? "None" : element.Title;
      this.learning_mode = ((LearningMode)Svc.SM.UI.ElementWdw.CurrentLearningMode).ToString();
      this.concept_id = element.Concept == null ? -1 : element.Concept.Id;
      this.concept_name = element.Concept == null ? "None" : element.Concept.Name;
      this.element_type = ((ElementType)element.Type).ToString();
      this.collection_name = Svc.SM.Collection.Name;
      this.category_path = this.GetCategoryPath(element);
      this.full_path = this.GetFullPath(element);
      this.element_content = content;
      this.deleted = element.Deleted;
      this.children_delta = childrenDelta;
    }

    private List<PathEntry> GetCategoryPath(IElement element)
    {
      var fullPath = new List<PathEntry>();
      var cur = element.Parent;
      while (cur != null)
      {
        if (cur.Type == ElementType.ConceptGroup)
          fullPath.Add(new PathEntry(cur));
        cur = cur.Parent;
      }
      return fullPath;
    }


    private List<PathEntry> GetFullPath(IElement element)
    {
      var fullPath = new List<PathEntry>();
      var cur = element.Parent;
      while (cur != null)
      {
        fullPath.Add(new PathEntry(cur));
        cur = cur.Parent;
      }
      return fullPath;
    }
  }
}
