using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;
using Newtonsoft.Json;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  public static class DiffEx
  {
    public static Dictionary<Operation, int> operationMap = new Dictionary<Operation, int>()
    {
        { Operation.Delete,  -1 },
        { Operation.Equal,    0},
        { Operation.Insert,   1 },
    };

    public static string Serialize(this List<Diff> diff)
    {
      var res = new List<string>();

      if (diff != null && diff.Count > 0)
      {
        foreach (var dif in diff)
        {
          res.Add($"({operationMap[dif.Operation]}, '{dif.Text}')");
        }
      }
      return  "[" + string.Join(", ", res) + "]";
    }

    public static List<Diff> CreateDiffList(string text1, string text2)
    {
      var dmp = DiffMatchPatchModule.Default;
      List<Diff> diff = dmp.DiffMain(text1, text2);
      dmp.DiffCleanupSemantic(diff);
      return diff;
    }

    public static string GetHtmlInnerText(this string html)
    {
      string ret = string.Empty;
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        ret = doc.DocumentNode.InnerText;
      }
      return ret;
    }

    public static string Jsonify(this List<Diff> diffs)
    {
      string ret = string.Empty;
      if (diffs != null && diffs.Count > 0)
      {
        var lis = new List<Dictionary<int, string>>();
        foreach(var dif in diffs)
        {
          var dic = new Dictionary<int, string>();
          dic.Add(operationMap[dif.Operation], dif.Text);
          lis.Add(dic);
        }
        ret = JsonConvert.SerializeObject(lis);
      }
      return ret;
    }
  }
}
