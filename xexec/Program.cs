using System.Diagnostics;

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
                return _writeTo;
            }

            public async Task<Stream> ReadAsync()
            {
                await _readFrom.CopyToAsync(_writeTo);
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

        async static Task<int> XExec()
        {
            var commandLine = new CommandLineSplit(Environment.CommandLine);
            if (!commandLine.OK)
            {
                XExecWriteError($"unparseable command line: {Environment.CommandLine}");
                return 911911;
            }
            XExecWriteDebug($"FileName[{commandLine.FileName}] Arguments[{commandLine.Arguments}]");
            Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = commandLine.FileName,
                    Arguments = commandLine.Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };
            proc.Start();
            Stream thisStdIn = Console.OpenStandardInput();
            Stream thisStdOut = Console.OpenStandardOutput();
            Stream thisStdErr = Console.OpenStandardError();
            var stdinWriteTask = new BinaryStreamWriter(proc.StandardInput, thisStdIn).WriteAsync();
            var stdoutReadTask = new BinaryStreamReader(proc.StandardOutput, thisStdOut).ReadAsync();
            var stderrReadTask = new BinaryStreamReader(proc.StandardError).ReadAsync();
            await Task.WhenAll(stdinWriteTask, stdoutReadTask, stderrReadTask);
            (await stdoutReadTask).CopyTo(thisStdErr);
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
