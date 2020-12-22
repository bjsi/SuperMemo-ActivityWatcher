using mshtml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{
  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.AutoDispatch)]
  public class OnHtmlDocChanged
  {
    [DispId(0)]
    public void handler(IHTMLEventObj e)
    {
      string c = e.type;
    }
  }
}
