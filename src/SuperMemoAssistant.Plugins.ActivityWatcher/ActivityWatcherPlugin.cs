#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2019/04/22 17:20
// Modified On:  2019/04/22 20:52
// Modified By:  Alexis

#endregion



using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.Sentry;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using System.Windows;
using SuperMemoAssistant.Sys.IO.Devices;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Sys.Remoting;
using System.Threading;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{
  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  public class ActivityWatcherPlugin : SentrySMAPluginBase<ActivityWatcherPlugin>
  {
    #region Constructors


    public ActivityWatcherPlugin() : base("https://6d08a2c936c642c690dd4edad996e782@sentry.io/5171113") { }

    #endregion


    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "ActivityWatcher";
    public ActivityWatcherCfg Config { get; set; }

    private ActivityWatcherService AWService { get; set; }
    private EventStore eventStore { get; set; }
    private MouseMoveHook mouseMoveHook { get; set; }
    private SemaphoreSlim semSlim { get; set; } = new SemaphoreSlim(1, 1);
    private DateTime lastMouseEvent { get; set; } = DateTime.UtcNow;
    private DateTime lastKeyboardEvent { get; set; } = DateTime.UtcNow;


    public override bool HasSettings => true;

    #endregion

    #region Methods Impl

    private void LoadConfig()
    {
      Config = Svc.Configuration.Load<ActivityWatcherCfg>() ?? new ActivityWatcherCfg();
    }

    /// <inheritdoc />
    protected override async void PluginInit()
    {

      LoadConfig();
      AWService = new ActivityWatcherService();
      eventStore = new EventStore(AWService);
      this.mouseMoveHook = new MouseMoveHook();
      await this.AWService.CreateBucket();

      // TODO: Switch to KeyboardPressed Event when available
      Svc.KeyboardHotKey.MainCallback += new Action<HotKey>(OnKeyboardInput);

      Svc.SM.UI.ElementWdw.OnElementChanged += new ActionProxy<SMDisplayedElementChangedEventArgs>(ElementWdw_OnElementChanged);
    }

    //~ActivityWatcherPlugin()
    //{
    //  Dispose(false);
    //}

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      if (mouseMoveHook == null)
        return;
      if (!mouseMoveHook.IsActive)
        return;
      mouseMoveHook.Dispose();
    }

    private async Task SendOldElementEvent(IElement e)
    {
      // Waits indefinitely because we always want to send the old element event
      await semSlim.WaitAsync();
      try
      {
        // TODO: Change this
        var last = eventStore.OrderBy(x => x.Key).LastOrDefault().Value;
        if (last != null
          && (last.element.Id == e.Id))
        {
          string content = last.content;
          await eventStore.AddEvent(new SMEvent(e, content, EventOrigin.ElementWdw));
        }
      }
      finally
      {
        semSlim.Release();
      }
    }

    private async Task SendNewElementEvent(IElement e, string content)
    {
      // Waits indefinitely because we always want to send the new element event
      await semSlim.WaitAsync();
      try
      {
        await eventStore.AddEvent(new SMEvent(e, content, EventOrigin.ElementWdw));
      }
      finally
      {
        semSlim.Release();
      }
    }

    private async void ElementWdw_OnElementChanged(SMDisplayedElementChangedEventArgs obj)
    {

      IElement newElement = obj.NewElement;
      IElement oldElement = obj.OldElement;

      if (oldElement != null)
      {
        await SendOldElementEvent(oldElement);
      }

      if (newElement != null)
      {
        string content = GetContent();
        await SendNewElementEvent(newElement, content);
      }

      if (!mouseMoveHook.IsActive)
      {
        mouseMoveHook.Move += MouseMoveHook_Move;
        mouseMoveHook.IsActive = true;
      }
    }

    private async void MouseMoveHook_Move(object sender, MouseHookEventArgs e)
    {

      if ((DateTime.UtcNow - lastMouseEvent).TotalSeconds > Config.MaxEventRate)
      {

        // Returns immediately if semaphore is already taken
        if (!await semSlim.WaitAsync(0))
          return;

        try
        {
          IElement element = Svc.SM.UI.ElementWdw.CurrentElement;
          string content = GetContent() ?? string.Empty;
          if (element == null)
            return;
          await eventStore.AddEvent(new SMEvent(element, content, EventOrigin.Mouse));
          lastMouseEvent = DateTime.UtcNow;
        }
        finally
        {
          semSlim.Release();
        }
      }
    }

    private async void OnKeyboardInput(HotKey h)
    {

      // Returns immediately if semaphore is already taken
      if ((DateTime.UtcNow - lastKeyboardEvent).TotalSeconds > Config.MaxEventRate)
      {

        if (!await semSlim.WaitAsync(0))
          return;

        try
        {
          IElement element = Svc.SM.UI.ElementWdw.CurrentElement;
          string content = GetContent();
          if (element == null)
            return;
          await eventStore.AddEvent(new SMEvent(element, content, EventOrigin.Keyboard));
          lastKeyboardEvent = DateTime.UtcNow;
        }
        finally
        {
          semSlim.Release();
        }
      }
    }

    private string GetContent()
    {
      string ret = string.Empty;
      var htmlCtrl = Svc.SM.UI.ElementWdw.ControlGroup.GetFirstHtmlControl();
      var html = htmlCtrl?.AsHtml();
      if (html != null)
        ret = html.Text;
      return ret;
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      Application.Current.Dispatcher.Invoke(
          () =>
          {
            ConfigurationWindow.ShowAndActivate(HotKeyManager.Instance, Config);
          }
      );
    }
    #endregion
  }
}
