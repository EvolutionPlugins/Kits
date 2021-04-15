using System;
using System.Text.RegularExpressions;
using MySqlConnector.Logging;
using Serilog;
using Serilog.Events;

namespace Kits.Logging
{
    /// <summary>
    /// The extension of SerilogLoggerProvider in MySqlConnector.Logging.Serilog.
    /// The extension will every time create the Serilog logger when requested.
    /// This was made for OpenMod because it disposes Serilog on each reloads.
    /// The source code: https://github.com/mysql-net/MySqlConnector/blob/master/src/MySqlConnector.Logging.Serilog/SerilogLoggerProvider.cs
    /// </summary>
    public class SerilogLoggerProviderEx : IMySqlConnectorLoggerProvider
    {
        public IMySqlConnectorLogger CreateLogger(string name)
        {
            return new SerilogLoggerEx(name);
        }

        private class SerilogLoggerEx : IMySqlConnectorLogger
        {
            private static readonly Regex s_TokenReplacer = new(@"((\w+)?\s?(?:=|:)?\s?'?)\{(?:\d+)(\:\w+)?\}('?)",
                RegexOptions.Compiled);

            private readonly string m_Name;

            public SerilogLoggerEx(string name)
            {
                m_Name = name;
            }

            private ILogger CreateLogger()
            {
                return Serilog.Log.ForContext("SourceContext", $"MySqlConnector.{m_Name}");
            }

            public bool IsEnabled(MySqlConnectorLogLevel level)
            {
                return CreateLogger().IsEnabled(GetLevel(level));
            }

            private static LogEventLevel GetLevel(MySqlConnectorLogLevel level) => level switch
            {
                MySqlConnectorLogLevel.Trace => LogEventLevel.Verbose,
                MySqlConnectorLogLevel.Debug => LogEventLevel.Debug,
                MySqlConnectorLogLevel.Info => LogEventLevel.Information,
                MySqlConnectorLogLevel.Warn => LogEventLevel.Warning,
                MySqlConnectorLogLevel.Error => LogEventLevel.Error,
                MySqlConnectorLogLevel.Fatal => LogEventLevel.Fatal,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
            };

            public void Log(MySqlConnectorLogLevel level, string message, object?[]? args = null,
                Exception? exception = null)
            {
                var logger = CreateLogger();
                if (args is null || args.Length == 0)
                {
                    logger.Write(GetLevel(level), exception, message);
                }
                else
                {
                    // rewrite message as template
                    var template = s_TokenReplacer.Replace(message, "$1{MySql$2$3}$4");
                    logger.Write(GetLevel(level), exception, template, args);
                }
            }
        }
    }
}