using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Os.Compare.Directories;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Dlls;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Process;
using Musoq.Schema.Os.Zip;

namespace Musoq.Schema.Os
{
    /// <summary>
    /// Provides schema to work with operating system abstractions
    /// </summary>
    public class OsSchema : SchemaBase
    {
        private const string SchemaName = "os";
        private const string DirectoriesTable = "directories";
        private const string FilesTable = "files";
        private const string DllsTable = "dlls";
        private const string ZipTable = "zip";
        private const string ProcessesName = "processes";
        private const string DirsCompare = "dirscompare";
        private const string Single = "single";

        /// <virtual-constructors>
        /// <virtual-constructor>
        /// <virtual-param>Path of the given file</virtual-param>
        /// <examples>
        /// <example>
        /// <from>from #os.dirscompare('dir1', 'dir2')</from>
        /// <description>Compares two directories</description>
        /// <columns>
        /// <column name="SourceFile" type="ExtendedFileInfo">Source file</column>
        /// <column name="DestinationFile" type="ExtendedFileInfo">Destination file</column>
        /// <column name="State" type="string">The Same / Modified / Added / Removed</column>
        /// <column name="SourceRoot" type="DirectoryInfo">Source directory</column>
        /// <column name="DestinationRoot" type="DirectoryInfo">Destination directory</column>
        /// <column name="SourceFileRelative" type="string">Relative path to source file</column>
        /// <column name="DestinationFileRelative" type="string">Relative path to destination file</column>
        /// </columns>
        /// </example>
        /// <example>
        /// <from>from #os.directories('dir1', true)</from>
        /// <description>Gets the directories</description>
        /// <columns>
        /// <column name="FullName" type="string">Full name of the directory</column>
        /// <column name="Attributes" type="FileAttributes">Directory attributes</column>
        /// <column name="CreationTime" type="DateTime">Creation time</column>
        /// <column name="CreationTimeUtc" type="DateTime">Creation time in UTC</column>
        /// <column name="Exists" type="bool">Determine does the directory exists</column>
        /// <column name="Extension" type="string">Gets the extension part of the file name</column>
        /// <column name="LastAccessTime" type="DateTime">Gets the time the current file or directory was last accessed</column>
        /// <column name="LastAccessTimeUtc" type="DateTime">Gets the time, in coordinated universal time (UTC), that the current file or directory was last accessed</column>
        /// <column name="Name" type="string">Gets the directory name</column>
        /// <column name="LastWriteTime" type="DateTime">Gets the date when the file or directory was written to</column>
        /// <column name="Parent" type="DirectoryInfo">Gets the parent directory</column>
        /// <column name="Root" type="DirectoryInfo">Gets the root directory</column>
        /// <column name="DirectoryInfo" type="DirectoryInfo">Gets raw DirectoryInfo</column>
        /// </columns>
        /// </example>
        /// <example>
        /// <from>from #os.dlls('dir1')</from>
        /// <description>Gets the dlls</description>
        /// <columns>
        /// <column name="FileInfo" type="FileInfo">Gets the metadata about the DLL file</column>
        /// <column name="Assembly" type="Assembly">Gets the Assembly object</column>
        /// <column name="Version" type="FileVersionInfo">Gets the assembly version</column>
        /// </columns>
        /// </example>
        /// <example>
        /// <from>from #os.files('path', false)</from>
        /// <description>Gets the files</description>
        /// <columns>
        /// <column name="Name" type="string">Full name of the directory</column>
        /// <column name="CreationTime" type="DateTime">Creation time</column>
        /// <column name="CreationTimeUtc" type="DateTime">Creation time in UTC</column>
        /// <column name="DirectoryName" type="string">Gets the directory name</column>
        /// <column name="Extension" type="string">Gets the extension part of the file name</column>
        /// <column name="FullName" type="string">Gets the full path of file</column>
        /// <column name="Exists" type="bool">Determine whether file exists or not</column>
        /// <column name="IsReadOnly" type="bool">Determine whether the file is readonly</column>
        /// <column name="Length" type="long">Gets the length of file</column>
        /// </columns>
        /// </example>
        /// <example>
        /// <from>from #os.processes()</from>
        /// <description>Gets the processes</description>
        /// <columns>
        /// <column name="BasePriority" type="int">Gets the base priority of associated process</column>
        /// <column name="EnableRaisingEvents" type="bool">Gets whether the exited event should be raised when the process terminates</column>
        /// <column name="ExitCode" type="int">Gets the value describing process termination</column>
        /// <column name="ExitTime" type="DateTime">Exit time in UTC</column>
        /// <column name="Handle" type="IntPtr">Gets the native handle of the associated process</column>
        /// <column name="HandleCount" type="int">Gets the number of handles opened by the process</column>
        /// <column name="HasExited" type="bool">Gets a value indicating whether the associated process has been terminated</column>
        /// <column name="Id" type="int">Gets the unique identifier for the associated process</column>
        /// <column name="MachineName" type="string">Gets the name of the computer the associated process is running on</column>
        /// <column name="MainWindowTitle" type="string">Gets the caption of the main window of the process</column>
        /// <column name="PagedMemorySize64" type="long">Gets a value indicating whether the user interface of the process is responding</column>
        /// <column name="ProcessName" type="string">The name that the system uses to identify the process to the user</column>
        /// <column name="ProcessorAffinity" type="IntPtr">Gets the processors on which the threads in this process can be scheduled to run</column>
        /// <column name="Responding" type="bool">Gets a value indicating whether the user interface of the process is responding</column>
        /// <column name="StartTime" type="DateTime">Gets the time that the associated process was started</column>
        /// <column name="TotalProcessorTime" type="TimeSpan">Gets the total processor time for this process</column>
        /// <column name="UserProcessorTime" type="TimeSpan">Gets the user processor time for this process</column>
        /// <column name="Directory" type="string">Gets the directory of the process</column>
        /// <column name="FileName" type="string">Gets the filename of the process</column>
        /// </columns>
        /// </example>
        /// <example>
        /// <from>from #os.zip('zipPath')</from>
        /// <description>Gets the zip files</description>
        /// <columns>
        /// <column name="Name" type="string">Gets the file name of the entry in the zip archive</column>
        /// <column name="FullName" type="string">Gets the relative path of the entry in the zip archive</column>
        /// <column name="CompressedLength" type="long">Gets the compressed size of the entry in the zip archive</column>
        /// <column name="LastWriteTime" type="DateTimeOffset">Gets the last time the entry in the zip archive was changed</column>
        /// <column name="Length" type="long">Gets the uncompressed size of the entry in the zip archive</column>
        /// <column name="IsDirectory" type="bool">Determine whether the entry is a directory</column>
        /// <column name="Level" type="int">Gets the nesting level</column>
        /// </columns>
        /// </example>
        /// </examples>
        /// </virtual-constructor>
        /// </virtual-constructors>
        public OsSchema()
            : base(SchemaName, CreateLibrary())
        {
            AddSource<FilesSource>(FilesTable);
            AddTable<FilesBasedTable>(FilesTable);

            AddSource<DirectoriesSource>(DirectoriesTable);
            AddTable<DirectoriesBasedTable>(DirectoriesTable);

            AddSource<ZipSource>(ZipTable);
            AddTable<ZipBasedTable>(ZipTable);

            AddSource<ProcessesSource>(ProcessesName);
            AddTable<ProcessBasedTable>(ProcessesName);

            AddSource<DllSource>(DllsTable);
            AddTable<DllBasedTable>(DllsTable);

            AddSource<CompareDirectoriesSource>(DirsCompare);
            AddTable<DirsCompareBasedTable>(DirsCompare);
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesBasedTable();
                case DirectoriesTable:
                    return new DirectoriesBasedTable();
                case ZipTable:
                    return new ZipBasedTable();
                case ProcessesName:
                    return new ProcessBasedTable();
                case DllsTable:
                    return new DllBasedTable();
                case DirsCompare:
                    return new DirsCompareBasedTable();
                case Single:
                    return new SingleRowSchemaTable();
            }

            throw new NotSupportedException($"Unsupported table {name}.");
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    if (parameters[0] is IReadOnlyTable filesTable)
                        return new FilesSource(filesTable, interCommunicator);

                    return new FilesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirectoriesTable:
                    if (parameters[0] is IReadOnlyTable directoriesTable)
                        return new DirectoriesSource(directoriesTable, interCommunicator);

                    return new DirectoriesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case ZipTable:
                    return new ZipSource((string)parameters[0], interCommunicator);
                case ProcessesName:
                    return new ProcessesSource(interCommunicator);
                case DllsTable:
                    return new DllSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirsCompare:
                    return new CompareDirectoriesSource((string)parameters[0], (string)parameters[1], interCommunicator);
                case Single:
                    return new SingleRowSource();
            }

            throw new NotSupportedException($"Unsupported row source {name}");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new OsLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}