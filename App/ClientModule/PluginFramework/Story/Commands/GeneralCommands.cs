﻿using System;
using System.Collections;
using System.Collections.Generic;
using StorySystem;
using GameFramework;
using GameFramework.Skill;
using GameFrameworkMessage;

namespace GameFramework.Story.Commands
{
    /// <summary>
    /// preload(objresid1,objresid2,...);
    /// </summary>
    internal class PreloadCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PreloadCommand cmd = new PreloadCommand();
            for (int i = 0; i < m_ObjResIds.Count; i++) {
                cmd.m_ObjResIds.Add(m_ObjResIds[i].Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_ObjResIds.Count; i++) {
                m_ObjResIds[i].Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            List<int> resIds = new List<int>();
            for (int i = 0; i < m_ObjResIds.Count; i++) {
                int resId = m_ObjResIds[i].Value;
                resIds.Add(resId);
                PreloadActorAndSkills(resId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<int> val = new StoryValue<int>();
                val.InitFromDsl(callData.GetParam(i));
                m_ObjResIds.Add(val);
            }
        }

        private void PreloadActorAndSkills(int resId)
        {
            TableConfig.Actor cfg = TableConfig.ActorProvider.Instance.GetActor(resId);
            if (null != cfg) {
                ResourceSystem.Instance.PreloadObject(cfg.avatar);
                int[] skillIds = new int[] { cfg.skill0, cfg.skill1, cfg.skill2, cfg.skill3, cfg.skill4, cfg.skill5, cfg.skill6, cfg.skill7, cfg.skill8, cfg.bornskill, cfg.deadskill };
                for (int ix = 0; ix < skillIds.Length; ++ix) {
                    int skillId = skillIds[ix];
                    if (skillId > 0) {
                        GfxSkillSystem.Instance.PreloadSkillInstance(skillId);
                        TableConfig.Skill skillCfg = TableConfig.SkillProvider.Instance.GetSkill(skillId);
                        if (null != skillCfg) {
                            foreach (var pair in skillCfg.resources) {
                                ResourceSystem.Instance.PreloadObject(pair.Value);
                            }
                        }
                    }
                }
            }
        }

        private List<IStoryValue<int>> m_ObjResIds = new List<IStoryValue<int>>();
    }
    /// <summary>
    /// startstory(story_id);
    /// </summary>
    internal class StartStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            StartStoryCommand cmd = new StartStoryCommand();
            cmd.m_StoryId = m_StoryId.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_StoryId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            PluginFramework.Instance.QueueAction(GfxStorySystem.Instance.StartStory, m_StoryId.Value);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_StoryId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<string> m_StoryId = new StoryValue<string>();
    }
    /// <summary>
    /// stopstory(story_id);
    /// </summary>
    internal class StopStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            StopStoryCommand cmd = new StopStoryCommand();
            cmd.m_StoryId = m_StoryId.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_StoryId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            GfxStorySystem.Instance.MarkStoryTerminated(m_StoryId.Value);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_StoryId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<string> m_StoryId = new StoryValue<string>();
    }
    /// <summary>
    /// waitstory(storyid1,storyid2,...)[set(var,val)timeoutset(timeout,var,val)];
    /// </summary>
    internal class WaitStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            WaitStoryCommand cmd = new WaitStoryCommand();
            for (int i = 0; i < m_StoryIds.Count; i++) {
                cmd.m_StoryIds.Add(m_StoryIds[i].Clone());
            }
            cmd.m_SetVar = m_SetVar.Clone();
            cmd.m_SetVal = m_SetVal.Clone();
            cmd.m_TimeoutVal = m_TimeoutVal.Clone();
            cmd.m_TimeoutSetVar = m_TimeoutSetVar.Clone();
            cmd.m_TimeoutSetVal = m_TimeoutSetVal.Clone();
            cmd.m_HaveSet = m_HaveSet;
            return cmd;
        }

        protected override void ResetState()
        {
            m_CurTime = 0;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_StoryIds.Count; i++) {
                m_StoryIds[i].Evaluate(instance, iterator, args);
            }
            if (m_HaveSet) {
                m_SetVar.Evaluate(instance, iterator, args);
                m_SetVal.Evaluate(instance, iterator, args);
                m_TimeoutVal.Evaluate(instance, iterator, args);
                m_TimeoutSetVar.Evaluate(instance, iterator, args);
                m_TimeoutSetVal.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int ct = 0;
            for (int i = 0; i < m_StoryIds.Count; i++) {
                ct += GfxStorySystem.Instance.CountStory(m_StoryIds[i].Value);
            }
            bool ret = false;
            if (ct <= 0) {
                string varName = m_SetVar.Value;
                object varVal = m_SetVal.Value;
                instance.SetVariable(varName, varVal);
            } else {
                int timeout = m_TimeoutVal.Value;
                int curTime = m_CurTime;
                m_CurTime += (int)delta;
                if (timeout <= 0 || curTime <= timeout) {
                    ret = true;
                } else {
                    string varName = m_TimeoutSetVar.Value;
                    object varVal = m_TimeoutSetVal.Value;
                    instance.SetVariable(varName, varVal);
                }
            }
            return ret;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_StoryIds.Add(val);
            }
        }

        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count >= 3) {
                Dsl.CallData first = statementData.Functions[0].Call;
                Dsl.CallData second = statementData.Functions[1].Call;
                Dsl.CallData third = statementData.Functions[2].Call;
                if (null != first && null != second && null != third) {
                    m_HaveSet = true;

                    Load(first);
                    LoadSet(second);
                    LoadTimeoutSet(third);
                }
            }
        }

        private void LoadSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 2) {
                m_SetVar.InitFromDsl(callData.GetParam(0));
                m_SetVal.InitFromDsl(callData.GetParam(1));
            }
        }

        private void LoadTimeoutSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 3) {
                m_TimeoutVal.InitFromDsl(callData.GetParam(0));
                m_TimeoutSetVar.InitFromDsl(callData.GetParam(1));
                m_TimeoutSetVal.InitFromDsl(callData.GetParam(2));
            }
        }

        private List<IStoryValue<string>> m_StoryIds = new List<IStoryValue<string>>();
        private IStoryValue<string> m_SetVar = new StoryValue<string>();
        private IStoryValue<object> m_SetVal = new StoryValue();
        private IStoryValue<int> m_TimeoutVal = new StoryValue<int>();
        private IStoryValue<string> m_TimeoutSetVar = new StoryValue<string>();
        private IStoryValue<object> m_TimeoutSetVal = new StoryValue();
        private bool m_HaveSet = false;
        private int m_CurTime = 0;
    }
    /// <summary>
    /// pausestory(storyid1,storyid2,...);
    /// </summary>
    internal class PauseStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PauseStoryCommand cmd = new PauseStoryCommand();
            for (int i = 0; i < m_StoryIds.Count; i++) {
                cmd.m_StoryIds.Add(m_StoryIds[i].Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_StoryIds.Count; i++) {
                m_StoryIds[i].Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            for (int i = 0; i < m_StoryIds.Count; i++) {
                GfxStorySystem.Instance.PauseStory(m_StoryIds[i].Value, true);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_StoryIds.Add(val);
            }
        }

        private List<IStoryValue<string>> m_StoryIds = new List<IStoryValue<string>>();
    }
    /// <summary>
    /// resumestory(storyid1,storyid2,...);
    /// </summary>
    internal class ResumeStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ResumeStoryCommand cmd = new ResumeStoryCommand();
            for (int i = 0; i < m_StoryIds.Count; i++) {
                cmd.m_StoryIds.Add(m_StoryIds[i].Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_StoryIds.Count; i++) {
                m_StoryIds[i].Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            for (int i = 0; i < m_StoryIds.Count; i++) {
                GfxStorySystem.Instance.PauseStory(m_StoryIds[i].Value, false);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_StoryIds.Add(val);
            }
        }

        private List<IStoryValue<string>> m_StoryIds = new List<IStoryValue<string>>();
    }
    /// <summary>
    /// firemessage(msgid,arg1,arg2,...);
    /// </summary>
    internal class FireMessageCommand : AbstractStoryCommand
    {
        public FireMessageCommand(bool isConcurrent)
        {
            m_IsConcurrent = isConcurrent;
        }
        public override IStoryCommand Clone()
        {
            FireMessageCommand cmd = new FireMessageCommand(m_IsConcurrent);
            cmd.m_MsgId = m_MsgId.Clone();
            for (int i = 0; i < m_MsgArgs.Count; ++i) {
                IStoryValue<object> val = m_MsgArgs[i];
                cmd.m_MsgArgs.Add(val.Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_MsgId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_MsgArgs.Count; ++i) {
                IStoryValue<object> val = m_MsgArgs[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string msgId = m_MsgId.Value;
            ArrayList arglist = new ArrayList();
            for (int i = 0; i < m_MsgArgs.Count; ++i) {
                IStoryValue<object> val = m_MsgArgs[i];
                arglist.Add(val.Value);
            }
            object[] args = arglist.ToArray();
            if(m_IsConcurrent)
                GfxStorySystem.Instance.SendConcurrentMessage(msgId, args);
            else
                GfxStorySystem.Instance.SendMessage(msgId, args);

            const string c_DialogOverPrefix = "dialog_over:";
            if (msgId.StartsWith(c_DialogOverPrefix)) {
                if (!PluginFramework.Instance.IsBattleState) {
                    GameFrameworkMessage.Msg_CR_DlgClosed msg = new GameFrameworkMessage.Msg_CR_DlgClosed();
                    msg.dialog_id = int.Parse(msgId.Substring(c_DialogOverPrefix.Length).Trim());
                    Network.NetworkSystem.Instance.SendMessage(GameFrameworkMessage.RoomMessageDefine.Msg_CR_DlgClosed, msg);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_MsgId.InitFromDsl(callData.GetParam(0));
            }
            for (int i = 1; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_MsgArgs.Add(val);
            }
        }

        private IStoryValue<string> m_MsgId = new StoryValue<string>();
        private List<IStoryValue<object>> m_MsgArgs = new List<IStoryValue<object>>();
        private bool m_IsConcurrent = false;
    }
    internal sealed class FireMessageCommandFactory : IStoryCommandFactory
    {
        public IStoryCommand Create()
        {
            return new FireMessageCommand(false);
        }
    }
    internal sealed class FireConcurrentMessageCommandFactory : IStoryCommandFactory
    {
        public IStoryCommand Create()
        {
            return new FireMessageCommand(true);
        }
    }
    /// <summary>
    /// waitallmessage(msgid1,msgid2,...)[set(var,val)timeoutset(timeout,var,val)];
    /// </summary>
    internal class WaitAllMessageCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            WaitAllMessageCommand cmd = new WaitAllMessageCommand();
            for (int i = 0; i < m_MsgIds.Count; i++) {
                cmd.m_MsgIds.Add(m_MsgIds[i].Clone());
            }
            cmd.m_SetVar = m_SetVar.Clone();
            cmd.m_SetVal = m_SetVal.Clone();
            cmd.m_TimeoutVal = m_TimeoutVal.Clone();
            cmd.m_TimeoutSetVar = m_TimeoutSetVar.Clone();
            cmd.m_TimeoutSetVal = m_TimeoutSetVal.Clone();
            cmd.m_HaveSet = m_HaveSet;
            return cmd;
        }

        protected override void ResetState()
        {
            m_CurTime = 0;
            m_StartTime = 0;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                m_MsgIds[i].Evaluate(instance, iterator, args);
            }
            if (m_HaveSet) {
                m_SetVar.Evaluate(instance, iterator, args);
                m_SetVal.Evaluate(instance, iterator, args);
                m_TimeoutVal.Evaluate(instance, iterator, args);
                m_TimeoutSetVar.Evaluate(instance, iterator, args);
                m_TimeoutSetVal.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            if (m_StartTime <= 0) {
                long startTime = GameFramework.TimeUtility.GetLocalMilliseconds();
                m_StartTime = startTime;
            }
            bool triggered = false;
            for (int i = 0; i < m_MsgIds.Count; i++) {
                long time = instance.GetMessageTriggerTime(m_MsgIds[i].Value);
                if (time > m_StartTime) {
                    triggered = true;
                    break;
                }
            }
            bool ret = false;
            if (triggered) {
                string varName = m_SetVar.Value;
                object varVal = m_SetVal.Value;
                instance.SetVariable(varName, varVal);
            } else {
                int timeout = m_TimeoutVal.Value;
                int curTime = m_CurTime;
                m_CurTime += (int)delta;
                if (timeout <= 0 || curTime <= timeout) {
                    ret = true;
                } else {
                    string varName = m_TimeoutSetVar.Value;
                    object varVal = m_TimeoutSetVal.Value;
                    instance.SetVariable(varName, varVal);
                }
            }
            return ret;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_MsgIds.Add(val);
            }
        }

        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count >= 3) {
                Dsl.CallData first = statementData.Functions[0].Call;
                Dsl.CallData second = statementData.Functions[1].Call;
                Dsl.CallData third = statementData.Functions[2].Call;
                if (null != first && null != second && null != third) {
                    m_HaveSet = true;

                    Load(first);
                    LoadSet(second);
                    LoadTimeoutSet(third);
                }
            }
        }

        private void LoadSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 2) {
                m_SetVar.InitFromDsl(callData.GetParam(0));
                m_SetVal.InitFromDsl(callData.GetParam(1));
            }
        }

        private void LoadTimeoutSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 3) {
                m_TimeoutVal.InitFromDsl(callData.GetParam(0));
                m_TimeoutSetVar.InitFromDsl(callData.GetParam(1));
                m_TimeoutSetVal.InitFromDsl(callData.GetParam(2));
            }
        }

        private List<IStoryValue<string>> m_MsgIds = new List<IStoryValue<string>>();
        private IStoryValue<string> m_SetVar = new StoryValue<string>();
        private IStoryValue<object> m_SetVal = new StoryValue();
        private IStoryValue<int> m_TimeoutVal = new StoryValue<int>();
        private IStoryValue<string> m_TimeoutSetVar = new StoryValue<string>();
        private IStoryValue<object> m_TimeoutSetVal = new StoryValue();
        private bool m_HaveSet = false;
        private int m_CurTime = 0;
        private long m_StartTime = 0;
    }
    /// <summary>
    /// waitallmessagehandler(msgid1,msgid2,...)[set(var,val)timeoutset(timeout,var,val)];
    /// </summary>
    internal class WaitAllMessageHandlerCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            WaitAllMessageHandlerCommand cmd = new WaitAllMessageHandlerCommand();
            for (int i = 0; i < m_MsgIds.Count; i++) {
                cmd.m_MsgIds.Add(m_MsgIds[i].Clone());
            }
            cmd.m_SetVar = m_SetVar.Clone();
            cmd.m_SetVal = m_SetVal.Clone();
            cmd.m_TimeoutVal = m_TimeoutVal.Clone();
            cmd.m_TimeoutSetVar = m_TimeoutSetVar.Clone();
            cmd.m_TimeoutSetVal = m_TimeoutSetVal.Clone();
            cmd.m_HaveSet = m_HaveSet;
            return cmd;
        }

        protected override void ResetState()
        {
            m_CurTime = 0;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                m_MsgIds[i].Evaluate(instance, iterator, args);
            }
            if (m_HaveSet) {
                m_SetVar.Evaluate(instance, iterator, args);
                m_SetVal.Evaluate(instance, iterator, args);
                m_TimeoutVal.Evaluate(instance, iterator, args);
                m_TimeoutSetVar.Evaluate(instance, iterator, args);
                m_TimeoutSetVal.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int ct = 0;
            for (int i = 0; i < m_MsgIds.Count; i++) {
                ct += GfxStorySystem.Instance.CountMessage(m_MsgIds[i].Value);
            }
            bool ret = false;
            if (ct <= 0) {
                string varName = m_SetVar.Value;
                object varVal = m_SetVal.Value;
                instance.SetVariable(varName, varVal);
            } else {
                int timeout = m_TimeoutVal.Value;
                int curTime = m_CurTime;
                m_CurTime += (int)delta;
                if (timeout <= 0 || curTime <= timeout) {
                    ret = true;
                } else {
                    string varName = m_TimeoutSetVar.Value;
                    object varVal = m_TimeoutSetVal.Value;
                    instance.SetVariable(varName, varVal);
                }
            }
            return ret;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_MsgIds.Add(val);
            }
        }

        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count >= 3) {
                Dsl.CallData first = statementData.Functions[0].Call;
                Dsl.CallData second = statementData.Functions[1].Call;
                Dsl.CallData third = statementData.Functions[2].Call;
                if (null != first && null != second && null != third) {
                    m_HaveSet = true;

                    Load(first);
                    LoadSet(second);
                    LoadTimeoutSet(third);
                }
            }
        }

        private void LoadSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 2) {
                m_SetVar.InitFromDsl(callData.GetParam(0));
                m_SetVal.InitFromDsl(callData.GetParam(1));
            }
        }

        private void LoadTimeoutSet(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 3) {
                m_TimeoutVal.InitFromDsl(callData.GetParam(0));
                m_TimeoutSetVar.InitFromDsl(callData.GetParam(1));
                m_TimeoutSetVal.InitFromDsl(callData.GetParam(2));
            }
        }

        private List<IStoryValue<string>> m_MsgIds = new List<IStoryValue<string>>();
        private IStoryValue<string> m_SetVar = new StoryValue<string>();
        private IStoryValue<object> m_SetVal = new StoryValue();
        private IStoryValue<int> m_TimeoutVal = new StoryValue<int>();
        private IStoryValue<string> m_TimeoutSetVar = new StoryValue<string>();
        private IStoryValue<object> m_TimeoutSetVal = new StoryValue();
        private bool m_HaveSet = false;
        private int m_CurTime = 0;
    }
    /// <summary>
    /// pauseallmessagehandler(msgid1,msgid2,...);
    /// </summary>
    internal class PauseAllMessageHandlerCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PauseAllMessageHandlerCommand cmd = new PauseAllMessageHandlerCommand();
            for (int i = 0; i < m_MsgIds.Count; i++) {
                cmd.m_MsgIds.Add(m_MsgIds[i].Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                m_MsgIds[i].Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                GfxStorySystem.Instance.PauseMessageHandler(m_MsgIds[i].Value, true);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_MsgIds.Add(val);
            }
        }

        private List<IStoryValue<string>> m_MsgIds = new List<IStoryValue<string>>();
    }
    /// <summary>
    /// resumeallmessagehandler(msgid1,msgid2,...);
    /// </summary>
    internal class ResumeAllMessageHandlerCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ResumeAllMessageHandlerCommand cmd = new ResumeAllMessageHandlerCommand();
            for (int i = 0; i < m_MsgIds.Count; i++) {
                cmd.m_MsgIds.Add(m_MsgIds[i].Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                m_MsgIds[i].Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            for (int i = 0; i < m_MsgIds.Count; i++) {
                GfxStorySystem.Instance.PauseMessageHandler(m_MsgIds[i].Value, false);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            for (int i = 0; i < num; ++i) {
                IStoryValue<string> val = new StoryValue<string>();
                val.InitFromDsl(callData.GetParam(i));
                m_MsgIds.Add(val);
            }
        }

        private List<IStoryValue<string>> m_MsgIds = new List<IStoryValue<string>>();
    }
    /// <summary>
    /// sendroomstorymessage(msg,arg1,arg2,...);
    /// </summary>
    internal class SendRoomStoryMessageCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendRoomStoryMessageCommand cmd = new SendRoomStoryMessageCommand();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string _msg = m_Msg.Value;

            Msg_CRC_StoryMessage msg = new Msg_CRC_StoryMessage();
            msg.m_MsgId = _msg;

            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                object v = val.Value;
                if (null == v) {
                    Msg_CRC_StoryMessage.MessageArg arg = new Msg_CRC_StoryMessage.MessageArg();
                    arg.val_type = ArgType.NULL;
                    arg.str_val = "";
                    msg.m_Args.Add(arg);
                } else if (v is int) {
                    Msg_CRC_StoryMessage.MessageArg arg = new Msg_CRC_StoryMessage.MessageArg();
                    arg.val_type = ArgType.INT;
                    arg.str_val = ((int)v).ToString();
                    msg.m_Args.Add(arg);
                } else if (v is float) {
                    Msg_CRC_StoryMessage.MessageArg arg = new Msg_CRC_StoryMessage.MessageArg();
                    arg.val_type = ArgType.FLOAT;
                    arg.str_val = ((float)v).ToString();
                    msg.m_Args.Add(arg);
                } else {
                    Msg_CRC_StoryMessage.MessageArg arg = new Msg_CRC_StoryMessage.MessageArg();
                    arg.val_type = ArgType.STRING;
                    arg.str_val = v.ToString();
                    msg.m_Args.Add(arg);
                }
            }
            Network.NetworkSystem.Instance.SendMessage(RoomMessageDefine.Msg_CRC_StoryMessage, msg);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_Msg.InitFromDsl(callData.GetParam(0));
            }
            for (int i = 1; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// sendserverstorymessage(msg,arg1,arg2,...);
    /// </summary>
    internal class SendServerStoryMessageCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendServerStoryMessageCommand cmd = new SendServerStoryMessageCommand();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string _msg = m_Msg.Value;

            Msg_CLC_StoryMessage protoData = new Msg_CLC_StoryMessage();
            protoData.m_MsgId = _msg;

            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                object v = val.Value;
                if (null == v) {
                    Msg_CLC_StoryMessage.MessageArg arg = new Msg_CLC_StoryMessage.MessageArg();
                    arg.val_type = LobbyArgType.NULL;
                    arg.str_val = "";
                    protoData.m_Args.Add(arg);
                } else if (v is int) {
                    Msg_CLC_StoryMessage.MessageArg arg = new Msg_CLC_StoryMessage.MessageArg();
                    arg.val_type = LobbyArgType.INT;
                    arg.str_val = ((int)v).ToString();
                    protoData.m_Args.Add(arg);
                } else if (v is float) {
                    Msg_CLC_StoryMessage.MessageArg arg = new Msg_CLC_StoryMessage.MessageArg();
                    arg.val_type = LobbyArgType.FLOAT;
                    arg.str_val = ((float)v).ToString();
                    protoData.m_Args.Add(arg);
                } else {
                    Msg_CLC_StoryMessage.MessageArg arg = new Msg_CLC_StoryMessage.MessageArg();
                    arg.val_type = LobbyArgType.STRING;
                    arg.str_val = v.ToString();
                    protoData.m_Args.Add(arg);
                }
            }

            try {
                Network.NodeMessage msg = new Network.NodeMessage(LobbyMessageDefine.Msg_CLC_StoryMessage, Network.UserNetworkSystem.Instance.Guid);
                msg.m_ProtoData = protoData;
                Network.NodeMessageDispatcher.SendMessage(msg);
            } catch (Exception ex) {
                LogSystem.Error("LobbyNetworkSystem.SendMessage throw Exception:{0}\n{1}", ex.Message, ex.StackTrace);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_Msg.InitFromDsl(callData.GetParam(0));
            }
            for (int i = 1; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// publishgfxevent(ev_name,group,arg1,arg2,...);
    /// </summary>
    internal class PublishGfxEventCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PublishGfxEventCommand cmd = new PublishGfxEventCommand();
            cmd.m_EventName = m_EventName.Clone();
            cmd.m_Group = m_Group.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_EventName.Evaluate(instance, iterator, args);
            m_Group.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string evname = m_EventName.Value;
            string group = m_Group.Value;
            ArrayList arglist = new ArrayList();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                arglist.Add(val.Value);
            }
            object[] args = arglist.ToArray();
            Utility.EventSystem.Publish(evname, group, args);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_EventName.InitFromDsl(callData.GetParam(0));
                m_Group.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<string> m_EventName = new StoryValue<string>();
        private IStoryValue<string> m_Group = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// sendgfxmessage(objname,msg,arg1,arg2,...);
    /// </summary>
    internal class SendGfxMessageCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendGfxMessageCommand cmd = new SendGfxMessageCommand();
            cmd.m_ObjName = m_ObjName.Clone();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjName.Evaluate(instance, iterator, args);
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string objname = m_ObjName.Value;
            string msg = m_Msg.Value;
            ArrayList arglist = new ArrayList();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                arglist.Add(val.Value);
            }
            object[] args = arglist.ToArray();
            if (args.Length == 0)
                Utility.SendMessage(objname, msg, null);
            else if (args.Length == 1)
                Utility.SendMessage(objname, msg, args[0]);
            else
                Utility.SendMessage(objname, msg, args);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjName.InitFromDsl(callData.GetParam(0));
                m_Msg.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<string> m_ObjName = new StoryValue<string>();
        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// sendgfxmessagewithtag(tagname,msg,arg1,arg2,...);
    /// </summary>
    internal class SendGfxMessageWithTagCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendGfxMessageWithTagCommand cmd = new SendGfxMessageWithTagCommand();
            cmd.m_ObjTag = m_ObjTag.Clone();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjTag.Evaluate(instance, iterator, args);
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string objtag = m_ObjTag.Value;
            string msg = m_Msg.Value;
            ArrayList arglist = new ArrayList();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                arglist.Add(val.Value);
            }
            object[] args = arglist.ToArray();
            if (args.Length == 0)
                Utility.SendMessageWithTag(objtag, msg, null);
            else if (args.Length == 1)
                Utility.SendMessageWithTag(objtag, msg, args[0]);
            else
                Utility.SendMessageWithTag(objtag, msg, args);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjTag.InitFromDsl(callData.GetParam(0));
                m_Msg.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<string> m_ObjTag = new StoryValue<string>();
        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// sendgfxmessagewithgameobject(gameobject,msg,arg1,arg2,...);
    /// </summary>
    internal class SendGfxMessageWithGameObjectCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendGfxMessageWithGameObjectCommand cmd = new SendGfxMessageWithGameObjectCommand();
            cmd.m_Object = m_Object.Clone();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Object.Evaluate(instance, iterator, args);
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object obj = m_Object.Value;
            var uobj = obj as UnityEngine.GameObject;
            if (null == uobj) {
                try {
                    int objId = (int)obj;
                    uobj = PluginFramework.Instance.GetGameObject(objId);
                } catch {
                    uobj = null;
                }
            }
            if (null != uobj) {
                string msg = m_Msg.Value;
                ArrayList arglist = new ArrayList();
                for (int i = 0; i < m_Args.Count; ++i) {
                    IStoryValue<object> val = m_Args[i];
                    arglist.Add(val.Value);
                }
                object[] args = arglist.ToArray();
                if (args.Length == 0)
                    uobj.SendMessage(msg, UnityEngine.SendMessageOptions.DontRequireReceiver);
                else if (args.Length == 1)
                    uobj.SendMessage(msg, args[0], UnityEngine.SendMessageOptions.DontRequireReceiver);
                else
                    uobj.SendMessage(msg, args, UnityEngine.SendMessageOptions.DontRequireReceiver);
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_Object.InitFromDsl(callData.GetParam(0));
                m_Msg.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }
        private IStoryValue<object> m_Object = new StoryValue();
        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// sendskillmessage(objId,skillid,seq,msg,arg1,arg2,...);
    /// </summary>
    internal class SendSkillMessageCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SendSkillMessageCommand cmd = new SendSkillMessageCommand();
            cmd.m_ActorId = m_ActorId.Clone();
            cmd.m_SkillId = m_SkillId.Clone();
            cmd.m_Seq = m_Seq.Clone();
            cmd.m_Msg = m_Msg.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ActorId.Evaluate(instance, iterator, args);
            m_SkillId.Evaluate(instance, iterator, args);
            m_Seq.Evaluate(instance, iterator, args);
            m_Msg.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int actorId = m_ActorId.Value;
            int skillId = m_SkillId.Value;
            int seq = m_Seq.Value;
            string msg = m_Msg.Value;
            Dictionary<string, object> locals = new Dictionary<string, object>();
            for (int i = 0; i < m_Args.Count - 1; i += 2) {
                string key = m_Args[i].Value as string;
                object val = m_Args[i + 1].Value;
                if (!string.IsNullOrEmpty(key)) {
                    locals.Add(key, val);
                }
            }
            GfxSkillSystem.Instance.SendMessage(actorId, skillId, seq, msg, locals);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 3) {
                m_ActorId.InitFromDsl(callData.GetParam(0));
                m_SkillId.InitFromDsl(callData.GetParam(1));
                m_Seq.InitFromDsl(callData.GetParam(2));
                m_Msg.InitFromDsl(callData.GetParam(3));
            }
            for (int i = 4; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<int> m_ActorId = new StoryValue<int>();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
        private IStoryValue<int> m_Seq = new StoryValue<int>();
        private IStoryValue<string> m_Msg = new StoryValue<string>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// creategameobject(name, prefab[, parent])[obj("varname")]{
    ///     position(vector3(x,y,z));
    ///     rotation(vector3(x,y,z));
    ///     scale(vector3(x,y,z));
    /// };
    /// </summary>
    internal class CreateGameObjectCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            CreateGameObjectCommand cmd = new CreateGameObjectCommand();
            cmd.m_Name = m_Name.Clone();
            cmd.m_Prefab = m_Prefab.Clone();
            cmd.m_HaveParent = m_HaveParent;
            cmd.m_Parent = m_Parent.Clone();
            cmd.m_HaveObj = m_HaveObj;
            cmd.m_ObjVarName = m_ObjVarName.Clone();
            cmd.m_Position = m_Position.Clone();
            cmd.m_Rotation = m_Rotation.Clone();
            cmd.m_Scale = m_Scale.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Name.Evaluate(instance, iterator, args);
            m_Prefab.Evaluate(instance, iterator, args);
            if (m_HaveParent) {
                m_Parent.Evaluate(instance, iterator, args);
            }
            if (m_HaveObj) {
                m_ObjVarName.Evaluate(instance, iterator, args);
            }
            m_Position.Evaluate(instance, iterator, args);
            m_Rotation.Evaluate(instance, iterator, args);
            m_Scale.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string name = m_Name.Value;
            string prefab = m_Prefab.Value;
            UnityEngine.GameObject obj = ResourceSystem.Instance.NewObject(prefab) as UnityEngine.GameObject;
            if(null!=obj){
                obj.name = name;
                if (m_HaveParent) {
                    object parent = m_Parent.Value;
                    string path = parent as string;
                    if (null != path) {
                        var pobj = UnityEngine.GameObject.Find(path);
                        if (null != pobj) {
                            obj.transform.SetParent(pobj.transform, false);
                        }
                    } else {
                        var pobj = parent as UnityEngine.GameObject;
                        if (null != pobj) {
                            obj.transform.SetParent(pobj.transform, false);
                        }
                    }
                }
                if (m_Position.HaveValue) {
                    var v = m_Position.Value;
                    obj.transform.localPosition = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
                if (m_Rotation.HaveValue) {
                    var v = m_Rotation.Value;
                    obj.transform.localEulerAngles = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
                if (m_Scale.HaveValue) {
                    var v = m_Scale.Value;
                    obj.transform.localScale = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
                if (m_HaveObj) {
                    string varName = m_ObjVarName.Value;
                    instance.SetVariable(varName, obj);
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_Name.InitFromDsl(callData.GetParam(0));
                m_Prefab.InitFromDsl(callData.GetParam(1));
                if (num > 2) {
                    m_HaveParent = true;
                    m_Parent.InitFromDsl(callData.GetParam(2));
                }
            }
        }
        protected override void Load(Dsl.FunctionData funcData)
        {
            var callData = funcData.Call;
            Load(callData);
            foreach (var comp in funcData.Statements) {
                var cd = comp as Dsl.CallData;
                if (null != cd) {
                    LoadOptional(cd);
                }
            }
        }
        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count == 2) {
                Dsl.FunctionData first = statementData.First;
                Dsl.FunctionData second = statementData.Second;
                if (null != first && null != first.Call && null != second && null != second.Call) {
                    Load(first.Call);
                    LoadVarName(second.Call);
                }
                if (null != second) {
                    foreach (var comp in second.Statements) {
                        var cd = comp as Dsl.CallData;
                        if (null != cd) {
                            LoadOptional(cd);
                        }
                    }
                }
            }
        }
        private void LoadVarName(Dsl.CallData callData)
        {
            if (callData.GetId() == "obj" && callData.GetParamNum() == 1) {
                m_ObjVarName.InitFromDsl(callData.GetParam(0));
                m_HaveObj = true;
            }
        }
        private void LoadOptional(Dsl.CallData callData)
        {
            string id = callData.GetId();
            if (id == "position") {
                m_Position.InitFromDsl(callData.GetParam(0));
            } else if (id == "rotation") {
                m_Rotation.InitFromDsl(callData.GetParam(0));
            } else if (id == "scale") {
                m_Scale.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<string> m_Name = new StoryValue<string>();
        private IStoryValue<string> m_Prefab = new StoryValue<string>();
        private IStoryValue<object> m_Parent = new StoryValue();
        private bool m_HaveParent = false;
        private bool m_HaveObj = false;
        private IStoryValue<string> m_ObjVarName = new StoryValue<string>();
        private IStoryValue<ScriptRuntime.Vector3> m_Position = new StoryValue<ScriptRuntime.Vector3>();
        private IStoryValue<ScriptRuntime.Vector3> m_Rotation = new StoryValue<ScriptRuntime.Vector3>();
        private IStoryValue<ScriptRuntime.Vector3> m_Scale = new StoryValue<ScriptRuntime.Vector3>();
    }
    /// <summary>
    /// settransform(name, local_or_world){
    ///     position(vector3(x,y,z));
    ///     rotation(vector3(x,y,z));
    ///     scale(vector3(x,y,z));
    /// };
    /// </summary>
    internal class SetTransformCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetTransformCommand cmd = new SetTransformCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_LocalOrWorld = m_LocalOrWorld.Clone();
            cmd.m_Position = m_Position.Clone();
            cmd.m_Rotation = m_Rotation.Clone();
            cmd.m_Scale = m_Scale.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_LocalOrWorld.Evaluate(instance, iterator, args);
            m_Position.Evaluate(instance, iterator, args);
            m_Rotation.Evaluate(instance, iterator, args);
            m_Scale.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            int localOrWorld = m_LocalOrWorld.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                if (m_Position.HaveValue) {
                    var v = m_Position.Value;
                    if (0 == localOrWorld)
                        obj.transform.localPosition = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                    else
                        obj.transform.position = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
                if (m_Rotation.HaveValue) {
                    var v = m_Rotation.Value;
                    if (0 == localOrWorld)
                        obj.transform.localEulerAngles = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                    else
                        obj.transform.eulerAngles = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
                if (m_Scale.HaveValue) {
                    var v = m_Scale.Value;
                    obj.transform.localScale = new UnityEngine.Vector3(v.X, v.Y, v.Z);
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_LocalOrWorld.InitFromDsl(callData.GetParam(1));
            }
        }
        protected override void Load(Dsl.FunctionData funcData)
        {
            var callData = funcData.Call;
            Load(callData);
            foreach (var comp in funcData.Statements) {
                var cd = comp as Dsl.CallData;
                if (null != cd) {
                    LoadOptional(cd);
                }
            }
        }
        private void LoadOptional(Dsl.CallData callData)
        {
            string id = callData.GetId();
            if (id == "position") {
                m_Position.InitFromDsl(callData.GetParam(0));
            } else if (id == "rotation") {
                m_Rotation.InitFromDsl(callData.GetParam(0));
            } else if (id == "scale") {
                m_Scale.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<int> m_LocalOrWorld = new StoryValue<int>();
        private IStoryValue<ScriptRuntime.Vector3> m_Position = new StoryValue<ScriptRuntime.Vector3>();
        private IStoryValue<ScriptRuntime.Vector3> m_Rotation = new StoryValue<ScriptRuntime.Vector3>();
        private IStoryValue<ScriptRuntime.Vector3> m_Scale = new StoryValue<ScriptRuntime.Vector3>();
    }
    /// <summary>
    /// destroygameobject(path);
    /// </summary>
    internal class DestroyGameObjectCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            DestroyGameObjectCommand cmd = new DestroyGameObjectCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objPath = m_ObjPath.Value;
            string path = objPath as string;
            if (null != path) {
                var obj = UnityEngine.GameObject.Find(path);
                if (null != obj) {
                    obj.transform.SetParent(null);
                    if (!ResourceSystem.Instance.RecycleObject(obj)) {
                        UnityEngine.GameObject.Destroy(obj);
                    }
                }
            } else {
                var obj = objPath as UnityEngine.GameObject;
                if (null != obj) {
                    obj.transform.SetParent(null);
                    if (!ResourceSystem.Instance.RecycleObject(obj)) {
                        UnityEngine.GameObject.Destroy(obj);
                    }
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
    }
    /// <summary>
    /// setparent(objpath,parent,stay_world_pos);
    /// </summary>
    internal class SetParentCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetParentCommand cmd = new SetParentCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_Parent = m_Parent.Clone();
            cmd.m_StayWorldPos = m_StayWorldPos.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_Parent.Evaluate(instance, iterator, args);
            m_StayWorldPos.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            object parentVal = m_Parent.Value;
            int stayWorldPos = m_StayWorldPos.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                string parentPath = parentVal as string;
                if (null != parentPath) {
                    if (string.IsNullOrEmpty(parentPath)) {
                        obj.transform.SetParent(null, stayWorldPos != 0);
                    } else {
                        var pobj = UnityEngine.GameObject.Find(parentPath);
                        if (null != pobj) {
                            obj.transform.SetParent(pobj.transform, stayWorldPos != 0);
                        }
                    }
                } else {
                    var pobj = parentVal as UnityEngine.GameObject;
                    if (null != pobj) {
                        obj.transform.SetParent(pobj.transform, stayWorldPos != 0);
                    } else {
                        try {
                            int id = (int)parentVal;
                            if (id <= 0) {
                                obj.transform.SetParent(null, stayWorldPos != 0);
                            } else {
                                pobj = PluginFramework.Instance.GetGameObject(id);
                                if (null != pobj) {
                                    obj.transform.SetParent(pobj.transform, stayWorldPos != 0);
                                }
                            }
                        } catch {
                        }
                    }
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_Parent.InitFromDsl(callData.GetParam(1));
                m_StayWorldPos.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<object> m_Parent = new StoryValue();
        private IStoryValue<int> m_StayWorldPos = new StoryValue<int>();
    }
    /// <summary>
    /// setactive(objpath,1_or_0);
    /// </summary>
    internal class SetActiveCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetActiveCommand cmd = new SetActiveCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_Active = m_Active.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_Active.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            int active = m_Active.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                obj.SetActive(active != 0);
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_Active.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<int> m_Active = new StoryValue<int>();
    }
    /// <summary>
    /// setvisible(objpath,1_or_0);
    /// </summary>
    internal class SetVisibleCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetVisibleCommand cmd = new SetVisibleCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_Visible = m_Visible.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_Visible.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            int visible = m_Visible.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        var view = EntityController.Instance.GetEntityViewById(id);
                        if (null != view) {
                            obj = view.Actor;
                            view.Visible = visible != 0;
                            return false;
                        }
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                var renderer = obj.GetComponent<UnityEngine.Renderer>();
                if (null != renderer) {
                    renderer.enabled = visible != 0;
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_Visible.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<int> m_Visible = new StoryValue<int>();
    }
    /// <summary>
    /// putonground(objpath);
    /// </summary>
    internal class PutOnGroundCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PutOnGroundCommand cmd = new PutOnGroundCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                var pos = obj.transform.position;
                Skill.Trigers.TriggerUtil.GetRayCastPosInNavMesh(pos + UnityEngine.Vector3.up * 500, pos + UnityEngine.Vector3.down * 500, ref pos);
                obj.transform.position = pos;
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
    }
    /// <summary>
    /// setnavmeshagentenable(objpath,1_or_0);
    /// </summary>
    internal class SetNavmeshAgentEnableCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetNavmeshAgentEnableCommand cmd = new SetNavmeshAgentEnableCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_Visible = m_Visible.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_Visible.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object objVal = m_ObjPath.Value;
            int enabled = m_Visible.Value;
            string objPath = objVal as string;
            UnityEngine.GameObject obj = null;
            if (null != objPath) {
                obj = UnityEngine.GameObject.Find(objPath);
            } else {
                obj = objVal as UnityEngine.GameObject;
                if (null == obj) {
                    try {
                        int id = (int)objVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                var agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (null != agent) {
                    agent.enabled = enabled != 0;
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_Visible.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<int> m_Visible = new StoryValue<int>();
    }
    /// <summary>
    /// addcomponent(objpath,type)[obj("varname")];
    /// </summary>
    internal class AddComponentCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            AddComponentCommand cmd = new AddComponentCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_ComponentType = m_ComponentType.Clone();
            cmd.m_HaveObj = m_HaveObj;
            cmd.m_ObjVarName = m_ObjVarName.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_ComponentType.Evaluate(instance, iterator, args);
            if (m_HaveObj) {
                m_ObjVarName.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            var objPathVal = m_ObjPath.Value;
            object componentType = m_ComponentType.Value;
            var obj = objPathVal as UnityEngine.GameObject;
            if (null == obj) {
                string objPath = objPathVal as string;
                if (null != objPath) {
                    obj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int id = (int)objPathVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                UnityEngine.Component component = null;
                Type t = componentType as Type;
                if (null != t) {
                    component = obj.AddComponent(t);
                } else {
                    string name = componentType as string;
                    if (null != name) {
                        t = Type.GetType(name);
                        component = obj.AddComponent(t);
                    }
                }
                if (m_HaveObj) {
                    string varName = m_ObjVarName.Value;
                    instance.SetVariable(varName, component);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_ComponentType.InitFromDsl(callData.GetParam(1));
            }
        }

        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count == 2) {
                Dsl.FunctionData first = statementData.First;
                Dsl.FunctionData second = statementData.Second;
                if (null != first && null != first.Call && null != second && null != second.Call) {
                    Load(first.Call);
                    LoadVarName(second.Call);
                }
            }
        }

        private void LoadVarName(Dsl.CallData callData)
        {
            if (callData.GetId() == "obj" && callData.GetParamNum() == 1) {
                m_ObjVarName.InitFromDsl(callData.GetParam(0));
                m_HaveObj = true;
            }
        }
        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<object> m_ComponentType = new StoryValue();
        private bool m_HaveObj = false;
        private IStoryValue<string> m_ObjVarName = new StoryValue<string>();
    }
    /// <summary>
    /// removecomponent(objpath,type);
    /// </summary>
    internal class RemoveComponentCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            RemoveComponentCommand cmd = new RemoveComponentCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_ComponentType = m_ComponentType.Clone();
            return cmd;
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_ComponentType.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            var objPathVal = m_ObjPath.Value;
            object componentType = m_ComponentType.Value;
            var obj = objPathVal as UnityEngine.GameObject;
            if (null == obj) {
                string objPath = objPathVal as string;
                if (null != objPath) {
                    obj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int id = (int)objPathVal;
                        obj = PluginFramework.Instance.GetGameObject(id);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                Type t = componentType as Type;
                if (null != t) {
                    var comp = obj.GetComponent(t);
                    UnityEngine.GameObject.Destroy(comp);
                } else {
                    string name = componentType as string;
                    if (null != name) {
                        t = Type.GetType(name);
                        var comp = obj.GetComponent(t);
                        UnityEngine.GameObject.Destroy(comp);
                    }
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_ComponentType.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<object> m_ComponentType = new StoryValue();
    }
    /// <summary>
    /// installplugin(obj_path, plugin_class, is_tick_plugin, use_lua_plugin);
    /// </summary>
    internal class InstallPluginCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            InstallPluginCommand cmd = new InstallPluginCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_PluginClass = m_PluginClass.Clone();
            cmd.m_IsTickPlugin = m_IsTickPlugin.Clone();
            cmd.m_UseLuaPlugin = m_UseLuaPlugin.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_PluginClass.Evaluate(instance, iterator, args);
            m_IsTickPlugin.Evaluate(instance, iterator, args);
            m_UseLuaPlugin.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string objPath = m_ObjPath.Value;
            string pluginClass = m_PluginClass.Value;
            int isTickPlugin = m_IsTickPlugin.Value;
            int useLuaPlugin = m_UseLuaPlugin.Value;
            if (useLuaPlugin != 0) {
                if (isTickPlugin != 0)
                    Plugin.PluginProxy.LuaProxy.InstallTickPlugin(objPath, pluginClass);
                else
                    Plugin.PluginProxy.LuaProxy.InstallStartupPlugin(objPath, pluginClass);
            } else {
                if (isTickPlugin != 0)
                    Plugin.PluginProxy.NativeProxy.InstallTickPlugin(objPath, pluginClass);
                else
                    Plugin.PluginProxy.NativeProxy.InstallStartupPlugin(objPath, pluginClass);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 3) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_PluginClass.InitFromDsl(callData.GetParam(1));
                m_IsTickPlugin.InitFromDsl(callData.GetParam(2));
                m_UseLuaPlugin.InitFromDsl(callData.GetParam(3));
            }
        }

        private IStoryValue<string> m_ObjPath = new StoryValue<string>();
        private IStoryValue<string> m_PluginClass = new StoryValue<string>();
        private IStoryValue<int> m_IsTickPlugin = new StoryValue<int>();
        private IStoryValue<int> m_UseLuaPlugin = new StoryValue<int>();
    }
    /// <summary>
    /// removeplugin(obj_path, plugin_class, is_tick_plugin, use_lua_plugin);
    /// </summary>
    internal class RemovePluginCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            RemovePluginCommand cmd = new RemovePluginCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_PluginClass = m_PluginClass.Clone();
            cmd.m_IsTickPlugin = m_IsTickPlugin.Clone();
            cmd.m_UseLuaPlugin = m_UseLuaPlugin.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_PluginClass.Evaluate(instance, iterator, args);
            m_IsTickPlugin.Evaluate(instance, iterator, args);
            m_UseLuaPlugin.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string objPath = m_ObjPath.Value;
            string pluginClass = m_PluginClass.Value;
            int isTickPlugin = m_IsTickPlugin.Value;
            int useLuaPlugin = m_UseLuaPlugin.Value;
            if (useLuaPlugin != 0) {
                if (isTickPlugin != 0)
                    Plugin.PluginProxy.LuaProxy.RemoveTickPlugin(objPath, pluginClass);
                else
                    Plugin.PluginProxy.LuaProxy.RemoveStartupPlugin(objPath, pluginClass);
            } else {
                if (isTickPlugin != 0)
                    Plugin.PluginProxy.NativeProxy.RemoveTickPlugin(objPath, pluginClass);
                else
                    Plugin.PluginProxy.NativeProxy.RemoveStartupPlugin(objPath, pluginClass);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 3) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_PluginClass.InitFromDsl(callData.GetParam(1));
                m_IsTickPlugin.InitFromDsl(callData.GetParam(2));
                m_UseLuaPlugin.InitFromDsl(callData.GetParam(3));
            }
        }

        private IStoryValue<string> m_ObjPath = new StoryValue<string>();
        private IStoryValue<string> m_PluginClass = new StoryValue<string>();
        private IStoryValue<int> m_IsTickPlugin = new StoryValue<int>();
        private IStoryValue<int> m_UseLuaPlugin = new StoryValue<int>();
    }
    /// <summary>
    /// openurl(url);
    /// </summary>
    internal class OpenUrlCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            OpenUrlCommand cmd = new OpenUrlCommand();
            cmd.m_Url = m_Url.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Url.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            UnityEngine.Application.OpenURL(m_Url.Value);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_Url.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<string> m_Url = new StoryValue<string>();
    }
    /// <summary>
    /// quit();
    /// </summary>
    internal class QuitCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            QuitCommand cmd = new QuitCommand();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            UnityEngine.Application.Quit();
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
        }
    }
    /// <summary>
    /// changescene(target_scene_id);
    /// </summary>
    internal class ChangeSceneCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ChangeSceneCommand cmd = new ChangeSceneCommand();
            cmd.m_TargetSceneId = m_TargetSceneId.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_TargetSceneId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int targetSceneId = m_TargetSceneId.Value;
            PluginFramework.Instance.DelayChangeScene(targetSceneId);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_TargetSceneId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_TargetSceneId = new StoryValue<int>();
    }
    /// <summary>
    /// openbattle(target_scene_id);
    /// </summary>
    internal class OpenBattleCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            OpenBattleCommand cmd = new OpenBattleCommand();
            cmd.m_TargetSceneId = m_TargetSceneId.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_TargetSceneId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int targetSceneId = m_TargetSceneId.Value;
            PluginFramework.Instance.LoadBattle(targetSceneId);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_TargetSceneId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_TargetSceneId = new StoryValue<int>();
    }
    /// <summary>
    /// closebattle(target_scene_id);
    /// </summary>
    internal class CloseBattleCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            CloseBattleCommand cmd = new CloseBattleCommand();
            cmd.m_TargetSceneId = m_TargetSceneId.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_TargetSceneId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int targetSceneId = m_TargetSceneId.Value;
            PluginFramework.Instance.UnloadBattle(targetSceneId);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_TargetSceneId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_TargetSceneId = new StoryValue<int>();
    }
    /// <summary>
    /// createscenelogic(config_id,logic_id,stringlist("param1 param2 param3 ..."));
    /// </summary>
    internal class CreateSceneLogicCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            CreateSceneLogicCommand cmd = new CreateSceneLogicCommand();
            cmd.m_ConfigId = m_ConfigId.Clone();
            cmd.m_Logic = m_Logic.Clone();
            cmd.m_Params = m_Params.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ConfigId.Evaluate(instance, iterator, args);
            m_Logic.Evaluate(instance, iterator, args);
            m_Params.Evaluate(instance, iterator, args);

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int configId = m_ConfigId.Value;
            int logicId = m_Logic.Value;
            IEnumerable args = m_Params.Value;
            List<string> list = new List<string>();
            foreach (string arg in args) {
                list.Add(arg);
            }
            int id = PluginFramework.Instance.CreateSceneLogic(configId, logicId, list.ToArray());
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_ConfigId.InitFromDsl(callData.GetParam(0));
                m_Logic.InitFromDsl(callData.GetParam(1));
                m_Params.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ConfigId = new StoryValue<int>();
        private IStoryValue<int> m_Logic = new StoryValue<int>();
        private IStoryValue<IEnumerable> m_Params = new StoryValue<IEnumerable>();
    }
    /// <summary>
    /// destroyscenelogic(config_id);
    /// </summary>
    internal class DestroySceneLogicCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            DestroySceneLogicCommand cmd = new DestroySceneLogicCommand();
            cmd.m_ConfigId = m_ConfigId.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ConfigId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int configId = m_ConfigId.Value;
            PluginFramework.Instance.DestroySceneLogicByConfigId(configId);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ConfigId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_ConfigId = new StoryValue<int>();
    }
    /// <summary>
    /// pausescenelogic(scene_logic_config_id,true_or_false);
    /// </summary>
    internal class PauseSceneLogicCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            PauseSceneLogicCommand cmd = new PauseSceneLogicCommand();
            cmd.m_SceneLogicConfigId = m_SceneLogicConfigId.Clone();
            cmd.m_Enabled = m_Enabled.Clone();
            return cmd;
        }

        protected override void ResetState()
        { }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_SceneLogicConfigId.Evaluate(instance, iterator, args);
            m_Enabled.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int cfgId = m_SceneLogicConfigId.Value;
            string enabled = m_Enabled.Value;
            SceneLogicInfo info = PluginFramework.Instance.GetSceneLogicInfoByConfigId(cfgId);
            if (null != info) {
                info.IsLogicPaused = (0 == string.Compare(enabled, "true"));
            } else {
                LogSystem.Error("pausescenelogic can't find scenelogic {0}", cfgId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_SceneLogicConfigId.InitFromDsl(callData.GetParam(0));
                m_Enabled.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_SceneLogicConfigId = new StoryValue<int>();
        private IStoryValue<string> m_Enabled = new StoryValue<string>();
    }
    /// <summary>
    /// restarttimeout(scene_logic_config_id[,timeout]);
    /// </summary>
    internal class RestartTimeoutCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            RestartTimeoutCommand cmd = new RestartTimeoutCommand();
            cmd.m_ParamNum = m_ParamNum;
            cmd.m_SceneLogicConfigId = m_SceneLogicConfigId.Clone();
            cmd.m_Timeout = m_Timeout.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_SceneLogicConfigId.Evaluate(instance, iterator, args);
            if (m_ParamNum > 1)
                m_Timeout.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int cfgId = m_SceneLogicConfigId.Value;
            SceneLogicInfo info = PluginFramework.Instance.GetSceneLogicInfoByConfigId(cfgId);
            if (null != info) {
                TimeoutLogicInfo data = info.LogicDatas.GetData<TimeoutLogicInfo>();
                if (null != data) {
                    data.m_IsTriggered = false;
                    data.m_CurTime = 0;
                    if (m_ParamNum > 1) {
                        data.m_Timeout = m_Timeout.Value;
                    }
                } else {
                    LogSystem.Warn("restarttimeout scenelogic {0} dosen't start, add wait command !", cfgId);
                }
            } else {
                LogSystem.Error("restarttimeout can't find scenelogic {0}", cfgId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            m_ParamNum = callData.GetParamNum();
            if (m_ParamNum > 0) {
                m_SceneLogicConfigId.InitFromDsl(callData.GetParam(0));
            }
            if (m_ParamNum > 1) {
                m_Timeout.InitFromDsl(callData.GetParam(1));
            }
        }

        private int m_ParamNum = 0;
        private IStoryValue<int> m_SceneLogicConfigId = new StoryValue<int>();
        private IStoryValue<int> m_Timeout = new StoryValue<int>();
    }
    /// <summary>
    /// highlightpromptwithdict(dictid,arg1,arg2,...);
    /// </summary>
    internal class HighlightPromptWithDictCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            HighlightPromptWithDictCommand cmd = new HighlightPromptWithDictCommand();
            cmd.m_DictId = m_DictId.Clone();
            for (int i = 0; i < m_DictArgs.Count; ++i) {
                IStoryValue<object> val = m_DictArgs[i];
                cmd.m_DictArgs.Add(val.Clone());
            }
            return cmd;
        }
        protected override void ResetState()
        { }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_DictId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_DictArgs.Count; ++i) {
                IStoryValue<object> val = m_DictArgs[i];
                val.Evaluate(instance, iterator, args);
            }

            for (int i = 0; i < m_DictArgs.Count; ++i) {
                IStoryValue<object> val = m_DictArgs[i];
            }
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string dictId = m_DictId.Value;
            ArrayList arglist = new ArrayList();
            for (int i = 0; i < m_DictArgs.Count; ++i) {
                IStoryValue<object> val = m_DictArgs[i];
                arglist.Add(val.Value);
            }
            object[] args = arglist.ToArray();
            PluginFramework.Instance.HighlightPromptWithDict(dictId, args);
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_DictId.InitFromDsl(callData.GetParam(0));
            }
            for (int i = 1; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_DictArgs.Add(val);
            }
        }
        private IStoryValue<string> m_DictId = new StoryValue<string>();
        private List<IStoryValue<object>> m_DictArgs = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// highlightprompt(txt);
    /// </summary>
    internal class HighlightPromptCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            HighlightPromptCommand cmd = new HighlightPromptCommand();
            cmd.m_Info = m_Info.Clone();
            return cmd;
        }
        protected override void ResetState()
        { }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_Info.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            string info = m_Info.Value;
            PluginFramework.Instance.HighlightPrompt(info);
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_Info.InitFromDsl(callData.GetParam(0));
            }
        }
        private IStoryValue<string> m_Info = new StoryValue<string>();
    }
    /// <summary>
    /// setactorscale(name,value);
    /// </summary>
    internal class SetActorScaleCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetActorScaleCommand cmd = new SetActorScaleCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            object value = m_Value.Value;
            UnityEngine.GameObject obj = EntityController.Instance.GetGameObject(objId);
            if (null != obj) {
                ScriptRuntime.Vector3 scale = (ScriptRuntime.Vector3)value;
                obj.transform.localScale = new UnityEngine.Vector3(scale.X, scale.Y, scale.Z);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<object> m_Value = new StoryValue();
    }
    /// <summary>
    /// gameobjectanimation(obj, anim[, normalized_time]);
    /// </summary>
    internal class GameObjectAnimationCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            GameObjectAnimationCommand cmd = new GameObjectAnimationCommand();
            cmd.m_ParamNum = m_ParamNum;
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_Anim = m_Anim.Clone();
            cmd.m_Time = m_Time.Clone();
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_Anim.Evaluate(instance, iterator, args);
            if (m_ParamNum > 2) {
                m_Time.Evaluate(instance, iterator, args);
            }
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object o = m_ObjPath.Value;
            string objPath = o as string;
            var uobj = o as UnityEngine.GameObject;
            if (null == uobj) {
                if (null != objPath) {
                    uobj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int objId = (int)o;
                        uobj = PluginFramework.Instance.GetGameObject(objId);
                    } catch {
                        uobj = null;
                    }
                }
            }
            if (null != uobj) {
                string anim = m_Anim.Value;
                EntityViewModel view = EntityController.Instance.GetEntityView(uobj);
                if (null != view && null != view.Animator) {
                    view.Animator.Play(anim);
                } else {
                    var animator = uobj.GetComponent<UnityEngine.Animator>();
                    if (null != animator) {
                        animator.Play(anim);
                    }
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            m_ParamNum = callData.GetParamNum();
            if (m_ParamNum > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_Anim.InitFromDsl(callData.GetParam(1));
            }
            if (m_ParamNum > 2) {
                m_Time.InitFromDsl(callData.GetParam(2));
            }
        }
        private int m_ParamNum = 0;
        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<string> m_Anim = new StoryValue<string>();
        private IStoryValue<float> m_Time = new StoryValue<float>();
    }
    /// <summary>
    /// gameobjectanimationparam(obj)
    /// {
    ///     float(name,val);
    ///     int(name,val);
    ///     bool(name,val);
    ///     trigger(name,val);
    /// };
    /// </summary>
    internal class GameObjectAnimationParamCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            GameObjectAnimationParamCommand cmd = new GameObjectAnimationParamCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            for (int i = 0; i < m_Params.Count; ++i) {
                ParamInfo param = new ParamInfo();
                param.CopyFrom(m_Params[i]);
                cmd.m_Params.Add(param);
            }
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Params.Count; ++i) {
                var pair = m_Params[i];
                pair.Key.Evaluate(instance, iterator, args);
                pair.Value.Evaluate(instance, iterator, args);
            }

            for (int i = 0; i < m_Params.Count; ++i) {
                var pair = m_Params[i];
            }
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object o = m_ObjPath.Value;
            string objPath = o as string;
            var obj = o as UnityEngine.GameObject;
            if (null == obj) {
                if (null != objPath) {
                    obj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int objId = (int)o;
                        obj = PluginFramework.Instance.GetGameObject(objId);
                    } catch {
                        obj = null;
                    }
                }
            }
            if (null != obj) {
                UnityEngine.Animator animator = obj.GetComponentInChildren<UnityEngine.Animator>();
                if (null != animator) {
                    for (int i = 0; i < m_Params.Count; ++i) {
                        var param = m_Params[i];
                        string type = param.Type;
                        string key = param.Key.Value;
                        object val = param.Value.Value;
                        if (type == "int") {
                            int v = (int)Convert.ChangeType(val, typeof(int));
                            animator.SetInteger(key, v);
                        } else if (type == "float") {
                            float v = (float)Convert.ChangeType(val, typeof(float));
                            animator.SetFloat(key, v);
                        } else if (type == "bool") {
                            bool v = (bool)Convert.ChangeType(val, typeof(bool));
                            animator.SetBool(key, v);
                        } else if (type == "trigger") {
                            string v = val.ToString();
                            if (v == "false") {
                                animator.ResetTrigger(key);
                            } else {
                                animator.SetTrigger(key);
                            }
                        }
                    }
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
            }
        }
        protected override void Load(Dsl.FunctionData funcData)
        {
            Dsl.CallData callData = funcData.Call;
            if (null != callData) {
                Load(callData);
                for (int i = 0; i < funcData.Statements.Count; ++i) {
                    Dsl.ISyntaxComponent statement = funcData.Statements[i];
                    Dsl.CallData stCall = statement as Dsl.CallData;
                    if (null != stCall && stCall.GetParamNum() >= 2) {
                        string id = stCall.GetId();
                        ParamInfo param = new ParamInfo(id, stCall.GetParam(0), stCall.GetParam(1));
                        m_Params.Add(param);
                    }
                }
            }
        }
        private class ParamInfo
        {
            internal string Type;
            internal IStoryValue<string> Key;
            internal IStoryValue<object> Value;
            internal ParamInfo()
            {
                Init();
            }
            internal ParamInfo(string type, Dsl.ISyntaxComponent keyDsl, Dsl.ISyntaxComponent valDsl)
                : this()
            {
                Type = type;
                Key.InitFromDsl(keyDsl);
                Value.InitFromDsl(valDsl);
            }
            internal void CopyFrom(ParamInfo other)
            {
                Type = other.Type;
                Key = other.Key.Clone();
                Value = other.Value.Clone();
            }
            private void Init()
            {
                Type = string.Empty;
                Key = new StoryValue<string>();
                Value = new StoryValue();
            }
        }
        private IStoryValue<object> m_ObjPath = new StoryValue();
        private List<ParamInfo> m_Params = new List<ParamInfo>();
    }
    /// <summary>
    /// gameobjectcastskill(obj, skillid, arg1, arg2, ...);
    /// </summary>
    internal class GameObjectCastSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            GameObjectCastSkillCommand cmd = new GameObjectCastSkillCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            cmd.m_SkillId = m_SkillId.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);
            m_SkillId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }

            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
            }
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object o = m_ObjPath.Value;
            string objPath = o as string;
            var uobj = o as UnityEngine.GameObject;
            if (null == uobj) {
                if (null != objPath) {
                    uobj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int objId = (int)o;
                        uobj = PluginFramework.Instance.GetGameObject(objId);
                    } catch {
                        uobj = null;
                    }
                }
            }
            int skillId = m_SkillId.Value;
            StrObjDict locals = new StrObjDict();
            for (int i = 0; i < m_Args.Count - 1; i += 2) {
                string key = m_Args[i].Value as string;
                object val = m_Args[i + 1].Value;
                if (!string.IsNullOrEmpty(key)) {
                    locals.Add(key, val);
                }
            }
            var cfg = TableConfig.SkillProvider.Instance.GetSkill(skillId);
            if (null != cfg) {
                GfxSkillSystem.Instance.StartSkillWithGameObject(uobj, cfg, 0, locals);
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
                m_SkillId.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }
        private IStoryValue<object> m_ObjPath = new StoryValue();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// gameobjectstopskill(obj);
    /// </summary>
    internal class GameObjectStopSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            GameObjectStopSkillCommand cmd = new GameObjectStopSkillCommand();
            cmd.m_ObjPath = m_ObjPath.Clone();
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjPath.Evaluate(instance, iterator, args);

        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            object o = m_ObjPath.Value;
            string objPath = o as string;
            var uobj = o as UnityEngine.GameObject;
            if (null == uobj) {
                if (null != objPath) {
                    uobj = UnityEngine.GameObject.Find(objPath);
                } else {
                    try {
                        int objId = (int)o;
                        uobj = PluginFramework.Instance.GetGameObject(objId);
                    } catch {
                        uobj = null;
                    }
                }
            }
            GfxSkillSystem.Instance.StopAllSkillWithGameObject(uobj, true);
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ObjPath.InitFromDsl(callData.GetParam(0));
            }
        }
        private IStoryValue<object> m_ObjPath = new StoryValue();
    }
}
