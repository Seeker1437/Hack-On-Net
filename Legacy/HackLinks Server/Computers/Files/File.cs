﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Files;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;
using MySql.Data.MySqlClient;

namespace HackLinks_Server.Files
{
    public class File
    {

        /// <summary>FilType determines how a file will be handled by the system</summary>
        public enum FileType
        {
            Regular,
            Directory,
            Link,
            LOG,
            // TODO other types 
        }

        public readonly int id;

        private string name;
        private int ownerId;
        private Group group;
        private string content = "";

        private File parent;
        private int parentId;
        public int computerId;

        private FileType type = FileType.Regular;

        public bool Dirty { get; set; }

        public string Name { get => name; set { name = value; Dirty = true; } }

        public FilePermissions Permissions { get; set; }

        public int OwnerId { get => ownerId; set { ownerId = value; Dirty = true; } }

        public Group Group { get => group; set { group = value; Dirty = true; } }

        public string Content { get => content; set { content = value; Dirty = true; Checksum = content.GetHashCode(); } } // TODO make hash function portable/low collision eg. https://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed
        public int Checksum { get; private set; }

        public int ParentId { get => parentId; set { parentId = value; Dirty = true; } }
        public int ComputerId { get => computerId; set { computerId = value; Dirty = true; } }

        public FileType Type { get => type; set { type = value; Dirty = true; } }

        internal File Parent { get => parent;
            set
            {
                if (parent != null)
                {
                    parent.children.RemoveAll(child => child.id == id);
                }

                parent = value;
                if(parent != null)
                {
                    ParentId = parent.id;
                }
            }
        }

        public List<File> children = new List<File>();
        internal bool isFolder;

        protected File(int id, Node computer, File parent, string name)
        {
            this.id = id;
            this.computerId = computer.id;
            this.Name = name;
            this.Parent = parent;
            if(parent != null)
            {
                this.Parent.children.Add(this);
            }
            Permissions = new FilePermissions(this);
        }

        /// <summary>
        /// Create a new file and register it a new file id with the given <see cref="FileSystemManager"/>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="computer"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static File CreateNewFile(FileSystemManager manager, Node computer, File parent, string name)
        {
            File newFile = new File(manager.GetNewFileId(), computer, parent, name);
            manager.RegisterNewFile(newFile);
            return newFile;
        }

        /// <summary>
        /// Attempt to create a new file with the given id and register it with the given <see cref="FileSystemManager"/>
        /// It's usually better to use <see cref="CreateNewFile(FileSystemManager, Node, File, string)"/> unless you need to explicitly specify the file id.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="computer"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown when the id is already registered</exception>
        public static File CreateNewFile(int id, FileSystemManager manager, Node computer, File parent, string name)
        {
            if (manager.IsIdInUse(id))
            {
                throw new ArgumentException($"File id \"{id}\" is already in use");
            }

            File newFile = new File(id, computer, parent, name);
            manager.RegisterNewFile(newFile);
            return newFile;
        }

        public static File CreateNewFolder(FileSystemManager manager, Node computer, File parent, string name)
        {
            File newFile = new File(manager.GetNewFileId(), computer, parent, name);
            newFile.isFolder = true;
            manager.RegisterNewFile(newFile);
            return newFile;
        }

        public bool HasExecutePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, false, true);
        }

        public bool HasWritePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, true, false);
        }

        public bool HasReadPermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, true, false, false);
        }

        public bool HasExecutePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, false, true);
        }

        public bool HasWritePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, true, false);
        }

        public bool HasReadPermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, true, false, false);
        }

        public bool HasExecutePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, false, true);
        }

        public bool HasWritePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, true, false);
        }

        public bool HasReadPermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, true, false, false);
        }

        public bool HasPermission(int userId, Group priv, bool read, bool write, bool execute)
        {
            return HasPermission(userId, new List<Group> { priv }, read, write, execute);
        }

        public bool HasPermission(int userId, List<Group> privs, bool read, bool write, bool execute)
        {
            if (privs.Contains(Group))
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.Group, read, write, execute))
                {
                    return true;
                }
            }

            if (OwnerId == userId)
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.User, read, write, execute))
                {
                    return true;
                }
            }

            return Permissions.CheckPermission(FilePermissions.PermissionType.Others, read, write, execute);
        }

        virtual public bool IsFolder()
        {
            return isFolder;
        }

        virtual public void RemoveFile()
        {
            Parent.children.Remove(this);
            ParentId = 0;
            if (Type == FileType.LOG)
            {
                Log log = null;
                foreach (var log2 in Server.Instance.GetComputerManager().GetNodeById(ComputerId).logs)
                {
                    if (log2.file == this)
                    {
                        log = log2;
                        break;
                    }
                }
                Server.Instance.GetComputerManager().GetNodeById(ComputerId).logs.Remove(log);
            }
        }

        public void SetType(int specType)
        {
            Type = (FileType)specType;
        }

        public string[] GetLines()
        {
            return this.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
        }

        public File GetFile(string name)
        {
            foreach (File file in children)
            {
                if (file.Name == name)
                    return file;
            }
            return null;
        }

        public File GetFileAtPath(string path)
        {
            string[] pathSteps = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            File activeFolder = this;
            for (int i = 0; i < pathSteps.Length - 1; i++)
            {
                var folder = activeFolder.GetFile(pathSteps[i]);
                if (folder == null || !folder.IsFolder())
                    return null;
                activeFolder = folder;
            }
            return activeFolder.GetFile(pathSteps[pathSteps.Length - 1]);
        }

        public void PrintFolderRecursive(int depth)
        {
            string tabs = new String(' ', depth);
            Logger.Debug(tabs + id + "  d- " + Name);
            foreach (var item in children)
            {
                if (item.IsFolder())
                {
                    item.PrintFolderRecursive(depth + 1);
                }
                else
                {
                    Logger.Debug(tabs + " " + item.id + "  f- " + item.Name);
                }
            }
        }

    }
}
