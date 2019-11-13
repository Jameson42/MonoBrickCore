using System;
using System.Collections.Generic;
using System.IO;

namespace MonoBrick.NXT
{
    /// <summary>
    /// File mode
    /// </summary>
    public enum FileMode : byte
    {
        /// <summary>
        /// File is fragmented
        /// </summary>
        Fragmented = CommandByte.OpenWrite,
        /// <summary>
        /// File is not fragmented
        /// </summary>
        NoneFragmented = CommandByte.OpenWriteLinear,
        /// <summary>
        /// The file can be closed before the whole file is written	
        /// </summary>
        Data = CommandByte.OpenWriteData
    }


    /// <summary>
    /// A Brick file
    /// </summary>
    public class BrickFile : IBrickFile
    {
        private FileType StringToNXTFileType(string extenstion)
        {
            FileType type;
            switch (extenstion.ToLower())
            {
                case ".rfw":
                    type = FileType.Firmware;
                    break;
                case ".rxe":
                    type = FileType.Program;
                    break;
                case ".rpg":
                    type = FileType.OnBrickProgram;
                    break;
                case ".rtm":
                    type = FileType.TryMeProgram;
                    break;
                case ".rso":
                    type = FileType.Sound;
                    break;
                case ".ric":
                    type = FileType.Graphics;
                    break;
                case ".rdt":
                    type = FileType.Datalog;
                    break;
                default:
                    type = FileType.Unknown;
                    break;
            }
            return type;
        }

        /// <summary>
        /// Initializes a new instance of the NXTBrickFile class.
        /// </summary>
        /// <param name='name'>
        /// The name of the file
        /// </param>
        /// <param name='handle'>
        /// The file handle
        /// </param>
        /// <param name='size'>
        /// The size of the file in bytes
        /// </param>
        public BrickFile(string name, byte handle, uint size)
        {
            Name = name;
            Handle = handle;
            Size = size;
            Extension = Path.GetExtension(name).ToLower();
            FileType = StringToNXTFileType(Extension);
        }

        /// <summary>
        /// Initializes a empty instance of the NXTBrickFile class.
        /// </summary>
        public BrickFile()
        {
            Name = "";
            Size = 0;
            Handle = 0;
            Extension = "";
            FileType = FileType.Unknown;
        }

        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>
        /// The name of the file
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the file handle.
        /// </summary>
        /// <value>
        /// The file handle
        /// </value>
        public byte Handle { get; }

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
    }

    /// <summary>
    /// Fil system for the NXT brick
    /// </summary>
    public class FileSystem
    {
        /// <summary>
        /// The max number of bytes for a file name.
        /// </summary>
        public static byte MaxFileNameLength = 19;
        private const int MaxBytesWrite = 50;
        private const int MaxBytesRead = 50;

        /// <summary>
        /// Occurs when bytes are read.
        /// </summary>
        public event Action<int> OnBytesRead;

        /// <summary>
        /// Occurs when bytes are written.
        /// </summary>
        public event Action<int> OnBytesWritten;

        /// <summary>
        /// Occurs when flash is deleted.
        /// </summary>
        public event Action OnFlashDeleted;
        internal Connection<Command, Reply> Connection { get; set; }

        private BrickFile FindFirst(string wildCard)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.FindFirst, true);
            command.Append(wildCard, MaxFileNameLength, true);
            Connection.Send(command);
            //command.Print();
            var reply = Connection.Receive();
            if (reply.HasError && reply.ErrorCode != (byte)BrickError.FileNotFound)
            {
                Error.ThrowException(reply);
            }
            if (reply.ErrorCode == (byte)BrickError.FileNotFound)
            {
                return new BrickFile();//empty file
            }
            if (reply.Length != 28)
            {
                throw new BrickException(BrickError.WrongNumberOfBytes);
            }
            BrickFile newFile = new BrickFile(reply.GetString(4), reply[3], reply.GetUInt32(24));
            try
            {
                CloseFile(newFile);
            }
            catch (MonoBrickException e)
            {
                if (e.ErrorCode != (byte)BrickError.HandleAlreadyClosed)
                    Error.ThrowException(e.ErrorCode);
            }
            return newFile;
        }

        private BrickFile FindNext(BrickFile file)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.FindNext, true);
            command.Append(file.Handle);
            Connection.Send(command);
            var reply = Connection.Receive();
            if (reply.HasError && reply.ErrorCode != (byte)BrickError.FileNotFound)
            {
                Error.ThrowException(reply);
            }
            if (reply.ErrorCode == (byte)BrickError.FileNotFound)
            {
                return new BrickFile();//empty file
            }
            if (reply.Length != 28)
            {
                throw new BrickException(BrickError.WrongNumberOfBytes);
            }
            BrickFile newFile = new BrickFile(reply.GetString(4), reply[3], reply.GetUInt32(24));
            try
            {
                CloseFile(newFile);
            }
            catch (MonoBrickException e)
            {
                if (e.ErrorCode != (byte)BrickError.HandleAlreadyClosed)
                    Error.ThrowException(e.ErrorCode);
            }
            return newFile;
        }

        /// <summary>
        /// Get a list of files on the brick
        /// </summary>
        /// <returns>
        /// The list of files
        /// </returns>
        public BrickFile[] FileList()
        {
            return FileList("*.*");
        }

        /// <summary>
        /// Get a list of files on the brick
        /// </summary>
        /// <returns>
        /// The list of files
        /// </returns>
        /// <param name='wildCard'>
        /// Wild card to list i.e. *.rso; *.rxe... 
        /// </param>
        public BrickFile[] FileList(string wildCard)
        {
            BrickFile newFile;
            List<BrickFile> list = new List<BrickFile>();
            newFile = FindFirst(wildCard);
            if (newFile.IsEmpty)
            {
                return list.ToArray();
            }
            list.Add(newFile);
            CloseFile(newFile);
            do
            {
                newFile = FindNext(newFile);
                if (!newFile.IsEmpty)
                {
                    list.Add(newFile);
                    CloseFile(newFile);
                }
            } while (!newFile.IsEmpty);
            return list.ToArray();
        }

        /// <summary>
        /// Write a byte array to a file
        /// </summary>
        /// <param name='file'>
        /// The file
        /// </param>
        /// <param name='data'>
        /// Data to write
        /// </param>
        public int Write(BrickFile file, byte[] data)
        {
            int bytesWritten = 0;
            while (bytesWritten < data.Length)
            {
                if ((data.Length - bytesWritten) >= MaxBytesWrite)
                {
                    if (Write(file, data, bytesWritten, MaxBytesWrite) != MaxBytesWrite)
                    {
                        CloseFile(file);
                        throw new BrickException(BrickError.UndefinedFileError);
                    }
                    bytesWritten += MaxBytesRead;
                }
                else
                {
                    int bytesToWrite = (data.Length - bytesWritten);
                    if (Write(file, data, bytesWritten, bytesToWrite) != bytesToWrite)
                    {
                        CloseFile(file);
                        throw new BrickException(BrickError.UndefinedFileError);
                    }
                    bytesWritten += bytesToWrite;
                }
            }
            return bytesWritten;
        }

        private int Write(BrickFile file, byte[] data, int offset, int length)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.Write, true);
            command.Append(file.Handle);
            command.Append(data, offset, length);
            Connection.Send(command);
            //Console.WriteLine(file.Size);
            //command.Print();
            var reply = Connection.Receive();
            Error.CheckForError(reply, 6, delegate () { CloseFile(file); });
            int result = (int)reply.GetUInt16(4);//The number of bytes written
            OnBytesWritten?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Read all bytes from a file
        /// </summary>
        /// <param name='file'>
        /// The file to read from
        /// </param>
        public byte[] Read(BrickFile file)
        {
            byte[] data = new byte[file.Size];
            int bytesRead = 0;
            while (bytesRead < file.Size)
            {
                byte[] newData;
                if ((file.Size - bytesRead) >= MaxBytesRead)
                {
                    newData = Read(file, MaxBytesRead);
                    Array.Copy(newData, 0, data, bytesRead, MaxBytesRead);
                    if (newData.Length != MaxBytesRead)
                    {
                        CloseFile(file);
                        throw new BrickException(BrickError.UndefinedFileError);
                    }
                }
                else
                {
                    var bytesToRead = file.Size - (uint)bytesRead;
                    newData = Read(file, (ushort)bytesToRead);
                    Array.Copy(newData, 0, data, bytesRead, bytesToRead);
                    if (newData.Length != bytesToRead)
                    {
                        CloseFile(file);
                        throw new BrickException(BrickError.UndefinedFileError);
                    }
                }
                bytesRead += newData.Length;

            }
            return data;
        }

        /// <summary>
        /// Read a specefic number of bytes from a file
        /// </summary>
        /// <param name='file'>
        /// The file to read from
        /// </param>
        /// <param name='bytesToRead'>
        /// Bytes to read
        /// </param>
        public byte[] Read(BrickFile file, ushort bytesToRead)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.Read, true);
            command.Append(file.Handle);
            command.Append(bytesToRead);
            Connection.Send(command);
            var reply = Connection.Receive();
            if (reply.HasError)
            {
                CloseFile(file);
                Error.ThrowException(reply);
            }
            //UInt16 bytesRead = reply.GetUInt16(4); //use this for some error checking
            byte[] result = reply.GetData(6);
            OnBytesRead?.Invoke(result.Length);
            return result;
        }

        /// <summary>
        /// Opens file for write in fragmented mode. If file exist it is overwritten
        /// </summary>
        /// <returns>
        /// The file
        /// </returns>
        /// <param name='fileName'>
        /// The name of the file
        /// </param>
        /// <param name='fileSize'>
        /// The size of the file
        /// </param>
        public BrickFile OpenWrite(string fileName, uint fileSize)
        {
            return OpenWrite(fileName, fileSize, FileMode.Fragmented);
        }

        /// <summary>
        /// Opens file for write. If file exist it is overwritten
        /// </summary>
        /// <returns>
        /// The file.
        /// </returns>
        /// <param name='fileName'>
        /// The name of the file
        /// </param>
        /// <param name='fileSize'>
        /// File size in bytes
        /// </param>
        /// <param name='fileType'>
        /// File mode when opening
        /// </param>
        public BrickFile OpenWrite(string fileName, uint fileSize, FileMode fileType)
        {
            var command = new Command(CommandType.SystemCommand, (CommandByte)fileType, true);
            command.Append(fileName, MaxFileNameLength, true);
            command.Append(fileSize);
            Connection.Send(command);
            var reply = Connection.Receive();
            Error.CheckForError(reply, 4);
            if (fileName.Length > MaxFileNameLength)
                fileName.Remove(MaxFileNameLength);
            return new BrickFile(fileName, reply[3], fileSize);

        }

        /// <summary>
        /// Opens file for read.
        /// </summary>
        /// <returns>
        /// The file
        /// </returns>
        /// <param name='fileName'>
        /// File name
        /// </param>
        public BrickFile OpenRead(string fileName)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.OpenRead, true);
            command.Append(fileName, MaxFileNameLength, true);
            Connection.Send(command);
            var reply = Connection.Receive();
            Error.CheckForError(reply, 8);
            if (fileName.Length > MaxFileNameLength)
                fileName.Remove(MaxFileNameLength);
            return new BrickFile(fileName, reply[3], reply.GetUInt32(4));

        }

        /// <summary>
        /// Opens file to append data
        /// </summary>
        /// <returns>
        /// The file
        /// </returns>
        /// <param name='fileName'>
        /// File name
        /// </param>
        public BrickFile OpenAppend(string fileName)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.OpenAppendData, true);
            command.Append(fileName, MaxFileNameLength, true);
            Connection.Send(command);
            var reply = Connection.Receive();
            Error.CheckForError(reply, 8);
            if (fileName.Length > MaxFileNameLength)
                fileName.Remove(MaxFileNameLength);
            return new BrickFile(fileName, reply[3], reply.GetUInt32(4));
        }

        /// <summary>
        /// Closes the file.
        /// </summary>
        /// <param name='file'>
        /// File to close
        /// </param>
        public void CloseFile(BrickFile file)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.Close, true);
            command.Append(file.Handle);
            Connection.Send(command);
            var reply = Connection.Receive();
            if (reply.HasError && reply.ErrorCode != (byte)BrickError.HandleAlreadyClosed)
            {
                Error.ThrowException(reply);
            }
            //compare the handle numbers
        }

        /// <summary>
        /// Deletes file.
        /// </summary>
        /// <param name='file'>
        /// File to delete
        /// </param>
        public void DeleteFile(BrickFile file)
        {
            DeleteFile(file.Name);
        }

        /// <summary>
        /// Deletes file.
        /// </summary>
        /// <param name='fileName'>
        /// Name of file to delete
        /// </param>
        public void DeleteFile(string fileName)
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.Delete, true);
            command.Append(fileName, MaxFileNameLength, true);
            Connection.Send(command);
            var reply = Connection.Receive();
            if (reply.HasError)//dont't know the length of the reply compare the file name
                Error.ThrowException(reply.ErrorCode);
        }

        /// <summary>
        /// Deletes flash memory
        /// </summary>
        public void DeleteFlash()
        {
            var command = new Command(CommandType.SystemCommand, CommandByte.DeleteUserFlash, true);
            Connection.Send(command);
            var reply = Connection.Receive();
            Error.CheckForError(reply, 3);
            OnFlashDeleted?.Invoke();
        }

        /// <summary>
        /// Gets the size of the free flash memory in bytes
        /// </summary>
        /// <returns>
        /// The amount of free flash in bytes
        /// </returns>
        public uint GetFreeFlashMemory()
        {
            var reply = Connection.SendAndReceive(new Command(CommandType.SystemCommand, CommandByte.GetDeviceInfo, true));
            Error.CheckForError(reply, 33);
            return reply.GetUInt32(29);
        }

        /// <summary>
        /// Upload a file to the brick
        /// </summary>
        /// <param name='fileToUpload'>
        /// File name that should be uploaded to the brick 
        /// </param>
        /// <param name='brickFileName'>
        /// File name that the file will have on the brick
        /// </param>
        public void UploadFile(string fileToUpload, string brickFileName)
        {
            uint maxSize = GetFreeFlashMemory();
            FileStream fileStream = new FileStream(fileToUpload, System.IO.FileMode.Open, FileAccess.Read);
            if (fileStream.Length >= maxSize)
            {
                throw new BrickException(BrickError.NoSpace);
            }
            byte[] fromFile = new byte[fileStream.Length];
            fileStream.Read(fromFile, 0, (int)fileStream.Length);
            string extension = Path.GetExtension(fileToUpload);
            fileStream.Close();
            BrickFile nxtFile;
            if (extension.ToLower() == ".rxe" || extension.ToLower() == ".ric")
            {
                nxtFile = OpenWrite(brickFileName, (uint)fromFile.Length, FileMode.NoneFragmented);
            }
            else
            {
                nxtFile = OpenWrite(brickFileName, (uint)fromFile.Length, FileMode.Fragmented);
            }
            Write(nxtFile, fromFile);
            CloseFile(nxtFile);
        }

        /// <summary>
        /// Downloads a file from the brick
        /// </summary>
        /// <param name='destinationFileName'>
        /// Destination file name
        /// </param>
        /// <param name='brickFile'>
        /// Brick file to download
        /// </param>
        public void DownloadFile(string destinationFileName, BrickFile brickFile)
        {
            DownloadFile(destinationFileName, brickFile.Name);
        }

        /// <summary>
        /// Downloads a file from the brick
        /// </summary>
        /// <param name='destinationFileName'>
        /// Destination file name
        /// </param>
        /// <param name='brickFileName'>
        /// The name of the file on the brick to download
        /// </param>
        public void DownloadFile(string destinationFileName, string brickFileName)
        {
            FileStream fileStream = null;
            BrickFile nxtFile = null;
            try
            {
                nxtFile = OpenRead(brickFileName);
                fileStream = new FileStream(destinationFileName, System.IO.FileMode.Create, FileAccess.Write);
                fileStream.Write(Read(nxtFile), 0, (ushort)nxtFile.Size);
                fileStream.Close();
                CloseFile(nxtFile);
            }
            catch (Exception e)
            {
                if (fileStream != null)
                    fileStream.Close();
                if (nxtFile != null)
                    CloseFile(nxtFile);
                throw (e);
            }
        }

    }
}

