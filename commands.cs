using System;
using System.Text;
using System.Collections.Generic;

namespace MonoBrick
{
    /// <summary>
    /// Class for creating a command
    /// </summary>
    public abstract class BrickCommand
    {
        /// <summary>
        /// A list that holds the data bytes of the command
        /// </summary>
        protected List<byte> dataBytes = new List<byte>();

        /// <summary>
        /// Does the command require a reply
        /// </summary>
        protected bool replyRequired;

        /// <summary>
        /// Append a boolean value
        /// </summary>
        public void Append(bool value) => Append(value ? (byte)0x01 : (byte)0x00);

        /// <summary>
        /// Append a string
        /// </summary>
        public void Append(string value)
        {
            for (int i = 0; i < value?.Length; i++)
            {
                Append((byte)value[i]);
            }
            Append(0x00);
        }

        /*public void Append(String s , int maxSize){
			Append(s, maxSize, false);
		}*/

        /// <summary>
        /// Append a string
        /// </summary>
        /// <param name='padWithZero'>
        /// If set to <c>true</c> and string length is less that maxsize the remaining bytes will be padded with zeros
        /// If set to <c>false</c> and string length is less that maxsize no padding will be added
        /// </param>
        public void Append(string value, int maxSize, bool padWithZero)
        {
            if (value?.Length > maxSize)
                value = value.Remove(maxSize);
            if (padWithZero && !(value?.Length == maxSize))
            {
                value += new string((char)0, maxSize - (value?.Length ?? 0));
            }
            Append(value);
        }

        /// <summary>
        /// Append a byte
        /// </summary>
        public void Append(byte value) => dataBytes.Add(value);

        /// <summary>
        /// Append a signed byte
        /// </summary>
        public void Append(sbyte value) => Append((byte)value);

        /// <summary>
        /// Append a UInt16
        /// </summary>
        public void Append(ushort value)
        {
            Append(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Append a Int16
        /// </summary>
        public void Append(short value)
        {
            Append(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Append a UInt32
        /// </summary>
        public void Append(uint value)
        {
            Append(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Append a Int32
        /// </summary>
        public void Append(int value)
        {
            Append(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Append a float
        /// </summary>
        public void Append(float value)
        {
            Append(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Append a byte array
        /// </summary>
        public void Append(byte[] value)
        {
            Append(value, 0, value.Length);
        }

        /// <summary>
        /// Append a byte array
        /// </summary>
        /// <param name='offset'>
        /// The byte array offset
        /// </param>
        public void Append(byte[] value, int offset)
        {
            Append(value, offset, value.Length);
        }

        /// <summary>
        /// Append a byte array
        /// </summary>
        /// <param name='offset'>
        /// The byte array offset
        /// </param>
        /// <param name='length'>
        /// The length to append
        /// </param>
        public void Append(byte[] value, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Append(value[i + offset]);
            }
        }

        /// <summary>
        /// Appends zeros 
        /// </summary>
        public void AppendZeros(int numberOfZeros)
        {
            for (int i = 0; i < numberOfZeros; i++)
            {
                Append(0);
            }
        }

        /// <summary>
        /// A value indicating whether a reply is required.
        /// </summary>
        public bool ReplyRequired => replyRequired;

        /// <summary>
        /// Byte array of the command
        /// </summary>
        /// <value>
        /// The command data
        /// </value>
        public byte[] Data => dataBytes.ToArray();

        /// <summary>
        /// Length of the command
        /// </summary>
        public int Length => dataBytes.Count;

        internal static string AddSpacesToString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }


    }

    /// <summary>
    /// Class holding a reply from the brick
    /// </summary>
    public class BrickReply
    {
        /// <summary>
        /// The data array used for the reply
        /// </summary>
        protected byte[] dataArray;

        /// <summary>
        /// Set the payload data of the reply
        /// </summary>
        public void SetData(byte[] data)
        {
            dataArray = data;
        }

        /// <summary>
        /// The length
        /// </summary>
        public int Length => dataArray.Length;

        /// <summary>
        /// The data byte at i.
        /// </summary>
        public byte this[int index] => dataArray[index];

        /// <summary>
        /// Read the data of the reply
        /// </summary>
        public byte[] Data => (byte[])dataArray.Clone();

        /// <summary>
        /// Read a string
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public string GetString(byte offset)
        {
            byte size = 0;
            while (offset + size < dataArray.Length && dataArray[offset + size] != 0)
            {
                size++;
            }
            if (offset + size > dataArray.Length)
            {
                return "";
            }
            return GetString(offset, size);
        }

        /// <summary>
        /// Read a string
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        /// <param name='length'>
        /// Length of the string to read
        /// </param>
        public string GetString(byte offset, byte length)
        {
            return Encoding.ASCII.GetString(dataArray, offset, length);
        }

        /// <summary>
        /// Gets raw command bytes
        /// </summary>
        /// <param name='offset'>
        /// Offset 
        /// </param>
        public byte[] GetData(int offset)
        {
            byte[] a = null;
            if (offset <= dataArray.Length)
            {
                int newSize = dataArray.Length - offset;
                a = new byte[newSize];
                for (int i = 0; i < newSize; i++)
                {
                    a[i] = dataArray[i + offset];
                }
            }
            return a;
        }

        /// <summary>
        /// Read a signed byte 
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public sbyte GetSbyte(int offset)
        {
            return (sbyte)this[offset];
        }

        /// <summary>
        /// Read a byte
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public byte GetByte(int offset)
        {
            return this[offset];
        }

        /// <summary>
        /// Read a UInt16
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public ushort GetUInt16(int offset)
        {
            return BitConverter.ToUInt16(dataArray, offset);
        }

        /// <summary>
        /// Read a Int16
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public short GetInt16(int offset)
        {
            return BitConverter.ToInt16(dataArray, offset);
        }

        /// <summary>
        /// Read a UInt32
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public uint GetUInt32(int offset)
        {
            return BitConverter.ToUInt32(dataArray, offset);
        }

        /// <summary>
        /// Read a Int32
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public int GetInt32(int offset)
        {
            return BitConverter.ToInt32(dataArray, offset);
        }

        /// <summary>
        /// Read a float
        /// </summary>
        /// <param name='offset'>
        /// Where to start reading
        /// </param>
        public float GetFloat(int offset)
        {
            return BitConverter.ToSingle(dataArray, offset);
        }
    }
}