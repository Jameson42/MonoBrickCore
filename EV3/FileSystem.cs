using System;
using System.Collections.Generic;
using System.IO;

namespace MonoBrick.EV3
{
    /// <summary>
    /// Class for accessing a file on the EV3
    /// </summary>
    public class BrickFile : IBrickFile, IComparable<BrickFile>
    {
        private FileType StringToFileType(string extenstion)
        {
            FileType type;
            switch (extenstion.ToLower())
            {
                /*case "":
					type = MonoBrick.FileType.Firmware;
				break;
				case "":
					type = MonoBrick.FileType.OnBrickProgram;
				break;
				case "":
					type = MonoBrick.FileType.TryMeProgram;
				break;*/
                case ".rbf":
                    type = FileType.Program;
                    break;
                case ".rsf":
                    type = FileType.Sound;
                    break;
                case ".rgf":
                    type = FileType.Graphics;
                    break;
                case ".rdf":
                    type = FileType.Datalog;
                    break;
                default:
                    type = FileType.Unknown;
                    break;
            }
            return type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrickFile"/> class.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="size">Size of the file in bytes.</param>
        /// <param name="path">Path to the location of the file.</param>
        public BrickFile(string name, uint size, string path)
        {
            Name = System.IO.Path.GetFileName(name);
            Size = size;
            Extension = System.IO.Path.GetExtension(name).ToLower();
            FileType = StringToFileType(Extension);
            Path = path;
        }

        /// <summary>
        /// Gets the full name of the file e.i. path+filename
        /// </summary>
        /// <value>The full name.</value>
        public string FullName
        {
            get
            {
                string final = Path;
                if (!final.EndsWith("/"))
                {
                    final += "/";
                }
                return final + Name;
            }

        }

        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>
        /// The name of the file
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the size
        /// </summary>
        /// <value>
        /// The size in bytes
        /// </value>
        public uint Size { get; }

        /// <summary>
        /// Gets a value indicating whether the file is empty
        /// </summary>
        /// <value>
        /// <c>true</c> if the file is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty => !Convert.ToBoolean(Size);

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        /// <value>
        /// The file extension.
        /// </value>
        public string Extension { get; }

        /// <summary>
        /// Gets the file type
        /// </summary>
        /// <value>
        /// The file type
        /// </value>
        public FileType FileType { get; }

        /// <summary>
        /// Gets the path where the file is located
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; private set; }

        /// <summary>
        /// Compares to.
        /// </summary>
        /// <returns>The to.</returns>
        /// <param name="other">Other.</param>
        public int CompareTo(BrickFile other)
        {
            return Name.CompareTo(other.Name);
        }
    }

    /// <summary>
    /// Class that holds information about a EV3 folder and it's subfolders.
    /// </summary>
    public class FolderStructure : IComparable<FolderStructure>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderStructure"/> class.
        /// </summary>
        /// <param name="path">Path of the folder.</param>
        /// <param name="isBrowsable">If set to <c>true</c> the folder is browsable.</param>
        public FolderStructure(string path, bool isBrowsable)
        {
            Path = path;
            FileList = new List<BrickFile>();
            FolderList = new List<FolderStructure>();
            IsBrowsable = isBrowsable;
        }

        /// <summary>
        /// Runs recursively through the folders structure.
        /// </summary>
        /// <returns>The next (sub)folder in the structure.</returns>
        public IEnumerable<FolderStructure> RunThroughFolders()
        {
            return RecursiveRunThrough(this);
        }

        /// <summary>
        /// Recursives run through the folder structure.
        /// </summary>
        /// <returns>A structure enumeable</returns>
        /// <param name="structure">Structure.</param>
        private IEnumerable<FolderStructure> RecursiveRunThrough(FolderStructure structure)
        {
            yield return structure;
            foreach (var child in structure.FolderList)
            {
                foreach (var f in RecursiveRunThrough(child))
                {
                    yield return f;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether folder is browsable.
        /// </summary>
        /// <value><c>true</c> if this folder is readable; otherwise, <c>false</c>.</value>
        public bool IsBrowsable { get; private set; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the file list.
        /// </summary>
        /// <value>The file list.</value>
        public List<BrickFile> FileList { get; private set; }

        /// <summary>
        /// Gets the sub folders.
        /// </summary>
        /// <value>The sub folders.</value>
        public List<FolderStructure> FolderList { get; private set; }

        /// <Docs>To be added.</Docs>
        /// <para>Returns the sort order of the current instance compared to the specified object.</para>
        /// <summary>
        /// Compares to.
        /// </summary>
        /// <returns>The to.</returns>
        /// <param name="other">Other.</param>
        public int CompareTo(FolderStructure other)
        {
            return Path.CompareTo(other.Path);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="FolderStructure"/>.
        /// </summary>
        /// <returns>A the path as a string.</returns>
        public override string ToString()
        {
            return Path;
        }

    }

    /// <summary>
    /// Fil system for the EV3 brick
    /// </summary>
    public class FilSystem
    {
        private const int MaxBytesToWrite = 50;
        private const int MaxBytestoRead = 100;
        private const uint MaxBytesInFileList = 1000;

        internal Connection<Command, Reply> Connection { get; set; }

        /// <summary>
        /// Begins the download from you to the EV3
        /// </summary>
        /// <returns>The download.</returns>
        /// <param name="fileLength">File length.</param>
        /// <param name="filename">Filename relative to /home/root/lms2012/sys</param>
        private byte BeginWrite(uint fileLength, string filename)
        {
            var command = new Command(SystemCommand.BeginDownload, 100, true);
            command.Append(fileLength);
            command.Append(filename);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, 6, 100);
            return reply.GetByte(5);
        }

        /// <summary>
        /// Continues writing files.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <param name="data">Data.</param>
        /// <param name="startIdx">Start index.</param>
        /// <param name="length">Length.</param>
        private void ContinueWrite(byte handle, byte[] data, int startIdx, int length)
        {
            var command = new Command(SystemCommand.ContinueDownload, 101, true);
            command.Append(handle);
            command.Append(data, startIdx, length);
            var reply = Connection.SendAndReceive(command);
            if (reply.HasError)
            {
                Error.ThrowException(reply);
            }

        }

        private void WriteCompleteArray(string fileName, byte[] data)
        {
            int bytesWritten = 0;
            int bytesToWrite = data.Length;
            try
            {
                byte handle = BeginWrite((uint)data.Length, fileName);
                while (bytesWritten < bytesToWrite)
                {
                    if ((bytesToWrite - bytesWritten) >= MaxBytesToWrite)
                    {
                        ContinueWrite(handle, data, bytesWritten, MaxBytesToWrite);
                        bytesWritten += MaxBytesToWrite;
                    }
                    else
                    {
                        ContinueWrite(handle, data, bytesWritten, (bytesToWrite - bytesWritten));
                        bytesWritten = bytesToWrite + (bytesToWrite - bytesWritten);
                    }
                }
            }
            catch (BrickException e)
            {
                //CloseFile (handle);
                throw e;
            }
            //CloseFile (handle);
        }

        private byte[] BeginRead(ushort bytesToRead, string filePath, out byte handle, out uint fileSize)
        {
            var command = new Command(SystemCommand.BeginUpload, 102, true);
            command.Append(bytesToRead);
            command.Append(filePath);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, 102);
            fileSize = reply.GetUInt32(5);
            handle = reply.GetByte(9);
            return reply.GetData(10);
        }

        private byte[] ContinueRead(byte handle, ushort bytesToRead)
        {
            var command = new Command(SystemCommand.ContinueUpload, 103, true);
            command.Append(handle);
            command.Append(bytesToRead);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, bytesToRead + 6, 103);
            if (handle != reply[5])
            {
                throw new BrickException(BrickError.UnknownHandle);
            }
            return reply.GetData(6);
        }

        private byte[] ReadWholeFile(string fileName)
        {
            byte[] dataRead = BeginRead(MaxBytestoRead, fileName, out byte handle, out uint fileSize);
            byte[] fileData = new byte[fileSize];
            Array.Copy(dataRead, fileData, dataRead.Length);
            uint bytesRead = (uint)dataRead.Length;
            while (bytesRead < fileSize)
            {
                if ((fileSize - bytesRead) >= MaxBytestoRead)
                {
                    dataRead = ContinueRead(handle, MaxBytestoRead);
                    Array.Copy(dataRead, 0, fileData, bytesRead, dataRead.Length);
                    bytesRead += MaxBytestoRead;
                }
                else
                {
                    dataRead = ContinueRead(handle, (ushort)(fileSize - bytesRead));
                    Array.Copy(dataRead, 0, fileData, bytesRead, dataRead.Length);
                    bytesRead += fileSize - bytesRead;
                }
            }
            return fileData;
        }

        private void CloseFile(byte handle)
        {
            var command = new Command(SystemCommand.CloseFileHandle, 104, true);
            command.Append(handle);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, 6, 104);
        }

        private byte[] OpenHandles()
        {
            var reply = Connection.SendAndReceive(new Command(SystemCommand.ListOpenHandles, 108, true));
            Error.CheckForError(reply, 108);
            return reply.GetData(5);
        }

        private string ListFiles(string path, ushort length)
        {
            var command = new Command(SystemCommand.ListFiles, 109, true);
            command.Append(length);
            command.Append(path);
            Connection.Send(command);
            var reply = Connection.Receive();
            Error.CheckForError(reply, 109);
            return reply.GetString(10);
        }

        /// <summary>
        /// Gets the folder info.
        /// </summary>
        /// <param name="path">Path to folder to read</param>
        /// <param name="files">Files in folder</param>
        /// <param name="subFoldersNames">Sub folders names</param>
        public void GetFolderInfo(string path, out BrickFile[] files, out string[] subFoldersNames)
        {
            //Console.WriteLine("Get folder info at " +  path);
            var fileList = new List<BrickFile>();
            var folderList = new List<string>();
            string list = ListFiles(path, (ushort)MaxBytesInFileList);
            //Console.Write("Raw string: " + list);
            string[] element = list.Split(Environment.NewLine.ToCharArray());
            foreach (string s in element)
            {
                //Console.WriteLine("Element:" + s);
                if (s.EndsWith("/"))
                {
                    try
                    {
                        if (s != "../" && s != "./" && s != "/../" && s != "/./")
                        {
                            folderList.Add("/" + s.Remove(s.Length - 1));
                            //Console.WriteLine("Adding folder: "  + "/" + s.Remove(s.Length-1));
                        }
                    }
                    catch
                    {
                        //Console.WriteLine("Failed to add folder");	
                    }
                }
                else
                {
                    string[] fileInfo = s.Split(' ');
                    if (fileInfo.Length > 1)
                    {
                        try
                        {
                            byte[] sizeBytes = System.Text.Encoding.ASCII.GetBytes(fileInfo[1]);
                            uint fileSize = BitConverter.ToUInt32(sizeBytes, 0);
                            fileList.Add(new BrickFile(fileInfo[2], fileSize, path));
                            //Console.WriteLine("Adding file: "  + fileInfo[2] +  " with size " + fileSize);
                        }
                        catch
                        {
                            //Console.WriteLine("Failed to file");			
                        }
                    }
                }
            }
            files = fileList.ToArray();
            subFoldersNames = folderList.ToArray();
        }

        /// <summary>
        /// Get a complete folder structure
        /// </summary>
        /// <returns>The folder structure</returns>
        /// <param name="path">Path where you want the folder structure to start</param>
        public FolderStructure GetFolderStructure(string path)
        {
            FolderStructure folderStructure;
            try
            {
                GetFolderInfo(path, out BrickFile[] files, out string[] folders);
                folderStructure = new FolderStructure(path, true);
                foreach (var file in files)
                {
                    folderStructure.FileList.Add(file);
                }
                for (int i = 0; i < folders.Length; i++)
                {
                    string newPath = "";
                    if (path.EndsWith("/"))
                    {
                        newPath = path.Remove(path.Length - 1) + folders[i];
                    }
                    else
                    {
                        newPath = path + folders[i];
                    }
                    folderStructure.FolderList.Add(GetFolderStructure(newPath));
                }
            }
            catch
            {
                folderStructure = new FolderStructure(path, false);
            }
            if (folderStructure.FolderList.Count > 0)
                folderStructure.FolderList.Sort();
            if (folderStructure.FileList.Count > 0)
                folderStructure.FileList.Sort();
            return folderStructure;
        }

        /// <summary>
        /// Read a file from the EV3
        /// </summary>
        /// <param name="file">File to download.</param>
        /// <param name="destinationFileName">Destination file name.</param>
        public void ReadFile(BrickFile file, string destinationFileName)
        {
            ReadFile(file.Name, destinationFileName);
        }

        /// <summary>
        /// Read a file from the EV3
        /// </summary>
        /// <param name="brickFileName">Brick file name.</param>
        /// <param name="destinationFileName">Destination file name.</param>
        public void ReadFile(string brickFileName, string destinationFileName)
        {
            byte[] fileData = ReadWholeFile(brickFileName);
            FileStream fileStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write);
            fileStream.Write(fileData, 0, fileData.Length);
            fileStream.Close();
        }

        ///<summary>
        /// Write a file to the EV3
        /// </summary>
        /// <param name='fileToWrite'>
        /// PC file to write to brick
        /// </param>
        /// <param name='brickFileName'>
        /// File name that the file should have on the brick including file path example: /home/root/lms2012/prjs/someFileName.txt
        /// </param>
        public void WriteFile(string fileToWrite, string brickFileName)
        {
            FileStream fileStream = new FileStream(fileToWrite, System.IO.FileMode.Open, FileAccess.Read);
            byte[] fileData = new byte[fileStream.Length];
            fileStream.Read(fileData, 0, (int)fileStream.Length);
            fileStream.Close();
            WriteCompleteArray(brickFileName, fileData);
        }

        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <param name="dir">Full path of directory to create.</param>
        public void CreateDirectory(string dir)
        {
            var command = new Command(SystemCommand.CreateDir, 105, true);
            command.Append(dir);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, 5, 105);
        }

        /// <summary>
        /// Deletes a file on the EV3.
        /// </summary>
        /// <param name="file">File to delete.</param>
        public void DeleteFile(BrickFile file)
        {
            DeleteFile(file.FullName);
        }

        /// <summary>
        /// Deletes a file on the EV3.
        /// </summary>
        /// <param name="fileName">File name to delete.</param>
        public void DeleteFile(string fileName)
        {
            var command = new Command(SystemCommand.DeleteFile, 106, true);
            command.Append(fileName);
            var reply = Connection.SendAndReceive(command);
            Error.CheckForError(reply, 5, 106);
        }
    }
}

