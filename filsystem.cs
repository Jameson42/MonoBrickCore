namespace MonoBrick
{
    /// <summary>
    /// File types
    /// </summary>
    public enum FileType
    {
        Firmware, Program, OnBrickProgram, TryMeProgram, Sound, Graphics, Datalog, Unknown
    }
    /// <summary>
    /// Interface for a file placed on the brick
    /// </summary>
    public interface IBrickFile
    {
        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>The name of the file</value>
        string Name { get; }

        /// <summary>
        /// Gets the size of the file in bytes
        /// </summary>
        /// <value>The size of the file in bytes</value>
        uint Size { get; }

        /// <summary>
        /// Gets the file extension
        /// </summary>
        /// <value>The file extension.</value>
        string Extension { get; }

        /// <summary>
        /// Gets the file type
        /// </summary>
        /// <value>The file type</value>
        FileType FileType { get; }
    }
}

