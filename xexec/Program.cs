using System.Diagnostics;
using System.Runtime.InteropServices;

namespace xexec
{

    internal class Program
    {
        public class BinaryStreamReader
        {
            private readonly Stream _readFrom;
            private readonly Stream _writeTo;

            public BinaryStreamReader(Stream readFrom, Stream writeTo)
            {
                _readFrom = readFrom;
                _writeTo = writeTo;
            }

            public BinaryStreamReader(StreamReader reader)
                : this(reader.BaseStream, new MemoryStream()) { }

            public BinaryStreamReader(StreamReader reader, Stream _writeTo)
                : this(reader.BaseStream, _writeTo) { }

            public BinaryStreamReader(Stream stream)
                : this(stream, new MemoryStream()) { }

            public Stream Read()
            {
                _readFrom.CopyTo(_writeTo);
                if (_writeTo is MemoryStream ms)
                {
                    ms.Position = 0;
                }
                return _writeTo;
            }

            public async Task<Stream> ReadAsync()
            {
                await _readFrom.CopyToAsync(_writeTo);
                if (_writeTo is MemoryStream ms)
                {
                    ms.Position = 0;
                }
                return _writeTo;
            }

            public Stream Output => _writeTo;
        }

        public class BinaryStreamWriter
        {
            private readonly Stream _writeTo;
            private readonly Stream _readFrom;

            public BinaryStreamWriter(StreamWriter writeTo, Stream readFrom)
            {
                _writeTo = writeTo.BaseStream;
                _readFrom = readFrom;
            }

            public BinaryStreamWriter(Stream stream, Stream readFrom)
                : this(new StreamWriter(stream), readFrom) { }

            public void Write()
            {
                _readFrom.CopyTo(_writeTo);
                _writeTo.Flush();
            }

            public async Task WriteAsync()
            {
                await _readFrom.CopyToAsync(_writeTo);
                await _writeTo.FlushAsync();
            }
        }

        public static CommandLineSplit ParseCommandLine(string commandLine, int rc)
        {
            CommandLineSplit commandLineSplit = new(commandLine);
            XExecWriteDebug($"{rc} FileName[{commandLineSplit.FileName}] Arguments[{commandLineSplit.Arguments}]");
            if (!commandLineSplit.OK)
            {
                XExecWriteError($"unparseable command line: {commandLine}");
                Environment.Exit(rc);
            }
            return commandLineSplit;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCommandLineW();

        static string Win32GetCommandLine()
        {
            IntPtr ptr = GetCommandLineW();
            return Marshal.PtrToStringUni(ptr);
        }

        async static Task<int> XExec()
        {
            var commandLine = Win32GetCommandLine();
            XExecWriteDebug($"Win32GetCommandLine: {commandLine}");
            var parsedCommand = ParseCommandLine(commandLine, 911911);
            parsedCommand = ParseCommandLine(parsedCommand.Arguments, 912912);
            XExecWriteDebug($"FileName[{parsedCommand.FileName}] Arguments[{parsedCommand.Arguments}]");
            Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = parsedCommand.FileName,
                    Arguments = parsedCommand.Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };
            proc.Start();
            var stdinWriteTask = Task.CompletedTask;
            if (Console.IsInputRedirected)
            {
                stdinWriteTask = new BinaryStreamWriter(proc.StandardInput, Console.OpenStandardInput()).WriteAsync();
            }
            Stream thisStdOut = Console.OpenStandardOutput();
            Stream thisStdErr = Console.OpenStandardError();
            var stdoutReadTask = new BinaryStreamReader(proc.StandardOutput, thisStdOut).ReadAsync();
            var stderrReadTask = new BinaryStreamReader(proc.StandardError).ReadAsync();
            await Task.WhenAll(stdinWriteTask, stdoutReadTask, stderrReadTask);
            stderrReadTask.Result.CopyTo(thisStdErr);
            proc.WaitForExit();
            var exitCode = proc.ExitCode;
            XExecWriteDebug($"exit code: {exitCode}");
            return exitCode;
        }

        static void XExecWriteDebug(string message)
        {
            if (Environment.GetEnvironmentVariable("XEXEC_DEBUG") != null)
                Console.Error.WriteLine($"xexec debug: {message}");
        }

        static void XExecWriteError(string message)
        {
            if (Environment.GetEnvironmentVariable("XEXEC_QUIET") == null)
                Console.Error.WriteLine($"xexec error: {message}");
        }

        static async Task Main(string[] args)
        {
            try
            {
                var exitCode = await XExec();
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                XExecWriteError(ex.GetType().Name + ": " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
