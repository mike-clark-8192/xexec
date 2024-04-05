using System.Text.RegularExpressions;

namespace xexec
{
    public class CommandLineSplit
    {
        public CommandLineSplit(string commandLine)
        {
            Regex regexQuoted = new Regex(@"^\s*""(?<exe>[^""]+)""(?:\s(?<arg>.+))?$");
            Regex regexUnquoted = new Regex(@"^\s*(?<exe>[^\s]+)(?:\s(?<arg>.+))?$");
            Match matchQuoted = regexQuoted.Match(commandLine);
            Match matchUnquoted = regexUnquoted.Match(commandLine);
            if (matchQuoted.Success)
            {
                FileName = matchQuoted.Groups["exe"].Value;
                Arguments = matchQuoted.Groups["arg"].Value;
            }
            else if (matchUnquoted.Success)
            {
                FileName = matchUnquoted.Groups["exe"].Value;
                Arguments = matchUnquoted.Groups["arg"].Value;
            }
            else
            {
                OK = false;
            }
        }
        public bool OK { get; set; } = true;
        public string FileName { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}
