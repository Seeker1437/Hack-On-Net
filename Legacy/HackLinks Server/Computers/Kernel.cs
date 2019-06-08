﻿using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons;
using HackLinks_Server.Daemons.Types;
using HackLinks_Server.Files;
using HackLinksCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;

namespace HackLinks_Server.Computers
{
    public class Kernel
    {
        private Node node;

        public Kernel(Node node)
        {
            this.node = node;
        }

        private GameClient GetClient(Process process)
        {
            Session session = node.GetSession(process.ProcessId) ?? null;
            return session  != null? session.owner : null;
        }

        private Session GetSession(Process process)
        {
            return node.GetSession(process.ProcessId);
            // TODO throw exception if null. All processes here should belong to a session.
        }

        public void Display(Process process, string type, params string[] data)
        {
            List<string> completeData = new List<string>(){"state", type};
            completeData.AddRange(data);
            GetClient(process).Send(NetUtil.PacketType.KERNL, completeData.ToArray());
        }

        public void Login(CommandProcess process, string username, string password)
        {
            GameClient client = GetClient(process);
            Credentials credentials = node.Login(GetClient(process), username, password);
            if (credentials != null)
            {
                client.Login(node, credentials);
                client.Send(NetUtil.PacketType.MESSG, "Logged as : " + username);
                node.Log(Log.LogEvents.Login, node.logs.Count + 1 + " " + client.homeComputer.ip + " logged in as " + username, client.activeSession.sessionId, client.homeComputer.ip);
            } else
            {
                client.Send(NetUtil.PacketType.MESSG, "Wrong identificants.");
            }
        }

        public void Connect(Process process, string host)
        {
            GameClient client = GetClient(process);
            if (client.activeSession != null)
                client.activeSession.DisconnectSession();
            var compManager = client.server.GetComputerManager();
            string resultIP = null;

            if (client.homeComputer != null)
            {
                if (host == "localhost" || host == "127.0.0.1")
                    resultIP = client.homeComputer.ip;
                else
                {
                    var DNSConfigFile = client.homeComputer.fileSystem.rootFile.GetFileAtPath("/cfg/dns.cfg");
                    if (DNSConfigFile != null)
                    {
                        foreach (string ip in DNSConfigFile.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var DNSNode = compManager.GetNodeByIp(ip);
                            if (DNSNode == null)
                                continue;
                            var daemon = (DNSDaemon)DNSNode.GetDaemon("dns");
                            if (daemon == null)
                                continue;
                            resultIP = daemon.LookUp(host);
                            if (resultIP != null)
                                break;
                        }
                    }
                }
            }
            var connectingToNode = compManager.GetNodeByIp(resultIP ?? host);
            if (connectingToNode != null)
                client.ConnectTo(connectingToNode);
            else
                client.Send(NetUtil.PacketType.KERNL, "connect", "fail", "0");
        }

        /// <summary>
        /// If the given is currently an attached process, then reattach the parent instead
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public void ReattachParent(Process parent, Process child)
        {
            GameClient client = GetClient(child);
            if (client != null)
            {
                Session session = client.activeSession;
                if (session.HasProcessId(child.ProcessId))
                {
                    session.AttachProcess(parent);
                }
            }
        }

        public void Disconnect(CommandProcess process)
        {
            GetClient(process).Disconnect();
        }

        public void LS(Process process, string[] fileData)
        {
            List<string> data = new List<string>() { "ls" };
            data.AddRange(fileData);

            GetClient(process).Send(NetUtil.PacketType.KERNL, data.ToArray());
        }

        public void CD(Process process, string name)
        {
            GetClient(process).Send(NetUtil.PacketType.KERNL, new string[] { "cd", name});
        }

        public void RemoveFile(Process process, File file)
        {
            file.RemoveFile();
            GetClient(process).server.GetComputerManager().AddToDelete(file);
        }

        public Process StartProcess(Process process, File file)
        {
            if(!file.HasExecutePermission(process.Credentials))
                StartProcess(process, "False");
            string type = GetClient(process).server.GetCompileManager().GetType(file.Checksum);
            return StartProcess(process, type);
        }

        public Process StartProcess(Process process, string type)
        {
            // TODO clone credentials OR make sure they're imutable
            Process child = GetClient(process).CreateProcess(node, type, process);
            StartProcess(process, child);
            return child;
        }

        public Process StartProcess(Process process, Process child)
        {
            process.computer.SetChildProcess(process, child);
            child.ActiveDirectory = process.ActiveDirectory;
            return child;
        }

        public void PlayMusic(Process process, string song)
        {
            GetClient(process).Send(NetUtil.PacketType.MUSIC, song);
        }

        public void OpenDaemon(CommandProcess process, string target)
        {
            Session activeSession = GetClient(process).activeSession;
            foreach (Daemon daemon in activeSession.connectedNode.daemons)
            {
                if (daemon.IsOfType(target))
                {
                    DaemonClient daemonClient = daemon.CreateClient(activeSession, process);
                    StartProcess(process, daemonClient);
                    daemon.OnConnect(activeSession, daemonClient);
                    GetClient(process).activeSession.AttachProcess(daemonClient);
                }
            }
        }

        public List<Account> GetAccounts()
        {
            return Account.FromFile(node.fileSystem.rootFile.GetFileAtPath("etc/passwd"), node);
        }
    }
}
