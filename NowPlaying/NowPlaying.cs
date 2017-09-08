﻿using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sync;
using Sync.Command;
using Sync.MessageFilter;
using System.Threading.Tasks;
using static Sync.Plugins.PluginEvents;
namespace NowPlaying
{
    public class NowPlaying : Plugin, IFilter, ISourceDanmaku
    {
        private MessageDispatcher MainMessager = null;
        private MSNHandler handler = null;
        private OSUStatus osuStat = new OSUStatus();

        public NowPlaying() : base("Now Playing", "Deliay")
        {
        }

        public override void OnEnable()
        {
            base.EventBus.BindEvent<InitFilterEvent>((filter) => filter.Filters.AddFilter(this));
            base.EventBus.BindEvent<LoadCompleteEvent>(evt => MainMessager = evt.Host.Messages);
            handler = new MSNHandler();

            Sync.Tools.IO.CurrentIO.WriteColor(Name + " By " + Author, ConsoleColor.DarkCyan);
            //绑定NowPlayingEvents这个Dispatcher的StatusChangeEvent事件
            NowPlayingEvents.Instance.BindEvent<StatusChangeEvent>(OnOSUStatusChange);
        }

        private void OnOSUStatusChange(StatusChangeEvent @event)
        {
            osuStat = @event.CurrentStatus;
#if (DEBUG)
            Sync.Tools.IO.CurrentIO.WriteColor(osuStat.status + " " + osuStat.artist + " - " + osuStat.title, ConsoleColor.DarkCyan);
#endif
        }

        public void onMsg(ref IMessageBase msg)
        {
            if (msg.Message.RawText.Equals("?np"))
            {
                msg.Cancel = true;
                string strMsg = string.Empty;
                if (osuStat.status == "Playing")
                {
                    strMsg = "玩";
                }
                else if (osuStat.status == "Editing")
                {
                    strMsg = "做";
                }
                else //include  Listening
                {
                    strMsg = "听";
                }
                if (osuStat.title.Length > 17)
                {
                    MainMessager.onIRC(SyncHost.Instance.ClientWrapper.Client.NickName, "我在" + strMsg + osuStat.title.Substring(0, 14) + "...");
                }
                else
                {
                    MainMessager.onIRC(SyncHost.Instance.ClientWrapper.Client.NickName, "我在" + strMsg + osuStat.title);
                }
            }

        }

        [Obsolete("Replace with EventBus", true)]
        public void registerCallback(Func<IOSUStatus, Task<bool>> callback)
        {
            ((IMSNHandler)handler).registerCallback(callback);
        }
    }
}
